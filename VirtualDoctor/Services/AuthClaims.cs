using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using VirtualDoctor.Data;
using VirtualDoctor.Models;

namespace VirtualDoctor.Services;

/// <summary>
/// Satu tempat pembentukan identitas login. Halaman login (minimal API di Program.cs)
/// dan <see cref="AuthService"/> sama-sama memakai kelas ini agar isi claim tidak
/// pernah berbeda antar jalur masuk.
/// </summary>
public static class AuthClaims
{
    public const string DoctorIdClaim = "vd:doctorId";

    public static async Task<ClaimsPrincipal> BuildAsync(AppDbContext db, ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName)
        };

        var roles = await db.UserRoles
            .Where(r => r.UserId == user.Id)
            .Select(r => r.Role)
            .ToListAsync();

        // Setiap akun aktif minimal berperan sebagai pasien.
        if (roles.Count == 0) roles.Add(AppRoles.Patient);

        foreach (var role in roles.Distinct())
            claims.Add(new Claim(ClaimTypes.Role, role));

        if (!string.IsNullOrEmpty(user.DoctorId))
            claims.Add(new Claim(DoctorIdClaim, user.DoctorId));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }
}
