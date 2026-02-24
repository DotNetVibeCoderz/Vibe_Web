using Microsoft.AspNetCore.Identity;
using Bogus;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace MyClinic.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            context.Database.EnsureCreated();

            // Seed Roles
            string[] roles = { "Admin", "Doctor", "Nurse", "Pharmacist", "Patient" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Seed Admin User
            if (await userManager.FindByEmailAsync("admin@clinic.com") == null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = "admin",
                    Email = "admin@clinic.com",
                    FullName = "Administrator",
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(adminUser, "admin123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Seed Data with Bogus
            if (!context.Patients.Any())
            {
                var patientFaker = new Faker<Patient>()
                    .RuleFor(p => p.MedicalRecordNumber, f => "MR" + f.Random.Number(100000, 999999))
                    .RuleFor(p => p.FullName, f => f.Name.FullName())
                    .RuleFor(p => p.Nik, f => f.Random.Long(1000000000000000L, 9999999999999999L).ToString())
                    .RuleFor(p => p.DoB, f => f.Person.DateOfBirth)
                    .RuleFor(p => p.Gender, f => f.PickRandom("Male", "Female"))
                    .RuleFor(p => p.Phone, f => f.Phone.PhoneNumber("08##########"))
                    .RuleFor(p => p.Address, f => f.Address.FullAddress());
                
                var patients = patientFaker.Generate(100);
                context.Patients.AddRange(patients);
                await context.SaveChangesAsync();

                var employeeFaker = new Faker<Employee>()
                    .RuleFor(e => e.FullName, f => "Dr. " + f.Name.FullName())
                    .RuleFor(e => e.Role, f => "Doctor")
                    .RuleFor(e => e.Specialization, f => f.PickRandom("General", "Pediatrician", "Dentist", "Cardiologist"))
                    .RuleFor(e => e.Phone, f => f.Phone.PhoneNumber("08##########"));
                
                var doctors = employeeFaker.Generate(10);
                context.Employees.AddRange(doctors);
                await context.SaveChangesAsync();

                var medicineFaker = new Faker<Medicine>()
                    .RuleFor(m => m.Name, f => f.Commerce.ProductName())
                    .RuleFor(m => m.Category, f => f.PickRandom("Tablet", "Syrup", "Capsule", "Injection"))
                    .RuleFor(m => m.Stock, f => f.Random.Number(10, 500))
                    .RuleFor(m => m.Price, f => f.Random.Number(5, 500) * 1000);
                
                var medicines = medicineFaker.Generate(50);
                context.Medicines.AddRange(medicines);
                await context.SaveChangesAsync();

                var appointmentFaker = new Faker<Appointment>()
                    .RuleFor(a => a.PatientId, (f, a) => f.PickRandom(patients).Id)
                    .RuleFor(a => a.DoctorId, (f, a) => f.PickRandom(doctors).Id)
                    .RuleFor(a => a.AppointmentDate, f => f.Date.Recent(30))
                    .RuleFor(a => a.Status, f => f.PickRandom("Scheduled", "InProgress", "Completed", "Cancelled"))
                    .RuleFor(a => a.QueueNumber, f => f.Random.Number(1, 100))
                    .RuleFor(a => a.IsTelemedicine, f => f.Random.Bool(0.2f));

                var appointments = appointmentFaker.Generate(200);
                context.Appointments.AddRange(appointments);
                await context.SaveChangesAsync();

                var recordFaker = new Faker<MedicalRecord>()
                    .RuleFor(m => m.PatientId, (f, m) => f.PickRandom(patients).Id)
                    .RuleFor(m => m.DoctorId, (f, m) => f.PickRandom(doctors).Id)
                    .RuleFor(m => m.Date, f => f.Date.Recent(30))
                    .RuleFor(m => m.Diagnosis, f => f.Lorem.Sentence())
                    .RuleFor(m => m.Notes, f => f.Lorem.Paragraph())
                    .RuleFor(m => m.Treatment, f => f.Lorem.Sentence());
                
                var records = recordFaker.Generate(150);
                context.MedicalRecords.AddRange(records);
                await context.SaveChangesAsync();

                var presFaker = new Faker<Prescription>()
                    .RuleFor(p => p.MedicalRecordId, (f, p) => f.PickRandom(records).Id)
                    .RuleFor(p => p.MedicineId, (f, p) => f.PickRandom(medicines).Id)
                    .RuleFor(p => p.Quantity, f => f.Random.Number(1, 30))
                    .RuleFor(p => p.Dosage, f => f.PickRandom("3x1", "2x1", "1x1", "PRN"))
                    .RuleFor(p => p.Status, f => f.PickRandom("Pending", "Dispensed"));
                
                var prescriptions = presFaker.Generate(300);
                context.Prescriptions.AddRange(prescriptions);
                await context.SaveChangesAsync();

                var labFaker = new Faker<LabResult>()
                    .RuleFor(l => l.MedicalRecordId, (f, l) => f.PickRandom(records).Id)
                    .RuleFor(l => l.TestName, f => f.PickRandom("Blood Test", "Urinalysis", "X-Ray", "Cholesterol"))
                    .RuleFor(l => l.Result, f => f.Lorem.Sentence())
                    .RuleFor(l => l.Date, f => f.Date.Recent(30));

                var labResults = labFaker.Generate(100);
                context.LabResults.AddRange(labResults);
                await context.SaveChangesAsync();

                var billFaker = new Faker<Bill>()
                    .RuleFor(b => b.PatientId, (f, b) => f.PickRandom(patients).Id)
                    .RuleFor(b => b.TotalAmount, f => f.Random.Number(50, 2000) * 1000)
                    .RuleFor(b => b.IsPaid, f => f.Random.Bool(0.8f))
                    .RuleFor(b => b.Date, f => f.Date.Recent(30))
                    .RuleFor(b => b.PaymentMethod, f => f.PickRandom("Cash", "BPJS", "Transfer"))
                    .RuleFor(b => b.BpjsClaimStatus, f => f.PickRandom("None", "Pending", "Claimed"));

                var bills = billFaker.Generate(200);
                context.Bills.AddRange(bills);
                await context.SaveChangesAsync();
            }
        }
    }
}
