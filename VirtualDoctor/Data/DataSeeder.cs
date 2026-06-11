using VirtualDoctor.Models;
using VirtualDoctor.Services;
using VirtualDoctor.Services.Storage;

namespace VirtualDoctor.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext db, IFileStorageService storage)
    {
        if (db.Users.Any() || db.Doctors.Any()) return;
        Console.WriteLine("[Seeder] Mulai seeding data...");

        var users = new List<ApplicationUser>
        {
            new() { Id = "user-001", Email = "budi@email.com", FullName = "Budi Santoso", PhoneNumber = "081234567890", DateOfBirth = new DateTime(1990, 5, 15), Gender = "Male", BloodType = "O", Address = "Jl. Merdeka No.10, Jakarta Pusat", Latitude = -6.2088, Longitude = 106.8456 },
            new() { Id = "user-002", Email = "siti@email.com", FullName = "Siti Nurhaliza", PhoneNumber = "081234567891", DateOfBirth = new DateTime(1985, 8, 22), Gender = "Female", BloodType = "A", Address = "Jl. Sudirman No.25, Jakarta Selatan", Latitude = -6.2278, Longitude = 106.8086 },
            new() { Id = "user-003", Email = "andi@email.com", FullName = "Andi Wijaya", PhoneNumber = "081234567892", DateOfBirth = new DateTime(1995, 3, 10), Gender = "Male", BloodType = "B", Address = "Jl. Thamrin No.5, Jakarta Pusat", Latitude = -6.1950, Longitude = 106.8230 },
            new() { Id = "user-004", Email = "rani@email.com", FullName = "Rani Kusuma", PhoneNumber = "081234567893", DateOfBirth = new DateTime(1992, 11, 28), Gender = "Female", BloodType = "AB", Address = "Jl. Gatot Subroto No.8, Jakarta Selatan", Latitude = -6.2387, Longitude = 106.7994 },
            new() { Id = "user-005", Email = "dewi@email.com", FullName = "Dewi Lestari", PhoneNumber = "081234567894", DateOfBirth = new DateTime(1978, 7, 3), Gender = "Female", BloodType = "O", Address = "Jl. Kebon Sirih No.15, Jakarta Pusat", Latitude = -6.1823, Longitude = 106.8336 },
            new() { Id = "user-006", Email = "ahmad@email.com", FullName = "Ahmad Fauzi", PhoneNumber = "081234567895", DateOfBirth = new DateTime(1988, 1, 17), Gender = "Male", BloodType = "A", Address = "Jl. Diponegoro No.30, Jakarta Pusat", Latitude = -6.1750, Longitude = 106.8275 },
            new() { Id = "user-007", Email = "lina@email.com", FullName = "Lina Marlina", PhoneNumber = "081234567896", DateOfBirth = new DateTime(1998, 9, 8), Gender = "Female", BloodType = "B", Address = "Jl. Veteran No.12, Jakarta Pusat", Latitude = -6.1700, Longitude = 106.8300 },
            new() { Id = "user-008", Email = "admin@virtualdoctor.com", FullName = "Admin VirtualDoctor", PhoneNumber = "081111111111", DateOfBirth = new DateTime(1990, 1, 1), Gender = "Male", BloodType = "O", Address = "VirtualDoctor HQ" },

            // Doctor logins
            new() { Id = "doc-user-001", Email = "andi.pratama@virtualdoctor.com", FullName = "Dr. Andi Pratama", IsDoctor = true, DoctorId = "doc-001", CreatedAt = DateTime.UtcNow },
            new() { Id = "doc-user-002", Email = "siti.rahayu@virtualdoctor.com", FullName = "Dr. Siti Rahayu", IsDoctor = true, DoctorId = "doc-002", CreatedAt = DateTime.UtcNow },
            new() { Id = "doc-user-003", Email = "budi.hartono@virtualdoctor.com", FullName = "Dr. Budi Hartono", IsDoctor = true, DoctorId = "doc-003", CreatedAt = DateTime.UtcNow }
        };
        db.Users.AddRange(users);

        foreach (var u in users)
            db.Set<Services.PasswordHash>().Add(new Services.PasswordHash
            {
                UserId = u.Id,
                Hash = AuthHelpers.HashPassword("Password123!")
            });

        db.Doctors.AddRange(new[] {
            new Doctor { Id = "doc-001", FullName = "Dr. Andi Pratama, Sp.PD", Email = "andi.pratama@vd.com", Specialization = "Spesialis Penyakit Dalam", SubSpecialization = "Diabetes & Endokrin", LicenseNumber = "STR-001-2020", ExperienceYears = 12, About = "Dokter spesialis penyakit dalam.", Education = "Universitas Indonesia", HospitalAffiliation = "RS Premier Jakarta", ConsultationFee = 150000, Rating = 4.8, TotalPatients = 2500, IsAvailable = true, IsOnline = true },
            new Doctor { Id = "doc-002", FullName = "Dr. Siti Rahayu, Sp.A", Email = "siti.rahayu@vd.com", Specialization = "Spesialis Anak", LicenseNumber = "STR-002-2020", ExperienceYears = 10, About = "Dokter spesialis anak.", Education = "Universitas Gadjah Mada", HospitalAffiliation = "RSIA Bunda Jakarta", ConsultationFee = 180000, Rating = 4.9, TotalPatients = 3200, IsAvailable = true, IsOnline = true },
            new Doctor { Id = "doc-003", FullName = "Dr. Budi Hartono, Sp.JP", Email = "budi.hartono@vd.com", Specialization = "Spesialis Jantung", LicenseNumber = "STR-003-2021", ExperienceYears = 15, About = "Dokter spesialis jantung.", Education = "Universitas Indonesia", HospitalAffiliation = "RS Jantung Harapan Kita", ConsultationFee = 200000, Rating = 4.7, TotalPatients = 1800, IsAvailable = true, IsOnline = false },
            new Doctor { Id = "doc-004", FullName = "Dr. Maya Indah, Sp.KK", Email = "maya.indah@vd.com", Specialization = "Spesialis Kulit & Kelamin", LicenseNumber = "STR-004-2021", ExperienceYears = 8, About = "Dokter spesialis kulit.", Education = "Universitas Airlangga", HospitalAffiliation = "RS Mitra Keluarga", ConsultationFee = 175000, Rating = 4.6, TotalPatients = 2100, IsAvailable = true, IsOnline = true },
            new Doctor { Id = "doc-005", FullName = "Dr. Rudi Hartawan, Sp.OG", Email = "rudi.hartawan@vd.com", Specialization = "Spesialis Obstetri & Ginekologi", LicenseNumber = "STR-005-2020", ExperienceYears = 14, About = "Dokter spesialis kandungan.", Education = "Universitas Padjadjaran", HospitalAffiliation = "RS Hermina", ConsultationFee = 190000, Rating = 4.8, TotalPatients = 2800, IsAvailable = true, IsOnline = false },
            new Doctor { Id = "doc-006", FullName = "dr. Putri Ayu", Email = "putri.ayu@vd.com", Specialization = "Dokter Umum", LicenseNumber = "STR-006-2022", ExperienceYears = 4, About = "Dokter umum ramah.", Education = "Universitas Diponegoro", HospitalAffiliation = "Puskesmas Makmur", ConsultationFee = 75000, Rating = 4.5, TotalPatients = 1500, IsAvailable = true, IsOnline = true },
            new Doctor { Id = "doc-007", FullName = "dr. Reza Fahlevi", Email = "reza.fahlevi@vd.com", Specialization = "Dokter Umum", LicenseNumber = "STR-007-2022", ExperienceYears = 3, About = "Dokter umum.", Education = "Universitas Brawijaya", HospitalAffiliation = "Klinik Sehat", ConsultationFee = 65000, Rating = 4.4, TotalPatients = 800, IsAvailable = true, IsOnline = true },
            new Doctor { Id = "doc-008", FullName = "Dr. Nina Wijaya, M.Psi", Email = "nina.wijaya@vd.com", Specialization = "Psikolog", LicenseNumber = "PSI-001-2021", ExperienceYears = 9, About = "Psikolog klinis.", Education = "Universitas Indonesia", HospitalAffiliation = "Klinik Psikologi Harmoni", ConsultationFee = 160000, Rating = 4.7, TotalPatients = 1200, IsAvailable = true, IsOnline = true },
            new Doctor { Id = "doc-009", FullName = "Dr. Hendra Gunawan, Sp.PD-KGer", Email = "hendra.gunawan@vd.com", Specialization = "Spesialis Geriatri", LicenseNumber = "STR-009-2019", ExperienceYears = 18, About = "Dokter spesialis geriatri.", Education = "Universitas Indonesia", HospitalAffiliation = "RS Premier Jakarta", ConsultationFee = 185000, Rating = 4.9, TotalPatients = 3500, IsAvailable = true, IsOnline = false },
            new Doctor { Id = "doc-010", FullName = "Dr. Fitriani, Sp.GK", Email = "fitriani@vd.com", Specialization = "Spesialis Gizi Klinik", LicenseNumber = "STR-010-2021", ExperienceYears = 7, About = "Dokter spesialis gizi.", Education = "Universitas Hasanuddin", HospitalAffiliation = "RS Siloam", ConsultationFee = 155000, Rating = 4.6, TotalPatients = 900, IsAvailable = true, IsOnline = true }
        });

        db.Hospitals.AddRange(new[] {
            new Hospital { Id = "hosp-001", Name = "RS Premier Jakarta", Type = HospitalType.Hospital, Address = "Jl. Sudirman No.1, Jakarta Pusat", City = "Jakarta Pusat", Latitude = -6.2088, Longitude = 106.8456, PhoneNumber = "021-1234567", Rating = 4.5, TotalReviews = 350, AcceptsInsurance = true },
            new Hospital { Id = "hosp-002", Name = "RSIA Bunda Jakarta", Type = HospitalType.Hospital, Address = "Jl. Menteng No.20, Jakarta Pusat", City = "Jakarta Pusat", Latitude = -6.1950, Longitude = 106.8336, PhoneNumber = "021-2345678", Rating = 4.7, TotalReviews = 420, AcceptsInsurance = true },
            new Hospital { Id = "hosp-003", Name = "RS Jantung Harapan Kita", Type = HospitalType.Hospital, Address = "Jl. Letjen S Parman No.87, Jakarta Barat", City = "Jakarta Barat", Latitude = -6.2000, Longitude = 106.7969, PhoneNumber = "021-3456789", Rating = 4.8, TotalReviews = 500, AcceptsInsurance = true },
            new Hospital { Id = "hosp-004", Name = "Klinik Sehat 24", Type = HospitalType.Clinic, Address = "Jl. Thamrin No.5, Jakarta Pusat", City = "Jakarta Pusat", Latitude = -6.1950, Longitude = 106.8230, PhoneNumber = "021-4567890", Rating = 4.3, TotalReviews = 180 },
            new Hospital { Id = "hosp-005", Name = "Puskesmas Makmur", Type = HospitalType.HealthCenter, Address = "Jl. Merdeka No.10, Jakarta Pusat", City = "Jakarta Pusat", Latitude = -6.2088, Longitude = 106.8456, PhoneNumber = "021-5678901", Rating = 4.0, TotalReviews = 90, AcceptsInsurance = true },
            new Hospital { Id = "hosp-006", Name = "Apotek Sehat Farma", Type = HospitalType.Pharmacy, Address = "Jl. Diponegoro No.30, Jakarta Pusat", City = "Jakarta Pusat", Latitude = -6.1750, Longitude = 106.8275, PhoneNumber = "021-6789012", Rating = 4.2, TotalReviews = 75 },
            new Hospital { Id = "hosp-007", Name = "RS Mitra Keluarga", Type = HospitalType.Hospital, Address = "Jl. Gatot Subroto No.8, Jakarta Selatan", City = "Jakarta Selatan", Latitude = -6.2387, Longitude = 106.7994, PhoneNumber = "021-7890123", Rating = 4.4, TotalReviews = 310, AcceptsInsurance = true }
        });

        db.Medicines.AddRange(new[] {
            new Medicine { Id = "med-001", Name = "Paracetamol 500mg", Category = "Obat Bebas", Description = "Pereda nyeri dan penurun demam.", Price = 5000, Stock = 500, Manufacturer = "PT Kimia Farma", Rating = 4.5, TotalSold = 15000 },
            new Medicine { Id = "med-002", Name = "Amoxicillin 500mg", Category = "Obat Keras", Description = "Antibiotik.", Price = 12000, Stock = 200, RequiresPrescription = true, Manufacturer = "PT Sanbe Farma", Rating = 4.3, TotalSold = 8000 },
            new Medicine { Id = "med-003", Name = "Vitamin C 1000mg", Category = "Vitamin", Description = "Daya tahan tubuh.", Price = 35000, Stock = 1000, Manufacturer = "PT Kalbe Farma", Rating = 4.7, TotalSold = 25000 },
            new Medicine { Id = "med-004", Name = "Omeprazole 20mg", Category = "Obat Keras", Description = "Asam lambung.", Price = 8000, Stock = 300, RequiresPrescription = true, Manufacturer = "PT Dexa Medica", Rating = 4.4, TotalSold = 10000 },
            new Medicine { Id = "med-005", Name = "Antangin Herbal", Category = "Obat Bebas", Description = "Masuk angin.", Price = 4500, Stock = 800, Manufacturer = "PT Deltomed", Rating = 4.2, TotalSold = 30000 },
            new Medicine { Id = "med-006", Name = "Vitamin D3 1000IU", Category = "Vitamin", Description = "Kesehatan tulang.", Price = 55000, Stock = 600, Manufacturer = "PT Kalbe Farma", Rating = 4.6, TotalSold = 12000 },
            new Medicine { Id = "med-007", Name = "Cetirizine 10mg", Category = "Obat Bebas", Description = "Antihistamin alergi.", Price = 3000, Stock = 700, Manufacturer = "PT Sanbe Farma", Rating = 4.4, TotalSold = 18000 },
            new Medicine { Id = "med-008", Name = "Curcuma Plus", Category = "Suplemen", Description = "Nafsu makan.", Price = 25000, Stock = 400, Manufacturer = "PT Soho Industri", Rating = 4.8, TotalSold = 20000 },
            new Medicine { Id = "med-009", Name = "Ibuprofen 400mg", Category = "Obat Bebas", Description = "Anti-inflamasi.", Price = 7000, Stock = 450, Manufacturer = "PT Kimia Farma", Rating = 4.3, TotalSold = 14000 },
            new Medicine { Id = "med-010", Name = "Bioplacenton Gel", Category = "Obat Bebas", Description = "Luka bakar.", Price = 45000, Stock = 250, Manufacturer = "PT Kalbe Farma", Rating = 4.5, TotalSold = 9000 },
            new Medicine { Id = "med-011", Name = "Simvastatin 10mg", Category = "Obat Keras", Description = "Penurun kolesterol.", Price = 15000, Stock = 150, RequiresPrescription = true, Manufacturer = "PT Dexa Medica", Rating = 4.1, TotalSold = 5000 },
            new Medicine { Id = "med-012", Name = "Madu Herbal Anak", Category = "Suplemen", Description = "Daya tahan anak.", Price = 30000, Stock = 350, Manufacturer = "PT Deltomed", Rating = 4.7, TotalSold = 22000 }
        });

        // Appointments, consultations, orders, homecare, chat sample
        db.Appointments.AddRange(
            new Appointment { UserId = "user-001", DoctorId = "doc-001", HospitalId = "hosp-001", AppointmentDate = DateTime.UtcNow.AddDays(3), StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(9, 30, 0), Type = AppointmentType.InPerson, Status = AppointmentStatus.Scheduled, EstimatedCost = 150000 },
            new Appointment { UserId = "user-002", DoctorId = "doc-002", HospitalId = "hosp-002", AppointmentDate = DateTime.UtcNow.AddDays(2), StartTime = new TimeSpan(10, 0, 0), EndTime = new TimeSpan(10, 30, 0), Type = AppointmentType.InPerson, Status = AppointmentStatus.Scheduled, EstimatedCost = 180000 },
            new Appointment { UserId = "user-003", DoctorId = "doc-006", AppointmentDate = DateTime.UtcNow.AddDays(1), StartTime = new TimeSpan(14, 0, 0), EndTime = new TimeSpan(14, 30, 0), Type = AppointmentType.Online, Status = AppointmentStatus.Confirmed, EstimatedCost = 75000 }
        );

        db.HomecareServices.Add(new HomecareService { UserId = "user-005", ServiceType = HomecareServiceType.LabTest, ScheduledDate = DateTime.UtcNow.AddDays(5), Address = "Jl. Kebon Sirih No.15, Jakarta Pusat", Fee = 250000 });

        // Schedules
        foreach (var doc in new[] { "doc-001", "doc-002", "doc-003", "doc-004", "doc-005", "doc-006", "doc-007", "doc-008", "doc-009", "doc-010" })
            for (int d = 1; d <= 5; d++)
                db.DoctorSchedules.Add(new DoctorSchedule { DoctorId = doc, Day = (DayOfWeek)d, StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(16, 0, 0), MaxPatients = 15 });

        await db.SaveChangesAsync();
        Console.WriteLine("[Seeder] Selesai! ✅");
    }
}
