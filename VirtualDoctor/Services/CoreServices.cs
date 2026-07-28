using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using VirtualDoctor.Data;
using VirtualDoctor.Models;

namespace VirtualDoctor.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _http;

    public AuthService(AppDbContext db, IHttpContextAccessor http)
    { _db = db; _http = http; }

    // ============ Peran ============
    // Otorisasi dibaca dari claim yang dibuat saat login, bukan dari alamat email.

    public bool IsAdmin() => _http.HttpContext?.User.IsInRole(AppRoles.Admin) ?? false;

    public bool IsDoctor() => _http.HttpContext?.User.IsInRole(AppRoles.Doctor) ?? false;

    public string? GetDoctorId() => _http.HttpContext?.User.FindFirst(AuthClaims.DoctorIdClaim)?.Value;

    public async Task<IReadOnlyList<string>> GetRolesAsync(string userId) =>
        await _db.UserRoles.Where(r => r.UserId == userId).Select(r => r.Role).ToListAsync();

    public async Task<Dictionary<string, List<string>>> GetRolesForAllAsync() =>
        await _db.UserRoles.AsNoTracking()
            .GroupBy(r => r.UserId)
            .ToDictionaryAsync(g => g.Key, g => g.Select(r => r.Role).ToList());

    public async Task<bool> SetRolesAsync(string userId, IEnumerable<string> roles, string? grantedBy = null)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return false;

        var wanted = roles.Where(r => AppRoles.All.Contains(r)).Distinct().ToList();

        // Cegah penghapusan administrator terakhir agar sistem tidak terkunci.
        if (!wanted.Contains(AppRoles.Admin))
        {
            var isCurrentlyAdmin = await _db.UserRoles.AnyAsync(r => r.UserId == userId && r.Role == AppRoles.Admin);
            if (isCurrentlyAdmin)
            {
                var adminCount = await _db.UserRoles.CountAsync(r => r.Role == AppRoles.Admin);
                if (adminCount <= 1) return false;
            }
        }

        var existing = await _db.UserRoles.Where(r => r.UserId == userId).ToListAsync();
        _db.UserRoles.RemoveRange(existing.Where(r => !wanted.Contains(r.Role)));

        foreach (var role in wanted.Where(r => existing.All(e => e.Role != r)))
        {
            _db.UserRoles.Add(new UserRole
            {
                UserId = userId,
                Role = role,
                GrantedAt = DateTime.UtcNow,
                GrantedBy = grantedBy
            });
        }

        // Jaga agar penanda dokter pada profil tetap selaras dengan perannya.
        user.IsDoctor = wanted.Contains(AppRoles.Doctor);

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<int> CountAdminsAsync() =>
        await _db.UserRoles.CountAsync(r => r.Role == AppRoles.Admin);

    // ============ Registrasi & login ============

    public async Task<bool> RegisterAsync(string email, string password, string fullName)
    {
        var normalizedEmail = AuthHelpers.NormalizeEmail(email);
        if (!AuthHelpers.IsValidEmail(normalizedEmail)) return false;
        if (string.IsNullOrWhiteSpace(fullName)) return false;
        if (AuthHelpers.ValidatePasswordRules(password) != null) return false;
        if (await _db.Users.AnyAsync(u => u.Email == normalizedEmail)) return false;

        var user = new ApplicationUser
        {
            Email = normalizedEmail,
            FullName = fullName.Trim(),
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        await StorePasswordHashAsync(user.Id, password);

        _db.UserRoles.Add(new UserRole { UserId = user.Id, Role = AppRoles.Patient });
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> LoginAsync(string email, string password)
    {
        var normalizedEmail = AuthHelpers.NormalizeEmail(email);
        if (!AuthHelpers.IsValidEmail(normalizedEmail) || string.IsNullOrWhiteSpace(password)) return false;

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail && u.IsActive);
        if (user == null) return false;

        var storedHash = await GetStoredPasswordHashAsync(user.Id);
        var check = AuthHelpers.VerifyPassword(storedHash, password);
        if (check == PasswordCheck.Failed) return false;

        // Hash lama ditulis ulang dengan algoritma baru begitu sandi terbukti benar.
        if (check == PasswordCheck.SuccessNeedsRehash)
            await StorePasswordHashAsync(user.Id, password);

        var principal = await AuthClaims.BuildAsync(_db, user);

        var ctx = _http.HttpContext;
        if (ctx == null) return false;

        // Cookie persistent - tetap login walaupun browser ditutup
        await ctx.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7),
                AllowRefresh = true
            });

        return true;
    }

    public async Task LogoutAsync()
    {
        var ctx = _http.HttpContext;
        if (ctx != null)
            await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    public async Task<bool> ResetPasswordAsync(string email)
    {
        var normalizedEmail = AuthHelpers.NormalizeEmail(email);
        if (!AuthHelpers.IsValidEmail(normalizedEmail)) return false;

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail && u.IsActive);
        if (user == null) return false;
        await StorePasswordHashAsync(user.Id, "Reset123!");
        return true;
    }

    public async Task<bool> ChangePasswordAsync(string userId, string oldPassword, string newPassword)
    {
        var storedHash = await GetStoredPasswordHashAsync(userId);
        if (AuthHelpers.VerifyPassword(storedHash, oldPassword) == PasswordCheck.Failed) return false;
        if (AuthHelpers.ValidatePasswordRules(newPassword) != null) return false;
        await StorePasswordHashAsync(userId, newPassword);
        return true;
    }

    public ClaimsPrincipal? GetCurrentUser() => _http.HttpContext?.User;
    public string? GetCurrentUserId() => _http.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return null;
        return await _db.Users.FindAsync(userId);
    }

    // ============ Password helpers ============

    private async Task StorePasswordHashAsync(string userId, string password)
    {
        var hash = AuthHelpers.HashPassword(password);
        var existing = await _db.Set<PasswordHash>().FirstOrDefaultAsync(p => p.UserId == userId);
        if (existing != null) existing.Hash = hash;
        else _db.Set<PasswordHash>().Add(new PasswordHash { UserId = userId, Hash = hash });
        await _db.SaveChangesAsync();
    }

    private async Task<string?> GetStoredPasswordHashAsync(string userId)
    {
        var record = await _db.Set<PasswordHash>().FirstOrDefaultAsync(p => p.UserId == userId);
        return record?.Hash;
    }
}

public class PasswordHash
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
}

public class UserService : IUserService
{
    private readonly AppDbContext _db;
    public UserService(AppDbContext db) => _db = db;
    public async Task<ApplicationUser?> GetByIdAsync(string id) => await _db.Users.FindAsync(id);
    public async Task<ApplicationUser?> GetByEmailAsync(string email)
    {
        var normalizedEmail = AuthHelpers.NormalizeEmail(email);
        return await _db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);
    }
    public async Task<bool> UpdateProfileAsync(ApplicationUser user) { _db.Users.Update(user); return await _db.SaveChangesAsync() > 0; }
    public async Task<bool> DeleteAsync(string id) { var u = await _db.Users.FindAsync(id); if (u == null) return false; u.IsActive = false; return await _db.SaveChangesAsync() > 0; }
    public async Task<List<ApplicationUser>> GetAllAsync() => await _db.Users.ToListAsync();

    public async Task<ApplicationUser?> CreateAsync(ApplicationUser user, string password)
    {
        if (await _db.Users.AnyAsync(u => u.Email == user.Email)) return null;
        user.Email = AuthHelpers.NormalizeEmail(user.Email);
        user.IsActive = true;
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var hash = AuthHelpers.HashPassword(password);
        _db.Set<PasswordHash>().Add(new PasswordHash { UserId = user.Id, Hash = hash });

        // Pengguna baru dari backoffice tetap mendapat peran dasar.
        _db.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            Role = user.IsDoctor ? AppRoles.Doctor : AppRoles.Patient
        });

        await _db.SaveChangesAsync();
        return user;
    }

    public async Task<bool> UpdateAsync(ApplicationUser user)
    {
        _db.Users.Update(user);
        return await _db.SaveChangesAsync() > 0;
    }
}

public class DoctorService : IDoctorService
{
    private readonly AppDbContext _db;
    public DoctorService(AppDbContext db) => _db = db;
    public async Task<List<Doctor>> GetAllAsync() => await _db.Doctors.ToListAsync();
    public async Task<List<Doctor>> GetBySpecializationAsync(string s) => await _db.Doctors.Where(d => d.Specialization == s).ToListAsync();
    public async Task<Doctor?> GetByIdAsync(string id) => await _db.Doctors.FindAsync(id);
    public async Task<Doctor?> GetByIdWithScheduleAsync(string id) => await _db.Doctors.Include(d => d.Schedules).FirstOrDefaultAsync(d => d.Id == id);
    public async Task<List<Doctor>> SearchAsync(string q) => await _db.Doctors.Where(d => d.FullName.Contains(q) || d.Specialization.Contains(q) || d.About!.Contains(q)).ToListAsync();

    public async Task<Doctor> CreateAsync(Doctor doctor)
    {
        _db.Doctors.Add(doctor);
        await _db.SaveChangesAsync();
        return doctor;
    }

    public async Task<Doctor?> UpdateAsync(Doctor doctor)
    {
        var existing = await _db.Doctors.FindAsync(doctor.Id);
        if (existing == null) return null;
        _db.Entry(existing).CurrentValues.SetValues(doctor);
        await _db.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var doc = await _db.Doctors.FindAsync(id);
        if (doc == null) return false;
        doc.IsAvailable = false;
        doc.IsOnline = false;
        await _db.SaveChangesAsync();
        return true;
    }
}

public class MedicineService : IMedicineService
{
    private readonly AppDbContext _db;
    public MedicineService(AppDbContext db) => _db = db;
    public async Task<List<Medicine>> GetAllAsync() => await _db.Medicines.Where(m => m.IsActive).ToListAsync();
    public async Task<List<Medicine>> GetByCategoryAsync(string c) => await _db.Medicines.Where(m => m.Category == c && m.IsActive).ToListAsync();
    public async Task<Medicine?> GetByIdAsync(string id) => await _db.Medicines.FindAsync(id);
    public async Task<List<Medicine>> SearchAsync(string q) => await _db.Medicines.Where(m => m.Name.Contains(q) || m.Description!.Contains(q)).ToListAsync();
    public async Task<List<Medicine>> GetRecommendationsAsync(string uid) => await _db.Medicines.Where(m => m.IsActive).OrderByDescending(m => m.Rating).Take(10).ToListAsync();

    public async Task<Medicine> CreateAsync(Medicine m)
    {
        _db.Medicines.Add(m);
        await _db.SaveChangesAsync();
        return m;
    }

    public async Task<Medicine?> UpdateAsync(Medicine m)
    {
        var existing = await _db.Medicines.FindAsync(m.Id);
        if (existing == null) return null;
        _db.Entry(existing).CurrentValues.SetValues(m);
        await _db.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var med = await _db.Medicines.FindAsync(id);
        if (med == null) return false;
        med.IsActive = false;
        await _db.SaveChangesAsync();
        return true;
    }
}

public class HospitalService : IHospitalService
{
    private readonly AppDbContext _db;
    public HospitalService(AppDbContext db) => _db = db;
    public async Task<List<Hospital>> GetAllAsync() => await _db.Hospitals.Where(h => h.IsActive).ToListAsync();
    public async Task<List<Hospital>> GetByTypeAsync(HospitalType t) => await _db.Hospitals.Where(h => h.Type == t && h.IsActive).ToListAsync();
    public async Task<Hospital?> GetByIdAsync(string id) => await _db.Hospitals.FindAsync(id);
    public async Task<List<Hospital>> FindNearestAsync(double lat, double lng, double r = 10)
    {
        var all = await _db.Hospitals.Where(h => h.IsActive).ToListAsync();
        return all.Where(h => CalcDist(lat, lng, h.Latitude, h.Longitude) <= r).OrderBy(h => CalcDist(lat, lng, h.Latitude, h.Longitude)).Take(20).ToList();
    }
    public async Task<List<Hospital>> SearchAsync(string q) => await _db.Hospitals.Where(h => h.Name.Contains(q) || h.City!.Contains(q) || h.Address!.Contains(q)).ToListAsync();

    public async Task<Hospital> CreateAsync(Hospital h)
    {
        _db.Hospitals.Add(h);
        await _db.SaveChangesAsync();
        return h;
    }

    public async Task<Hospital?> UpdateAsync(Hospital h)
    {
        var existing = await _db.Hospitals.FindAsync(h.Id);
        if (existing == null) return null;
        _db.Entry(existing).CurrentValues.SetValues(h);
        await _db.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var hosp = await _db.Hospitals.FindAsync(id);
        if (hosp == null) return false;
        hosp.IsActive = false;
        await _db.SaveChangesAsync();
        return true;
    }

    private static double CalcDist(double lat1, double lng1, double lat2, double lng2) { const double R = 6371; var dLat = (lat2 - lat1) * Math.PI / 180; var dLng = (lat2 - lat1) * Math.PI / 180; var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(lat1 * Math.PI / 180) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2); return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a)); }
}

public class AppointmentService : IAppointmentService
{
    private readonly AppDbContext _db;
    public AppointmentService(AppDbContext db) => _db = db;
    public async Task<Appointment?> BookAsync(Appointment a) { _db.Appointments.Add(a); return await _db.SaveChangesAsync() > 0 ? a : null; }
    public async Task<List<Appointment>> GetUserAppointmentsAsync(string uid) => await _db.Appointments.Include(a => a.Doctor).Include(a => a.Hospital).Where(a => a.UserId == uid).OrderByDescending(a => a.AppointmentDate).ToListAsync();
    public async Task<List<Appointment>> GetDoctorAppointmentsAsync(string did) => await _db.Appointments.Include(a => a.User).Include(a => a.Hospital).Where(a => a.DoctorId == did).OrderByDescending(a => a.AppointmentDate).ToListAsync();
    public async Task<bool> UpdateStatusAsync(string id, AppointmentStatus s) { var a = await _db.Appointments.FindAsync(id); if (a == null) return false; a.Status = s; return await _db.SaveChangesAsync() > 0; }
    public async Task<bool> CancelAsync(string id) { var a = await _db.Appointments.FindAsync(id); if (a == null) return false; a.Status = AppointmentStatus.Cancelled; return await _db.SaveChangesAsync() > 0; }
    public async Task<List<DoctorSchedule>> GetDoctorScheduleAsync(string did) => await _db.DoctorSchedules.Where(s => s.DoctorId == did && s.IsActive).ToListAsync();

    public async Task<List<Appointment>> GetAllAsync()
        => await _db.Appointments.Include(a => a.User).Include(a => a.Doctor).Include(a => a.Hospital).OrderByDescending(a => a.CreatedAt).ToListAsync();

    public async Task<Appointment?> GetByIdAsync(string id)
        => await _db.Appointments.Include(a => a.User).Include(a => a.Doctor).Include(a => a.Hospital).FirstOrDefaultAsync(a => a.Id == id);

    public async Task<Appointment?> CreateAsync(Appointment a)
    {
        _db.Appointments.Add(a);
        await _db.SaveChangesAsync();
        return a;
    }

    public async Task<Appointment?> UpdateAsync(Appointment a)
    {
        var existing = await _db.Appointments.FindAsync(a.Id);
        if (existing == null) return null;
        _db.Entry(existing).CurrentValues.SetValues(a);
        await _db.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var app = await _db.Appointments.FindAsync(id);
        if (app == null) return false;
        app.Status = AppointmentStatus.Cancelled;
        await _db.SaveChangesAsync();
        return true;
    }
}

public class DoctorScheduleService : IDoctorScheduleService
{
    private readonly AppDbContext _db;
    public DoctorScheduleService(AppDbContext db) => _db = db;

    public async Task<List<DoctorSchedule>> GetAllAsync() => await _db.DoctorSchedules.Include(s => s.Doctor).ToListAsync();
    public async Task<DoctorSchedule?> GetByIdAsync(string id) => await _db.DoctorSchedules.Include(s => s.Doctor).FirstOrDefaultAsync(s => s.Id == id);
    public async Task<List<DoctorSchedule>> GetByDoctorAsync(string doctorId) => await _db.DoctorSchedules.Where(s => s.DoctorId == doctorId).ToListAsync();

    public async Task<DoctorSchedule> CreateAsync(DoctorSchedule s)
    {
        _db.DoctorSchedules.Add(s);
        await _db.SaveChangesAsync();
        return s;
    }

    public async Task<DoctorSchedule?> UpdateAsync(DoctorSchedule s)
    {
        var existing = await _db.DoctorSchedules.FindAsync(s.Id);
        if (existing == null) return null;
        _db.Entry(existing).CurrentValues.SetValues(s);
        await _db.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var sched = await _db.DoctorSchedules.FindAsync(id);
        if (sched == null) return false;
        _db.DoctorSchedules.Remove(sched);
        await _db.SaveChangesAsync();
        return true;
    }
}
