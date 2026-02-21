using System;
using System.ComponentModel.DataAnnotations;

namespace MediaMonitoring.Models
{
    /// <summary>
    /// Model untuk menyimpan konfigurasi sistem secara dinamis.
    /// Menggantikan appsettings.json untuk nilai yang bisa diubah via UI.
    /// </summary>
    public class SystemConfiguration
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ConfigKey { get; set; } = string.Empty;

        [Required]
        public string ConfigValue { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public bool IsSensitive { get; set; } = false; // Untuk API Key, dll (disembunyikan di UI)

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}