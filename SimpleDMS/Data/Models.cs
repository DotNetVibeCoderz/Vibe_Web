using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace SimpleDMS.Data
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public List<AuditLog> AuditLogs { get; set; } = new();
    }

    public class Folder
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public Guid? ParentFolderId { get; set; }
        public Folder? ParentFolder { get; set; }
        public List<Folder> SubFolders { get; set; } = new();
        public List<Document> Documents { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Document
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = "General";
        public string Description { get; set; } = string.Empty;
        
        public Guid FolderId { get; set; }
        public Folder Folder { get; set; } = null!;
        
        public string? CheckedOutByUserId { get; set; }
        public ApplicationUser? CheckedOutByUser { get; set; }
        public DateTime? CheckedOutAt { get; set; }
        
        public List<DocumentVersion> Versions { get; set; } = new();
        public List<DocumentTag> Tags { get; set; } = new();
        public List<Comment> Comments { get; set; } = new();
        
        public string CurrentStatus { get; set; } = "Draft"; // Draft, Review, Published
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;
    }

    public class DocumentVersion
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid DocumentId { get; set; }
        public Document Document { get; set; } = null!;
        
        public string VersionLabel { get; set; } = "1.0";
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string ContentType { get; set; } = string.Empty;
        
        public string? CreatedByUserId { get; set; } // Ubah jadi nullable
        public ApplicationUser? CreatedByUser { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? ChangeNote { get; set; }
    }

    public class DocumentTag
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public Guid DocumentId { get; set; }
        public Document Document { get; set; } = null!;
    }

    public class Comment
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Content { get; set; } = string.Empty;
        public string? UserId { get; set; } // Ubah jadi nullable
        public ApplicationUser? User { get; set; }
        public Guid DocumentId { get; set; }
        public Document Document { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class AuditLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Action { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string? UserId { get; set; } // Ubah jadi nullable agar tidak melanggar FK jika user tidak ditemukan
        public ApplicationUser? User { get; set; }
        public string Details { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class Notification
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Message { get; set; } = string.Empty;
        public string TargetUserId { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}