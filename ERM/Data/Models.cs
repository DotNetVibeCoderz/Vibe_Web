using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System;

namespace ERM.Data
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string? DepartmentId { get; set; }
        public Department? Department { get; set; }
    }

    public class Department
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
        public ICollection<RiskRegister> Risks { get; set; } = new List<RiskRegister>();
    }

    public class Vendor
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string RiskLevel { get; set; } = "Medium"; // Low, Medium, High
        public DateTime LastEvaluation { get; set; } = DateTime.UtcNow;
    }

    public class RiskRegister
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = "Operational"; // Operational, Financial, Compliance, Cyber
        public int Probability { get; set; } = 3; // 1-5
        public int Impact { get; set; } = 3; // 1-5
        public int RiskScore => Probability * Impact;
        public string Status { get; set; } = "Open"; // Open, Mitigated, Closed
        public string? DepartmentId { get; set; }
        public Department? Department { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Incident
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime DateOccurred { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Reported"; // Reported, Investigating, Resolved
        public decimal FinancialImpact { get; set; }
    }

    public class Control
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = "Preventive"; // Preventive, Detective, Corrective
        public string Effectiveness { get; set; } = "Medium"; // High, Medium, Low
        public string? RiskRegisterId { get; set; }
        public RiskRegister? RiskRegister { get; set; }
    }

    public class ScenarioNotebook
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = "Financial"; // Operational, Financial, Compliance, Cyber
        public string FilePath { get; set; } = string.Empty; // Path to .csx in storage
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class BusinessContinuityPlan
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "Draft"; // Draft, Active, Archived
        public DateTime LastTested { get; set; } = DateTime.UtcNow;
        public string? DepartmentId { get; set; }
        public Department? Department { get; set; }
    }

    public class ComplianceFramework
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    public class AuditLog
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Action { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Details { get; set; } = string.Empty;
    }
}