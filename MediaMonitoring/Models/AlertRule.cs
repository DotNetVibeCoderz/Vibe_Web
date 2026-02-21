using System;
using System.ComponentModel.DataAnnotations;

namespace MediaMonitoring.Models
{
    /// <summary>
    /// Aturan untuk memicu alert/notifikasi jika keyword terdeteksi.
    /// </summary>
    public class AlertRule
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Keyword { get; set; } = string.Empty;

        public string Severity { get; set; } = "Medium"; // Low, Medium, High, Critical

        public bool IsActive { get; set; } = true;

        public string NotificationEmail { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int TriggerCount { get; set; } = 0; // Berapa kali aturan ini terpicu
    }
}