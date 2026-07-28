namespace VirtualDoctor.Models;

/// <summary>
/// Override konfigurasi yang disimpan di database.
/// appsettings.json = nilai awal, tabel ini = perubahan dari halaman Pengaturan.
/// Nilai di sini menang saat aplikasi start.
/// </summary>
public class AppSetting
{
    /// <summary>Path konfigurasi, contoh: "Llm:OpenAI:Model" atau "Meeting:Provider".</summary>
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public bool IsSecret { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
