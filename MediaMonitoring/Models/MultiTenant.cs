using System.ComponentModel.DataAnnotations;

namespace MediaMonitoring.Models
{
    /// <summary>
    /// Model untuk dukungan multi-tenant (organisasi/perusahaan berbeda)
    /// </summary>
    public class Tenant
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Subdomain { get; set; } = string.Empty;

        public string ApiKey { get; set; } = string.Empty;

        public string Plan { get; set; } = "Free";

        public int MaxUsers { get; set; } = 5;

        public int MaxPosts { get; set; } = 1000;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? SubscriptionEnds { get; set; }
    }

    /// <summary>
    /// Interface untuk entity yang mendukung multi-tenant
    /// </summary>
    public interface ITenantEntity
    {
        int TenantId { get; set; }
    }

    /// <summary>
    /// Extension methods untuk query multi-tenant
    /// </summary>
    public static class TenantExtensions
    {
        public static int? CurrentTenantId { get; set; }

        public static IQueryable<T> ForTenant<T>(this IQueryable<T> query, int tenantId) 
            where T : ITenantEntity
        {
            return query.Where(e => e.TenantId == tenantId);
        }
    }
}