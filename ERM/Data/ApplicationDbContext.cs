using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ERM.Data;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Linq;

namespace ERM.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor httpContextAccessor = null)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public DbSet<Department> Departments { get; set; }
        public DbSet<Vendor> Vendors { get; set; }
        public DbSet<RiskRegister> RiskRegisters { get; set; }
        public DbSet<Incident> Incidents { get; set; }
        public DbSet<Control> Controls { get; set; }
        public DbSet<ScenarioNotebook> ScenarioNotebooks { get; set; }
        public DbSet<BusinessContinuityPlan> BusinessContinuityPlans { get; set; }
        public DbSet<ComplianceFramework> ComplianceFrameworks { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker.Entries().ToList();
            var userId = _httpContextAccessor?.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System";

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added || entry.State == EntityState.Modified || entry.State == EntityState.Deleted)
                {
                    if (entry.Entity is AuditLog) continue;

                    string entityId = "N/A";
                    var idProperty = entry.Entity.GetType().GetProperty("Id");
                    if (idProperty != null)
                    {
                        entityId = idProperty.GetValue(entry.Entity)?.ToString() ?? "N/A";
                    }

                    AuditLogs.Add(new AuditLog
                    {
                        Action = entry.State.ToString(),
                        EntityName = entry.Entity.GetType().Name,
                        EntityId = entityId,
                        UserId = userId,
                        Timestamp = DateTime.UtcNow,
                        Details = $"Record {entry.State} on {DateTime.UtcNow}"
                    });
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}