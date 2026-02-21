using System;
using System.ComponentModel.DataAnnotations;

namespace MediaMonitoring.Models
{
    /// <summary>
    /// Model untuk menyimpan data postingan media yang dikumpulkan (OSINT).
    /// Merepresentasikan satu entitas data dari berbagai sumber (Twitter, News, Blog, dll).
    /// </summary>
    public class MediaPost
    {
        [Key]
        public int Id { get; set; }

        // Sumber data (Twitter, Facebook, CNN, Detik, dll)
        [Required]
        public string Source { get; set; } = string.Empty;

        // Isi konten (teks)
        public string Content { get; set; } = string.Empty;

        // URL asli postingan
        public string Url { get; set; } = string.Empty;

        // Penulis/Akun
        public string Author { get; set; } = string.Empty;

        // Waktu posting (dari sumber)
        public DateTime PostedAt { get; set; }

        // Waktu dimasukkan ke sistem
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Sentimen: Positif, Negatif, Netral
        public string Sentiment { get; set; } = "Neutral";

        // Skor sentimen (-1.0 s/d 1.0)
        public double SentimentScore { get; set; } = 0.0;

        // Kategori (Politik, Ekonomi, Keamanan, Teknologi, dll)
        public string Category { get; set; } = "General";

        // Lokasi (jika ada)
        public string Location { get; set; } = string.Empty;

        // Bahasa terdeteksi
        public string Language { get; set; } = "id";

        // Metadata tambahan (JSON string untuk fleksibilitas)
        public string? MetadataJson { get; set; }

        // Apakah sudah diproses/dianalisis
        public bool IsProcessed { get; set; } = false;

        // Tag/Keyword yang terdeteksi
        public string? Tags { get; set; }
    }
}