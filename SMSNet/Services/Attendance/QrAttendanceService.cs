using Microsoft.EntityFrameworkCore;
using SMSNet.Data;
using SMSNet.Models;

namespace SMSNet.Services.Attendance;

public enum ScanOutcome
{
    /// <summary>Attendance was recorded.</summary>
    Recorded,
    /// <summary>Already scanned today — the earlier time is reported back.</summary>
    AlreadyPresent,
    /// <summary>The code matched nobody.</summary>
    Unknown,
    /// <summary>Nothing usable was scanned or typed.</summary>
    Empty
}

public sealed record ScanResult(
    ScanOutcome Outcome,
    string Message,
    ScanTarget? Target = null,
    DateTime? RecordedAt = null);

/// <summary>One person's attendance for the day, as shown in the live list.</summary>
public sealed record TodayEntry(
    int RecordId,
    string Name,
    string Role,
    DateTime Time,
    string Status,
    string Method);

/// <summary>
/// Turns a scanned card into an attendance record.
/// <para>
/// Scans arrive fast — a queue of students tapping cards at the gate — so each call
/// opens its own short-lived context rather than sharing one across the circuit.
/// </para>
/// </summary>
public sealed class QrAttendanceService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly QrCodeService _qr;
    private readonly AuditService _audit;

    public QrAttendanceService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        QrCodeService qr,
        AuditService audit)
    {
        _dbFactory = dbFactory;
        _qr = qr;
        _audit = audit;
    }

    /// <summary>
    /// Records attendance for whoever the code belongs to.
    /// <para>
    /// A second scan on the same day does not create a duplicate row — it reports the
    /// time already on file. Cards get double-tapped constantly at a school gate, and
    /// silently stacking rows would corrupt every attendance percentage in the app.
    /// </para>
    /// </summary>
    public async Task<ScanResult> RecordAsync(
        string? scannedCode,
        string status = "Present",
        CancellationToken ct = default)
    {
        var target = await _qr.ResolveAsync(scannedCode, ct);

        if (QrCodeService.Normalise(scannedCode).Length == 0)
        {
            return new ScanResult(ScanOutcome.Empty, "Kode kosong. Pindai kartu atau ketik kodenya.");
        }

        if (target is null)
        {
            return new ScanResult(ScanOutcome.Unknown,
                $"Kode \"{QrCodeService.Normalise(scannedCode)}\" tidak dikenali. " +
                "Pastikan kartu sudah diterbitkan dari halaman Kartu Siswa.");
        }

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var today = SchoolClock.Today;
        var tomorrow = today.AddDays(1);

        var existing = await db.AttendanceRecords
            .Where(a => a.PersonName == target.Name && a.Date >= today && a.Date < tomorrow)
            .OrderBy(a => a.Date)
            .FirstOrDefaultAsync(ct);

        if (existing is not null)
        {
            return new ScanResult(ScanOutcome.AlreadyPresent,
                $"{target.Name} sudah tercatat hadir pukul {existing.Date:HH:mm}.",
                target, existing.Date);
        }

        var now = SchoolClock.LocalNow;

        db.AttendanceRecords.Add(new AttendanceRecord
        {
            PersonName = target.Name,
            Role = target.Role,
            Date = now,          // full timestamp — the list shows the time of arrival
            Status = status,
            Method = "QR"
        });

        await db.SaveChangesAsync(ct);

        await _audit.WriteAsync("Absensi QR",
            $"{target.Name} ({target.Role}) pukul {now:HH:mm} · {target.QrCode}");

        return new ScanResult(ScanOutcome.Recorded,
            $"{target.Name} tercatat hadir pukul {now:HH:mm}.", target, now);
    }

    /// <summary>Everyone recorded today, newest first, optionally filtered.</summary>
    public async Task<List<TodayEntry>> GetTodayAsync(
        string? search = null,
        string? role = null,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var today = SchoolClock.Today;
        var tomorrow = today.AddDays(1);

        var query = db.AttendanceRecords.AsNoTracking()
            .Where(a => a.Date >= today && a.Date < tomorrow);

        if (!string.IsNullOrWhiteSpace(role))
        {
            query = query.Where(a => a.Role == role);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(a => a.PersonName.Contains(search));
        }

        return await query
            .OrderByDescending(a => a.Date)
            .Select(a => new TodayEntry(a.Id, a.PersonName, a.Role, a.Date, a.Status, a.Method))
            .ToListAsync(ct);
    }

    /// <summary>Headline counts for the scanning screen.</summary>
    public async Task<(int Total, int Students, int Teachers, int ViaQr)> GetTodayCountsAsync(
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var today = SchoolClock.Today;
        var tomorrow = today.AddDays(1);
        var scoped = db.AttendanceRecords.Where(a => a.Date >= today && a.Date < tomorrow);

        return (
            await scoped.CountAsync(ct),
            await scoped.CountAsync(a => a.Role == "Siswa", ct),
            await scoped.CountAsync(a => a.Role == "Guru", ct),
            await scoped.CountAsync(a => a.Method == "QR", ct));
    }

    /// <summary>Undoes a scan — for a card tapped by the wrong person.</summary>
    public async Task<bool> RemoveAsync(int recordId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var record = await db.AttendanceRecords.FirstOrDefaultAsync(a => a.Id == recordId, ct);
        if (record is null)
        {
            return false;
        }

        db.AttendanceRecords.Remove(record);
        await db.SaveChangesAsync(ct);

        await _audit.WriteAsync("Batalkan absensi QR", $"{record.PersonName} pukul {record.Date:HH:mm}");
        return true;
    }

    /// <summary>Who has not been recorded today — the gap the office chases up.</summary>
    public async Task<List<string>> GetMissingStudentsAsync(
        string? className = null,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var today = SchoolClock.Today;
        var tomorrow = today.AddDays(1);

        var present = await db.AttendanceRecords
            .Where(a => a.Date >= today && a.Date < tomorrow && a.Role == "Siswa")
            .Select(a => a.PersonName)
            .ToListAsync(ct);

        var students = db.Students.AsNoTracking().Where(s => s.Status == "Active");

        if (!string.IsNullOrWhiteSpace(className))
        {
            students = students.Where(s => s.ClassName == className);
        }

        return await students
            .Where(s => !present.Contains(s.FullName))
            .OrderBy(s => s.FullName)
            .Select(s => s.FullName)
            .ToListAsync(ct);
    }
}
