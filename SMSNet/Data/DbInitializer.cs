using Microsoft.AspNetCore.Identity;
using SMSNet.Models;

namespace SMSNet.Data;

public class DbInitializer
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public DbInitializer(ApplicationDbContext context, UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task SeedAsync()
    {
        foreach (var role in AppRoles.All)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var admin = await _userManager.FindByNameAsync("admin");
        if (admin == null)
        {
            admin = new AppUser
            {
                UserName = "admin",
                FullName = "Administrator SMSNet",
                Email = "admin@smsnet.local",
                RoleDisplay = "Admin"
            };

            await _userManager.CreateAsync(admin, "admin123");
            await _userManager.AddToRoleAsync(admin, AppRoles.Admin);
        }

        if (_context.Students.Any())
        {
            return;
        }

        var random = new Random(42);
        var classNames = new[] { "7A", "7B", "8A", "8B", "9A", "9B", "10A", "10B" };
        var subjects = new[] { "Matematika", "Bahasa Indonesia", "Bahasa Inggris", "IPA", "IPS", "Informatika", "Agama", "Seni" };

        for (var i = 1; i <= 40; i++)
        {
            _context.Students.Add(new Student
            {
                FullName = $"Siswa {i:00}",
                ClassName = classNames[random.Next(classNames.Length)],
                DateOfBirth = DateTime.Now.AddYears(-13).AddDays(random.Next(365)),
                Gender = i % 2 == 0 ? "Perempuan" : "Laki-laki",
                ParentName = $"Orang Tua {i:00}",
                Phone = $"0812-00{i:0000}",
                Status = i % 7 == 0 ? "Inactive" : "Active"
            });
        }

        for (var i = 1; i <= 15; i++)
        {
            _context.Teachers.Add(new Teacher
            {
                FullName = $"Guru {i:00}",
                Subject = subjects[random.Next(subjects.Length)],
                Email = $"guru{i:00}@smsnet.sch.id",
                Phone = $"0813-77{i:000}",
                Status = "Active"
            });
        }

        for (var i = 1; i <= 10; i++)
        {
            _context.Parents.Add(new ParentGuardian
            {
                FullName = $"Orang Tua {i:00}",
                Email = $"ortu{i:00}@mail.com",
                Phone = $"0812-88{i:000}",
                StudentName = $"Siswa {i:00}"
            });
        }

        for (var i = 1; i <= 8; i++)
        {
            _context.ClassRooms.Add(new ClassRoom
            {
                Name = classNames[i - 1],
                HomeroomTeacher = $"Guru {i:00}",
                Capacity = 32
            });
        }

        foreach (var subject in subjects)
        {
            _context.Subjects.Add(new Subject
            {
                Name = subject,
                Credits = 2,
                Description = $"Materi utama untuk {subject}"
            });
        }

        _context.CurriculumItems.AddRange(new[]
        {
            new CurriculumItem { Name = "Kurikulum Merdeka", GradeLevel = "SMP", Description = "Fokus pada kompetensi dan project." },
            new CurriculumItem { Name = "STEM Integration", GradeLevel = "SMA", Description = "Menggabungkan sains, teknologi, engineering, dan matematika." }
        });

        for (var i = 0; i < 12; i++)
        {
            _context.ScheduleItems.Add(new ScheduleItem
            {
                ClassName = classNames[random.Next(classNames.Length)],
                Subject = subjects[random.Next(subjects.Length)],
                Teacher = $"Guru {random.Next(1, 15):00}",
                Day = new[] { "Senin", "Selasa", "Rabu", "Kamis", "Jumat" }[random.Next(5)],
                TimeSlot = $"{8 + random.Next(6)}:00 - {9 + random.Next(6)}:40"
            });
        }

        for (var i = 1; i <= 18; i++)
        {
            _context.AttendanceRecords.Add(new AttendanceRecord
            {
                PersonName = i % 2 == 0 ? $"Siswa {i:00}" : $"Guru {i:00}",
                Role = i % 2 == 0 ? "Siswa" : "Guru",
                Date = DateTime.Today.AddDays(-random.Next(10)),
                Status = i % 5 == 0 ? "Absent" : "Present",
                Method = i % 3 == 0 ? "RFID" : "Barcode"
            });
        }

        for (var i = 1; i <= 20; i++)
        {
            _context.GradeRecords.Add(new GradeRecord
            {
                StudentName = $"Siswa {i:00}",
                Subject = subjects[random.Next(subjects.Length)],
                Score = random.Next(60, 100),
                Notes = "Perlu latihan tambahan pada topik tertentu"
            });
        }

        _context.ELearningContents.AddRange(new[]
        {
            new ELearningContent { Title = "Video Pembelajaran Matematika", ModuleType = "Video", Description = "Materi persamaan linear" },
            new ELearningContent { Title = "Quiz Bahasa Inggris", ModuleType = "Quiz", Description = "Ulangan harian" }
        });

        _context.TaskExams.AddRange(new[]
        {
            new TaskExam { Title = "UTS Matematika", Type = "Ujian", DueDate = DateTime.Today.AddDays(10), Status = "Scheduled" },
            new TaskExam { Title = "Tugas IPA", Type = "Tugas", DueDate = DateTime.Today.AddDays(4), Status = "Open" }
        });

        _context.ForumPosts.AddRange(new[]
        {
            new ForumPost { Topic = "Strategi belajar efektif", Author = "Guru 01", Content = "Diskusi metode pembelajaran aktif", PostedAt = DateTime.Today.AddDays(-2) },
            new ForumPost { Topic = "Koordinasi ujian", Author = "Staff 02", Content = "Jadwal ujian semester", PostedAt = DateTime.Today.AddDays(-1) }
        });

        _context.PerformanceReviews.AddRange(new[]
        {
            new PerformanceReview { TeacherName = "Guru 01", KPI = "Kehadiran", Score = "95%" },
            new PerformanceReview { TeacherName = "Guru 02", KPI = "Hasil belajar", Score = "90%" }
        });

        for (var i = 1; i <= 15; i++)
        {
            _context.PaymentRecords.Add(new PaymentRecord
            {
                StudentName = $"Siswa {i:00}",
                Category = i % 2 == 0 ? "SPP" : "Buku",
                Amount = 500000 + i * 25000,
                Status = i % 4 == 0 ? "Pending" : "Paid",
                Date = DateTime.Today.AddDays(-i)
            });
        }

        _context.InventoryItems.AddRange(new[]
        {
            new InventoryItem { Name = "Laptop Lab", Category = "Lab Komputer", Quantity = 20, Condition = "Good" },
            new InventoryItem { Name = "Mikroskop", Category = "Lab IPA", Quantity = 12, Condition = "Fair" }
        });

        _context.PayrollRecords.AddRange(new[]
        {
            new PayrollRecord { EmployeeName = "Guru 01", Role = "Guru", Salary = 5000000, Period = DateTime.Today.AddMonths(-1) },
            new PayrollRecord { EmployeeName = "Staff 01", Role = "Staff", Salary = 4000000, Period = DateTime.Today.AddMonths(-1) }
        });

        _context.FinancialReports.AddRange(new[]
        {
            new FinancialReport { Title = "Laporan Bulanan", Income = 150000000, Expense = 90000000, Period = DateTime.Today.AddMonths(-1) },
            new FinancialReport { Title = "Laporan Triwulan", Income = 450000000, Expense = 260000000, Period = DateTime.Today.AddMonths(-3) }
        });

        _context.Notifications.AddRange(new[]
        {
            new NotificationItem { Title = "Ujian Tengah Semester", Message = "UTS akan dimulai pekan depan.", Date = DateTime.Today, Audience = "Siswa" },
            new NotificationItem { Title = "Pembayaran SPP", Message = "Batas pembayaran SPP tanggal 25.", Date = DateTime.Today, Audience = "Orang Tua" }
        });

        _context.Documents.AddRange(new[]
        {
            new DocumentItem { DocumentType = "Rapor", OwnerName = "Siswa 01", FilePath = "/uploads/rapor_siswa01.pdf" },
            new DocumentItem { DocumentType = "Surat", OwnerName = "Siswa 02", FilePath = "/uploads/surat_siswa02.pdf" }
        });

        _context.AuditTrails.AddRange(new[]
        {
            new AuditTrail { Action = "Login", Actor = "admin", Timestamp = DateTime.Now.AddMinutes(-30), Detail = "Admin login sukses" },
            new AuditTrail { Action = "Update Data", Actor = "admin", Timestamp = DateTime.Now.AddMinutes(-10), Detail = "Update data siswa" }
        });

        _context.Events.AddRange(new[]
        {
            new EventItem { Title = "Lomba Sains", Date = DateTime.Today.AddDays(5), Location = "Aula", Type = "Ekstrakurikuler" },
            new EventItem { Title = "Rapat Orang Tua", Date = DateTime.Today.AddDays(12), Location = "Ruang Meeting", Type = "Event" }
        });

        await _context.SaveChangesAsync();
    }
}
