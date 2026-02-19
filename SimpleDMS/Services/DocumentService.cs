using Microsoft.EntityFrameworkCore;
using SimpleDMS.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

namespace SimpleDMS.Services
{
    public class DocumentService
    {
        private readonly AppDbContext _context;
        private readonly IStorageService _storage;

        public DocumentService(AppDbContext context, IStorageService storage)
        {
            _context = context;
            _storage = storage;
        }

        public async Task<int> GetDocumentCountAsync() => await _context.Documents.CountAsync();
        public async Task<int> GetUserCountAsync() => await _context.Users.CountAsync();
        
        public async Task<List<AuditLog>> GetRecentAuditLogsAsync(int count = 10)
        {
            return await _context.AuditLogs
                .Include(a => a.User)
                .OrderByDescending(a => a.Timestamp)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<Document>> SearchDocumentsAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return new List<Document>();

            // Menambahkan wildcard % agar berfungsi seperti LIKE %query% di SQL
            var pattern = $"%{query}%";

            return await _context.Documents
                .Include(d => d.Versions)
                .Include(d => d.Tags)
                .Where(d => EF.Functions.Like(d.Title, pattern) || 
                            EF.Functions.Like(d.Description ?? "", pattern) || 
                            EF.Functions.Like(d.Category ?? "", pattern) ||
                            d.Tags.Any(t => EF.Functions.Like(t.Name, pattern)))
                .OrderByDescending(d => d.UpdatedAt)
                .ToListAsync();
        }

        public async Task<Document> UploadDocumentAsync(string title, Stream content, string fileName, string contentType, string? userId, Guid folderId)
        {
            var folder = await _context.Folders.FindAsync(folderId);
            if (folder == null) throw new Exception("Folder not found");

            var doc = new Document
            {
                Title = title,
                FolderId = folderId,
                UpdatedAt = DateTime.UtcNow
            };

            var relativePath = await _storage.SaveFileAsync(content, fileName, folderId.ToString());

            var validUserId = await CheckUserExists(userId);

            var version = new DocumentVersion
            {
                Document = doc,
                FileName = fileName,
                FilePath = relativePath,
                FileSize = content.Length,
                ContentType = contentType,
                VersionLabel = "1.0",
                CreatedByUserId = validUserId,
                ChangeNote = "Initial upload"
            };

            doc.Versions.Add(version);
            _context.Documents.Add(doc);
            
            await LogActivity(validUserId, "Upload", "Document", doc.Id.ToString(), $"Uploaded document: {title}");
            await _context.SaveChangesAsync();

            return doc;
        }

        private async Task<string?> CheckUserExists(string? userId)
        {
            if (string.IsNullOrEmpty(userId)) return null;
            var user = await _context.Users.AnyAsync(u => u.Id == userId);
            return user ? userId : null;
        }

        public async Task<Folder> CreateFolderAsync(string name, Guid? parentId)
        {
            var folder = new Folder { Name = name, ParentFolderId = parentId };
            _context.Folders.Add(folder);
            await _context.SaveChangesAsync();
            return folder;
        }

        public async Task<List<Folder>> GetSubFoldersAsync(Guid parentId)
        {
            return await _context.Folders
                .Where(f => f.ParentFolderId == parentId)
                .OrderBy(f => f.Name)
                .ToListAsync();
        }

        public async Task<Folder?> GetFolderAsync(Guid id)
        {
            return await _context.Folders
                .Include(f => f.ParentFolder)
                .FirstOrDefaultAsync(f => f.Id == id);
        }

        public async Task CheckOutAsync(Guid docId, string userId)
        {
            var doc = await _context.Documents.FindAsync(docId);
            if (doc == null) throw new Exception("Not found");
            if (doc.CheckedOutByUserId != null) throw new Exception("Already checked out");

            var validUserId = await CheckUserExists(userId);
            doc.CheckedOutByUserId = validUserId;
            doc.CheckedOutAt = DateTime.UtcNow;
            
            await LogActivity(validUserId, "Check-out", "Document", doc.Id.ToString(), "Document checked out");
            await _context.SaveChangesAsync();
        }

        public async Task CheckInAsync(Guid docId, string userId, Stream? newContent, string? fileName, string? contentType, string changeNote)
        {
            var doc = await _context.Documents.Include(d => d.Versions).FirstOrDefaultAsync(d => d.Id == docId);
            if (doc == null) throw new Exception("Not found");
            
            var validUserId = await CheckUserExists(userId);
            if (doc.CheckedOutByUserId != validUserId && validUserId != null) throw new Exception("Not checked out by you");

            if (newContent != null && fileName != null)
            {
                var relativePath = await _storage.SaveFileAsync(newContent, fileName, doc.FolderId.ToString());
                var lastVersion = doc.Versions.OrderByDescending(v => v.CreatedAt).FirstOrDefault();
                
                double versionNum = 1.0;
                if (lastVersion != null && double.TryParse(lastVersion.VersionLabel, out var lastNum))
                {
                    versionNum = lastNum + 1.0;
                }

                var version = new DocumentVersion
                {
                    DocumentId = docId,
                    FileName = fileName,
                    FilePath = relativePath,
                    FileSize = newContent.Length,
                    ContentType = contentType ?? "application/octet-stream",
                    VersionLabel = versionNum.ToString("0.0"),
                    CreatedByUserId = validUserId,
                    ChangeNote = changeNote
                };
                doc.Versions.Add(version);
            }

            doc.CheckedOutByUserId = null;
            doc.CheckedOutAt = null;
            doc.UpdatedAt = DateTime.UtcNow;

            await LogActivity(validUserId, "Check-in", "Document", doc.Id.ToString(), $"Document checked in. Note: {changeNote}");
            await _context.SaveChangesAsync();
        }

        public async Task LogActivity(string? userId, string action, string entity, string entityId, string details)
        {
            var validUserId = await CheckUserExists(userId);
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = validUserId,
                Action = action,
                EntityName = entity,
                EntityId = entityId,
                Details = details,
                Timestamp = DateTime.UtcNow
            });
        }

        public async Task<List<Document>> GetDocumentsAsync(Guid folderId)
        {
            return await _context.Documents
                .Include(d => d.Tags)
                .Include(d => d.Versions)
                .Where(d => d.FolderId == folderId)
                .OrderByDescending(d => d.UpdatedAt)
                .ToListAsync();
        }

        public async Task<DocumentVersion?> GetLatestVersionAsync(Guid docId)
        {
            return await _context.DocumentVersions
                .Where(v => v.DocumentId == docId)
                .OrderByDescending(v => v.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<Stream> GetFileStreamAsync(string filePath)
        {
            return await _storage.GetFileAsync(filePath);
        }
        
        public async Task<Folder> GetOrCreateRootFolder()
        {
            var root = await _context.Folders.FirstOrDefaultAsync(f => f.ParentFolderId == null);
            if (root == null)
            {
                root = new Folder { Name = "Root" };
                _context.Folders.Add(root);
                await _context.SaveChangesAsync();
            }
            return root;
        }
    }
}