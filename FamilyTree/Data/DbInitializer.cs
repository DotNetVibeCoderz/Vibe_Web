using FamilyTree.Models;
using FamilyTree.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var encryption = scope.ServiceProvider.GetRequiredService<IEncryptionService>();

        //await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        var roles = new[] { "admin", "user" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var admin = await userManager.Users.FirstOrDefaultAsync(u => u.UserName == "admin");
        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = "admin",
                Email = "admin@familytree.app",
                DisplayName = "Administrator",
                Location = "Jakarta",
                EmailConfirmed = true
            };
            await userManager.CreateAsync(admin, "admin123");
            await userManager.AddToRoleAsync(admin, "admin");
        }

        if (!context.FamilyTrees.Any())
        {
            var tree = new FamilyTreeEntity
            {
                Name = "Keluarga Nusantara",
                Description = "Contoh pohon keluarga multigenerasi untuk demo",
                OwnerId = admin.Id
            };

            var people = new List<Person>
            {
                new()
                {
                    FirstName = "Sutrisno",
                    LastName = "Wibowo",
                    BirthDate = new DateTime(1945, 8, 17),
                    MarriageDate = new DateTime(1969, 6, 12),
                    Gender = "Male",
                    Location = "Yogyakarta",
                    BranchTag = "A",
                    NotesEncrypted = encryption.Encrypt("Pendiri keluarga, suka berkebun.")
                },
                new()
                {
                    FirstName = "Ratna",
                    LastName = "Wibowo",
                    BirthDate = new DateTime(1948, 3, 10),
                    MarriageDate = new DateTime(1969, 6, 12),
                    Gender = "Female",
                    Location = "Yogyakarta",
                    BranchTag = "A",
                    NotesEncrypted = encryption.Encrypt("Hobi memasak, aktif di komunitas.")
                },
                new()
                {
                    FirstName = "Budi",
                    LastName = "Wibowo",
                    BirthDate = new DateTime(1970, 5, 4),
                    MarriageDate = new DateTime(1994, 9, 4),
                    Gender = "Male",
                    Location = "Bandung",
                    BranchTag = "B",
                    NotesEncrypted = encryption.Encrypt("Anak sulung, bekerja di IT.")
                },
                new()
                {
                    FirstName = "Ayu",
                    LastName = "Wibowo",
                    BirthDate = new DateTime(1975, 11, 21),
                    MarriageDate = new DateTime(1999, 2, 14),
                    Gender = "Female",
                    Location = "Surabaya",
                    BranchTag = "C",
                    NotesEncrypted = encryption.Encrypt("Dokter anak, suka traveling.")
                },
                new()
                {
                    FirstName = "Dimas",
                    LastName = "Wibowo",
                    BirthDate = new DateTime(2000, 1, 14),
                    Gender = "Male",
                    Location = "Jakarta",
                    BranchTag = "B",
                    NotesEncrypted = encryption.Encrypt("Mahasiswa desain produk.")
                },
                new()
                {
                    FirstName = "Naya",
                    LastName = "Wibowo",
                    BirthDate = new DateTime(2003, 7, 9),
                    Gender = "Female",
                    Location = "Jakarta",
                    BranchTag = "B",
                    NotesEncrypted = encryption.Encrypt("Menyukai fotografi dan video.")
                }
            };

            tree.Members = people;

            tree.TimelineEvents = new List<TimelineEvent>
            {
                new()
                {
                    Title = "Pernikahan Sutrisno & Ratna",
                    Description = "Upacara pernikahan keluarga besar.",
                    EventDate = new DateTime(1969, 6, 12)
                },
                new()
                {
                    Title = "Reuni Keluarga 2015",
                    Description = "Pertemuan keluarga besar di Bandung.",
                    EventDate = new DateTime(2015, 12, 20)
                }
            };

            context.FamilyTrees.Add(tree);
            await context.SaveChangesAsync();

            var relations = new List<Relationship>
            {
                new() { PersonId = people[0].Id, RelatedPersonId = people[1].Id, Type = RelationshipType.Spouse },
                new() { PersonId = people[1].Id, RelatedPersonId = people[0].Id, Type = RelationshipType.Spouse },
                new() { PersonId = people[0].Id, RelatedPersonId = people[2].Id, Type = RelationshipType.Parent },
                new() { PersonId = people[1].Id, RelatedPersonId = people[2].Id, Type = RelationshipType.Parent },
                new() { PersonId = people[0].Id, RelatedPersonId = people[3].Id, Type = RelationshipType.Parent },
                new() { PersonId = people[1].Id, RelatedPersonId = people[3].Id, Type = RelationshipType.Parent },
                new() { PersonId = people[2].Id, RelatedPersonId = people[4].Id, Type = RelationshipType.Parent },
                new() { PersonId = people[2].Id, RelatedPersonId = people[5].Id, Type = RelationshipType.Parent },
                new() { PersonId = people[4].Id, RelatedPersonId = people[5].Id, Type = RelationshipType.Sibling }
            };

            context.Relationships.AddRange(relations);

            var stories = new List<Story>
            {
                new() { PersonId = people[0].Id, Title = "Cerita Merdeka", Content = "Mengikuti perayaan kemerdekaan pertama." },
                new() { PersonId = people[4].Id, Title = "Proyek Kampus", Content = "Membuat prototipe kursi bambu." }
            };

            context.Stories.AddRange(stories);

            var media = new List<MediaItem>
            {
                new() { PersonId = people[1].Id, Type = MediaType.Photo, Url = "https://images.unsplash.com/photo-1524504388940-b1c1722653e1?auto=format&fit=crop&w=400&q=80", Caption = "Foto keluarga 1970" },
                new() { PersonId = people[5].Id, Type = MediaType.Photo, Url = "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?auto=format&fit=crop&w=400&q=80", Caption = "Wisuda Naya" },
                new() { PersonId = people[2].Id, Type = MediaType.Document, Url = "https://www.w3.org/WAI/ER/tests/xhtml/testfiles/resources/pdf/dummy.pdf", Caption = "Akta kelahiran" }
            };

            context.MediaItems.AddRange(media);

            await context.SaveChangesAsync();
        }
    }
}
