using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ERM.Data;

namespace ERM.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            // Creates the database and schema if it doesn't exist
            context.Database.EnsureCreated();

            string[] roles = { 
                "admin", "Board of Directors", "Chief Risk Officer", 
                "Risk Manager", "Compliance Officer", "Internal Auditor", 
                "Department Manager", "Employee", "IT Security", "External Stakeholder" 
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Create admin user
            var adminEmail = "admin@erm.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = "admin",
                    Email = adminEmail,
                    FullName = "Administrator",
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(adminUser, "admin123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "admin");
                }
            }

            // Seed Master Data
            if (!context.Departments.Any())
            {
                context.Departments.AddRange(
                    new Department { Name = "IT", Description = "Information Technology" },
                    new Department { Name = "Finance", Description = "Financial Operations" },
                    new Department { Name = "HR", Description = "Human Resources" },
                    new Department { Name = "Operations", Description = "Daily Operations" }
                );
                await context.SaveChangesAsync();
            }

            if (!context.Vendors.Any())
            {
                context.Vendors.AddRange(
                    new Vendor { Name = "TechCorp", ContactPerson = "John Doe", RiskLevel = "High", LastEvaluation = DateTime.UtcNow.AddMonths(-1) },
                    new Vendor { Name = "SecureCloud", ContactPerson = "Jane Smith", RiskLevel = "Low", LastEvaluation = DateTime.UtcNow.AddDays(-15) },
                    new Vendor { Name = "AuditFirm", ContactPerson = "Mark Lee", RiskLevel = "Medium", LastEvaluation = DateTime.UtcNow.AddMonths(-3) }
                );
                await context.SaveChangesAsync();
            }

            if (!context.ComplianceFrameworks.Any())
            {
                context.ComplianceFrameworks.AddRange(
                    new ComplianceFramework { Name = "ISO 27001", Version = "2022", Description = "Information Security Management System" },
                    new ComplianceFramework { Name = "GDPR", Version = "v1", Description = "General Data Protection Regulation" },
                    new ComplianceFramework { Name = "SOX", Version = "2002", Description = "Sarbanes-Oxley Act for Financial Record Keeping" }
                );
                await context.SaveChangesAsync();
            }

            // Seed Risks
            if (!context.RiskRegisters.Any())
            {
                var itDept = context.Departments.FirstOrDefault(d => d.Name == "IT");
                var finDept = context.Departments.FirstOrDefault(d => d.Name == "Finance");

                context.RiskRegisters.AddRange(
                    new RiskRegister { Title = "Data Breach", Category = "Cyber", Description = "Hackers access customer data", Impact = 5, Probability = 3, Status = "Open", Department = itDept },
                    new RiskRegister { Title = "Ransomware Attack", Category = "Cyber", Description = "Malware encrypts critical systems", Impact = 5, Probability = 4, Status = "Mitigated", Department = itDept },
                    new RiskRegister { Title = "Currency Fluctuation", Category = "Financial", Description = "Losses due to USD exchange rate", Impact = 3, Probability = 4, Status = "Open", Department = finDept },
                    new RiskRegister { Title = "Regulatory Fine", Category = "Compliance", Description = "GDPR non-compliance fine", Impact = 4, Probability = 2, Status = "Closed", Department = itDept }
                );
                await context.SaveChangesAsync();
            }

            // Seed Incidents
            if (!context.Incidents.Any())
            {
                context.Incidents.AddRange(
                    new Incident { Title = "Server Outage", Description = "Main DB server down for 2 hours", DateOccurred = DateTime.UtcNow.AddDays(-5), Status = "Resolved", FinancialImpact = 5000 },
                    new Incident { Title = "Phishing Email", Description = "Employee clicked on malicious link", DateOccurred = DateTime.UtcNow.AddDays(-2), Status = "Investigating", FinancialImpact = 0 }
                );
                await context.SaveChangesAsync();
            }
        }
    }
}