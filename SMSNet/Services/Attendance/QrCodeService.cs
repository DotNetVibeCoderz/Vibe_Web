using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using SMSNet.Data;
using SMSNet.Models;

namespace SMSNet.Services.Attendance;

/// <summary>Who a scanned code belongs to.</summary>
public sealed record ScanTarget(int Id, string Name, string Role, string? Detail, string QrCode);

/// <summary>
/// Issues and reads the codes printed on student and teacher cards.
/// <para>
/// Codes are stored on the person rather than derived from their id, so a lost card
/// can be reissued with a fresh code while the old one immediately stops working.
/// The random suffix also stops anyone guessing a colleague's code from their own.
/// </para>
/// </summary>
public sealed class QrCodeService
{
    private const string StudentPrefix = "SIS";
    private const string TeacherPrefix = "GUR";

    /// <summary>Excludes I, O, 0, and 1 — they are read back wrongly when typed by hand.</summary>
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

    public QrCodeService(IDbContextFactory<ApplicationDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    // --- Rendering ---------------------------------------------------------

    /// <summary>
    /// The QR as an inline SVG data URI. SVG keeps the code crisp at any print size,
    /// and a data URI means a printed page needs no extra HTTP request.
    /// </summary>
    public string ToSvgDataUri(string payload, int pixelsPerModule = 4)
    {
        var svg = ToSvg(payload, pixelsPerModule);
        return "data:image/svg+xml;base64," + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(svg));
    }

    public string ToSvg(string payload, int pixelsPerModule = 4)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            payload = "-";
        }

        using var generator = new QRCodeGenerator();
        // Q recovery survives a scuffed or partly covered card — these get worn in pockets.
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        return new SvgQRCode(data).GetGraphic(pixelsPerModule, "#101a2e", "#ffffff", true);
    }

    // --- Issuing -----------------------------------------------------------

    /// <summary>
    /// Gives a code to everyone who lacks one and returns how many were issued.
    /// <para>
    /// Run on demand rather than at startup: existing installs already hold students
    /// seeded before this feature existed, and forcing a database reset on them would
    /// be worse than a one-click backfill.
    /// </para>
    /// </summary>
    public async Task<(int Students, int Teachers)> BackfillAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var taken = new HashSet<string>(
            (await db.Students.Where(s => s.QrCode != null).Select(s => s.QrCode!).ToListAsync(ct))
            .Concat(await db.Teachers.Where(t => t.QrCode != null).Select(t => t.QrCode!).ToListAsync(ct)),
            StringComparer.OrdinalIgnoreCase);

        var students = await db.Students.Where(s => s.QrCode == null).ToListAsync(ct);
        foreach (var student in students)
        {
            student.QrCode = NextUnique(StudentPrefix, student.Id, taken);
        }

        var teachers = await db.Teachers.Where(t => t.QrCode == null).ToListAsync(ct);
        foreach (var teacher in teachers)
        {
            teacher.QrCode = NextUnique(TeacherPrefix, teacher.Id, taken);
        }

        if (students.Count > 0 || teachers.Count > 0)
        {
            await db.SaveChangesAsync(ct);
        }

        return (students.Count, teachers.Count);
    }

    /// <summary>Issues a fresh code, invalidating the card that carried the old one.</summary>
    public async Task<string?> ReissueStudentAsync(int studentId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var student = await db.Students.FirstOrDefaultAsync(s => s.Id == studentId, ct);
        if (student is null)
        {
            return null;
        }

        var taken = await AllCodesAsync(db, ct);
        student.QrCode = NextUnique(StudentPrefix, student.Id, taken);
        await db.SaveChangesAsync(ct);

        return student.QrCode;
    }

    public async Task<string?> ReissueTeacherAsync(int teacherId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var teacher = await db.Teachers.FirstOrDefaultAsync(t => t.Id == teacherId, ct);
        if (teacher is null)
        {
            return null;
        }

        var taken = await AllCodesAsync(db, ct);
        teacher.QrCode = NextUnique(TeacherPrefix, teacher.Id, taken);
        await db.SaveChangesAsync(ct);

        return teacher.QrCode;
    }

    // --- Reading -----------------------------------------------------------

    /// <summary>
    /// Resolves a scanned or typed code to a person, or null when it matches nobody.
    /// <para>
    /// Input is normalised first: hardware scanners often append whitespace, and a
    /// person typing the code will use lower case and may add the separators they see
    /// printed on the card.
    /// </para>
    /// </summary>
    public async Task<ScanTarget?> ResolveAsync(string? scanned, CancellationToken ct = default)
    {
        var code = Normalise(scanned);
        if (code.Length == 0)
        {
            return null;
        }

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var student = await db.Students.AsNoTracking()
            .FirstOrDefaultAsync(s => s.QrCode == code, ct);

        if (student is not null)
        {
            return new ScanTarget(student.Id, student.FullName, "Siswa",
                student.ClassName, student.QrCode!);
        }

        var teacher = await db.Teachers.AsNoTracking()
            .FirstOrDefaultAsync(t => t.QrCode == code, ct);

        if (teacher is not null)
        {
            return new ScanTarget(teacher.Id, teacher.FullName, "Guru",
                teacher.Subject, teacher.QrCode!);
        }

        return null;
    }

    /// <summary>
    /// Uppercases, strips whitespace, and drops separators so a code typed as
    /// "sis 000007 k4m9" matches the stored "SIS-000007-K4M9".
    /// </summary>
    public static string Normalise(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var cleaned = new string(raw
            .Where(c => char.IsLetterOrDigit(c))
            .ToArray())
            .ToUpperInvariant();

        if (cleaned.Length < 4)
        {
            return string.Empty;
        }

        // Re-insert the separators the stored form uses: PREFIX-000000-XXXX
        return cleaned.Length >= 13
            ? $"{cleaned[..3]}-{cleaned[3..9]}-{cleaned[9..13]}"
            : cleaned;
    }

    // --- Helpers -----------------------------------------------------------

    private static async Task<HashSet<string>> AllCodesAsync(ApplicationDbContext db, CancellationToken ct) =>
        new(
            (await db.Students.Where(s => s.QrCode != null).Select(s => s.QrCode!).ToListAsync(ct))
            .Concat(await db.Teachers.Where(t => t.QrCode != null).Select(t => t.QrCode!).ToListAsync(ct)),
            StringComparer.OrdinalIgnoreCase);

    private static string NextUnique(string prefix, int id, HashSet<string> taken)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            var candidate = $"{prefix}-{id:000000}-{RandomSuffix(4)}";
            if (taken.Add(candidate))
            {
                return candidate;
            }
        }

        // 32^4 collisions in a row is not credible; the timestamp is a last resort.
        return $"{prefix}-{id:000000}-{DateTime.UtcNow.Ticks % 10000:0000}";
    }

    private static string RandomSuffix(int length)
    {
        var chars = new char[length];
        for (var i = 0; i < length; i++)
        {
            chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }
        return new string(chars);
    }
}
