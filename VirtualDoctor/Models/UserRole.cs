namespace VirtualDoctor.Models;

/// <summary>
/// Peran yang dikenal aplikasi. Dipakai sebagai claim otorisasi.
/// </summary>
public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Doctor = "Doctor";
    public const string Patient = "Patient";

    public static readonly string[] All = { Admin, Doctor, Patient };

    public static string Label(string role) => role switch
    {
        Admin => "Administrator",
        Doctor => "Dokter",
        Patient => "Pasien",
        _ => role
    };
}

/// <summary>
/// Pemberian peran kepada pengguna. Satu pengguna dapat memiliki lebih dari satu peran.
/// Menggantikan penentuan admin lewat perbandingan alamat email.
/// </summary>
public class UserRole
{
    public string UserId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
    public string? GrantedBy { get; set; }

    public ApplicationUser User { get; set; } = null!;
}
