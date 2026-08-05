using System.ComponentModel;
using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using SMSNet.Data;

namespace SMSNet.Services.Assistant.Plugins;

/// <summary>
/// Read-only access to the school's own records.
/// <para>
/// Two rules hold throughout. First, every function opens its own DI scope: the
/// assistant may run several tool calls concurrently and a single
/// <c>ApplicationDbContext</c> cannot serve overlapping queries. Second, the
/// caller's role is checked in the function body — the system prompt is guidance,
/// not a security boundary, and a model can be talked out of guidance.
/// </para>
/// </summary>
public sealed class SekolahDataPlugin
{
    private const int MaxRows = 40;

    private static readonly CultureInfo Rupiah = CultureInfo.GetCultureInfo("id-ID");

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AssistantUserContext _user;

    public SekolahDataPlugin(IServiceScopeFactory scopeFactory, AssistantUserContext user)
    {
        _scopeFactory = scopeFactory;
        _user = user;
    }

    // --- Overview ----------------------------------------------------------

    [KernelFunction("ringkasan_sekolah")]
    [Description("Mengembalikan ringkasan jumlah siswa, guru, kelas, mata pelajaran, dan kegiatan sekolah. " +
                 "Gunakan untuk pertanyaan umum seperti 'ada berapa siswa?'.")]
    public async Task<string> OverviewAsync(CancellationToken cancellationToken = default)
    {
        return await QueryAsync(async db =>
        {
            var siswaAktif = await db.Students.CountAsync(s => s.Status == "Active", cancellationToken);
            var siswaTotal = await db.Students.CountAsync(cancellationToken);
            var guru = await db.Teachers.CountAsync(t => t.Status == "Active", cancellationToken);
            var kelas = await db.ClassRooms.CountAsync(cancellationToken);
            var mapel = await db.Subjects.CountAsync(cancellationToken);
            var jadwal = await db.ScheduleItems.CountAsync(cancellationToken);
            var kegiatan = await db.Events.CountAsync(e => e.Date >= SchoolClock.Today, cancellationToken);

            return $"""
                    Ringkasan SMSNet per {SchoolClock.Today:dd MMMM yyyy}:

                    | Data | Jumlah |
                    | --- | ---: |
                    | Siswa aktif | {siswaAktif} |
                    | Siswa total | {siswaTotal} |
                    | Guru aktif | {guru} |
                    | Kelas | {kelas} |
                    | Mata pelajaran | {mapel} |
                    | Entri jadwal | {jadwal} |
                    | Kegiatan mendatang | {kegiatan} |
                    """;
        });
    }

    // --- Master data -------------------------------------------------------

    [KernelFunction("cari_siswa")]
    [Description("Mencari data siswa berdasarkan nama, kelas, atau status. " +
                 "Semua parameter opsional; kosongkan untuk menampilkan sebagian daftar.")]
    public async Task<string> FindStudentsAsync(
        [Description("Sebagian nama siswa")] string? nama = null,
        [Description("Nama kelas, contoh: 8A")] string? kelas = null,
        [Description("Status: Active atau Inactive")] string? status = null,
        CancellationToken cancellationToken = default)
    {
        if (!_user.IsStaff)
        {
            return Denied("daftar siswa", "admin atau guru");
        }

        return await QueryAsync(async db =>
        {
            var query = db.Students.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(nama))
            {
                query = query.Where(s => s.FullName.Contains(nama));
            }

            if (!string.IsNullOrWhiteSpace(kelas))
            {
                query = query.Where(s => s.ClassName != null && s.ClassName.Contains(kelas));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(s => s.Status == status);
            }

            var total = await query.CountAsync(cancellationToken);
            var rows = await query
                .OrderBy(s => s.FullName)
                .Take(MaxRows)
                .Select(s => new[]
                {
                    s.FullName,
                    s.ClassName ?? "-",
                    s.Gender ?? "-",
                    s.ParentName ?? "-",
                    s.Status
                })
                .ToListAsync(cancellationToken);

            return Table(new[] { "Nama", "Kelas", "Gender", "Orang Tua", "Status" }, rows, total);
        });
    }

    [KernelFunction("cari_guru")]
    [Description("Mencari data guru berdasarkan nama atau mata pelajaran yang diampu.")]
    public async Task<string> FindTeachersAsync(
        [Description("Sebagian nama guru")] string? nama = null,
        [Description("Mata pelajaran yang diampu")] string? mataPelajaran = null,
        CancellationToken cancellationToken = default)
    {
        return await QueryAsync(async db =>
        {
            var query = db.Teachers.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(nama))
            {
                query = query.Where(t => t.FullName.Contains(nama));
            }

            if (!string.IsNullOrWhiteSpace(mataPelajaran))
            {
                query = query.Where(t => t.Subject != null && t.Subject.Contains(mataPelajaran));
            }

            var total = await query.CountAsync(cancellationToken);

            // Contact details are staff-only; everyone else sees the teaching roster.
            var rows = _user.IsStaff
                ? await query.OrderBy(t => t.FullName).Take(MaxRows)
                    .Select(t => new[] { t.FullName, t.Subject ?? "-", t.Email ?? "-", t.Phone ?? "-", t.Status })
                    .ToListAsync(cancellationToken)
                : await query.OrderBy(t => t.FullName).Take(MaxRows)
                    .Select(t => new[] { t.FullName, t.Subject ?? "-", t.Status })
                    .ToListAsync(cancellationToken);

            var headers = _user.IsStaff
                ? new[] { "Nama", "Mata Pelajaran", "Email", "Telepon", "Status" }
                : new[] { "Nama", "Mata Pelajaran", "Status" };

            return Table(headers, rows, total);
        });
    }

    [KernelFunction("daftar_kelas")]
    [Description("Menampilkan daftar kelas beserta wali kelas dan kapasitasnya.")]
    public async Task<string> ClassesAsync(CancellationToken cancellationToken = default)
    {
        return await QueryAsync(async db =>
        {
            var rows = (await db.ClassRooms.AsNoTracking()
                    .OrderBy(c => c.Name)
                    .ToListAsync(cancellationToken))
                .Select(c => new[] { c.Name, c.HomeroomTeacher ?? "-", c.Capacity.ToString() })
                .ToList();

            return Table(new[] { "Kelas", "Wali Kelas", "Kapasitas" }, rows, rows.Count);
        });
    }

    [KernelFunction("daftar_mata_pelajaran")]
    [Description("Menampilkan daftar mata pelajaran beserta jumlah jam/kredit.")]
    public async Task<string> SubjectsAsync(CancellationToken cancellationToken = default)
    {
        return await QueryAsync(async db =>
        {
            var rows = (await db.Subjects.AsNoTracking()
                    .OrderBy(s => s.Name)
                    .ToListAsync(cancellationToken))
                .Select(s => new[] { s.Name, s.Credits.ToString(), s.Description ?? "-" })
                .ToList();

            return Table(new[] { "Mata Pelajaran", "Kredit", "Keterangan" }, rows, rows.Count);
        });
    }

    // --- Academic ----------------------------------------------------------

    [KernelFunction("jadwal_pelajaran")]
    [Description("Menampilkan jadwal pelajaran. Dapat disaring berdasarkan kelas, hari, atau nama guru.")]
    public async Task<string> ScheduleAsync(
        [Description("Nama kelas, contoh: 8A")] string? kelas = null,
        [Description("Hari: Senin, Selasa, Rabu, Kamis, atau Jumat")] string? hari = null,
        [Description("Nama guru pengampu")] string? guru = null,
        CancellationToken cancellationToken = default)
    {
        return await QueryAsync(async db =>
        {
            var query = db.ScheduleItems.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(kelas))
            {
                query = query.Where(s => s.ClassName.Contains(kelas));
            }

            if (!string.IsNullOrWhiteSpace(hari))
            {
                query = query.Where(s => s.Day == hari);
            }

            if (!string.IsNullOrWhiteSpace(guru))
            {
                query = query.Where(s => s.Teacher.Contains(guru));
            }

            var total = await query.CountAsync(cancellationToken);
            var rows = await query
                .OrderBy(s => s.ClassName).ThenBy(s => s.Day)
                .Take(MaxRows)
                .Select(s => new[] { s.ClassName, s.Day, s.TimeSlot, s.Subject, s.Teacher })
                .ToListAsync(cancellationToken);

            return Table(new[] { "Kelas", "Hari", "Jam", "Mata Pelajaran", "Guru" }, rows, total);
        });
    }

    [KernelFunction("rekap_absensi")]
    [Description("Merekap kehadiran dalam rentang tanggal tertentu, termasuk persentase kehadiran. " +
                 "Format tanggal: YYYY-MM-DD.")]
    public async Task<string> AttendanceAsync(
        [Description("Tanggal awal, format YYYY-MM-DD. Kosongkan untuk 30 hari terakhir.")] string? tanggalAwal = null,
        [Description("Tanggal akhir, format YYYY-MM-DD. Kosongkan untuk hari ini.")] string? tanggalAkhir = null,
        [Description("Peran: Siswa atau Guru")] string? peran = null,
        CancellationToken cancellationToken = default)
    {
        if (!_user.IsStaff)
        {
            return Denied("rekap absensi sekolah", "admin atau guru");
        }

        var start = ParseDate(tanggalAwal) ?? SchoolClock.Today.AddDays(-30);
        var end = ParseDate(tanggalAkhir) ?? SchoolClock.Today;

        return await QueryAsync(async db =>
        {
            var query = db.AttendanceRecords.AsNoTracking()
                .Where(a => a.Date >= start && a.Date <= end);

            if (!string.IsNullOrWhiteSpace(peran))
            {
                query = query.Where(a => a.Role == peran);
            }

            var total = await query.CountAsync(cancellationToken);

            if (total == 0)
            {
                return $"Tidak ada catatan absensi antara {start:yyyy-MM-dd} dan {end:yyyy-MM-dd}.";
            }

            var hadir = await query.CountAsync(a => a.Status == "Present", cancellationToken);

            var perStatus = await query
                .GroupBy(a => a.Status)
                .Select(g => new { Status = g.Key, Jumlah = g.Count() })
                .ToListAsync(cancellationToken);

            var sb = new StringBuilder()
                .AppendLine($"Rekap absensi {start:dd MMM yyyy} – {end:dd MMM yyyy}:")
                .AppendLine()
                .AppendLine($"- Total catatan: {total}")
                .AppendLine($"- Hadir: {hadir} ({(double)hadir / total * 100:0.#}%)")
                .AppendLine()
                .AppendLine("| Status | Jumlah |")
                .AppendLine("| --- | ---: |");

            foreach (var item in perStatus.OrderByDescending(x => x.Jumlah))
            {
                sb.AppendLine($"| {item.Status} | {item.Jumlah} |");
            }

            return sb.ToString();
        });
    }

    [KernelFunction("nilai_siswa")]
    [Description("Menampilkan nilai siswa. Dapat disaring berdasarkan nama siswa atau mata pelajaran, " +
                 "beserta rata-ratanya.")]
    public async Task<string> GradesAsync(
        [Description("Nama siswa")] string? namaSiswa = null,
        [Description("Mata pelajaran")] string? mataPelajaran = null,
        CancellationToken cancellationToken = default)
    {
        // A parent or student may look up a named person; only staff may browse everyone.
        if (!_user.IsStaff && string.IsNullOrWhiteSpace(namaSiswa))
        {
            return "Sebutkan nama siswa yang ingin dilihat nilainya. " +
                   "Penelusuran seluruh nilai sekolah hanya tersedia untuk admin dan guru.";
        }

        return await QueryAsync(async db =>
        {
            var query = db.GradeRecords.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(namaSiswa))
            {
                query = query.Where(g => g.StudentName.Contains(namaSiswa));
            }

            if (!string.IsNullOrWhiteSpace(mataPelajaran))
            {
                query = query.Where(g => g.Subject.Contains(mataPelajaran));
            }

            var total = await query.CountAsync(cancellationToken);

            if (total == 0)
            {
                return "Tidak ada data nilai yang cocok dengan kriteria tersebut.";
            }

            var average = await query.AverageAsync(g => (double)g.Score, cancellationToken);
            // Materialise before formatting: EF Core cannot translate ToString(format)
            // into SQL, and composing it inside Select throws at query time.
            var rows = (await query
                    .OrderBy(g => g.StudentName).ThenBy(g => g.Subject)
                    .Take(MaxRows)
                    .ToListAsync(cancellationToken))
                .Select(g => new[] { g.StudentName, g.Subject, g.Score.ToString("0.##"), g.Notes ?? "-" })
                .ToList();

            return Table(new[] { "Siswa", "Mata Pelajaran", "Nilai", "Catatan" }, rows, total)
                   + $"\n\nRata-rata: **{average:0.##}**";
        });
    }

    [KernelFunction("daftar_tugas_ujian")]
    [Description("Menampilkan daftar tugas dan ujian beserta tenggat waktunya.")]
    public async Task<string> TasksAsync(
        [Description("Status: Open, Scheduled, atau Closed")] string? status = null,
        CancellationToken cancellationToken = default)
    {
        return await QueryAsync(async db =>
        {
            var query = db.TaskExams.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(t => t.Status == status);
            }

            var total = await query.CountAsync(cancellationToken);
            var rows = (await query
                    .OrderBy(t => t.DueDate)
                    .Take(MaxRows)
                    .ToListAsync(cancellationToken))
                .Select(t => new[] { t.Title, t.Type, t.DueDate.ToString("yyyy-MM-dd"), t.Status })
                .ToList();

            return Table(new[] { "Judul", "Jenis", "Tenggat", "Status" }, rows, total);
        });
    }

    [KernelFunction("materi_elearning")]
    [Description("Menampilkan daftar materi e-learning: video, modul, kuis, dan ujian daring.")]
    public async Task<string> ELearningAsync(CancellationToken cancellationToken = default)
    {
        return await QueryAsync(async db =>
        {
            var rows = await db.ELearningContents.AsNoTracking()
                .OrderBy(c => c.Title)
                .Take(MaxRows)
                .Select(c => new[] { c.Title, c.ModuleType, c.Description ?? "-" })
                .ToListAsync(cancellationToken);

            return Table(new[] { "Judul", "Jenis", "Keterangan" }, rows, rows.Count);
        });
    }

    // --- Finance & operations ---------------------------------------------

    [KernelFunction("rekap_pembayaran")]
    [Description("Merekap pembayaran SPP dan biaya sekolah lainnya, termasuk total nominal " +
                 "dan tunggakan. Hanya untuk admin dan orang tua.")]
    public async Task<string> PaymentsAsync(
        [Description("Nama siswa")] string? namaSiswa = null,
        [Description("Status: Paid atau Pending")] string? status = null,
        [Description("Kategori, contoh: SPP atau Buku")] string? kategori = null,
        CancellationToken cancellationToken = default)
    {
        if (!_user.CanSeeFinance)
        {
            return Denied("data pembayaran", "admin atau orang tua");
        }

        return await QueryAsync(async db =>
        {
            var query = db.PaymentRecords.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(namaSiswa))
            {
                query = query.Where(p => p.StudentName.Contains(namaSiswa));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(p => p.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(kategori))
            {
                query = query.Where(p => p.Category == kategori);
            }

            var total = await query.CountAsync(cancellationToken);

            if (total == 0)
            {
                return "Tidak ada transaksi yang cocok dengan kriteria tersebut.";
            }

            var jumlah = await query.SumAsync(p => p.Amount, cancellationToken);
            var tertunda = await query.Where(p => p.Status == "Pending")
                .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

            var rows = (await query
                    .OrderByDescending(p => p.Date)
                    .Take(MaxRows)
                    .ToListAsync(cancellationToken))
                .Select(p => new[]
                {
                    p.StudentName,
                    p.Category,
                    p.Amount.ToString("C0", Rupiah),
                    p.Status,
                    p.Date.ToString("yyyy-MM-dd")
                })
                .ToList();

            return Table(new[] { "Siswa", "Kategori", "Nominal", "Status", "Tanggal" }, rows, total)
                   + $"\n\nTotal nilai transaksi: **{jumlah.ToString("C0", Rupiah)}**"
                   + $"\nTunggakan (Pending): **{tertunda.ToString("C0", Rupiah)}**";
        });
    }

    [KernelFunction("inventaris_sekolah")]
    [Description("Menampilkan daftar aset dan inventaris sekolah. Hanya untuk admin dan guru.")]
    public async Task<string> InventoryAsync(
        [Description("Kategori aset, contoh: Lab Komputer")] string? kategori = null,
        CancellationToken cancellationToken = default)
    {
        if (!_user.IsStaff)
        {
            return Denied("data inventaris", "admin atau guru");
        }

        return await QueryAsync(async db =>
        {
            var query = db.InventoryItems.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(kategori))
            {
                query = query.Where(i => i.Category.Contains(kategori));
            }

            var total = await query.CountAsync(cancellationToken);
            var rows = (await query
                    .OrderBy(i => i.Category).ThenBy(i => i.Name)
                    .Take(MaxRows)
                    .ToListAsync(cancellationToken))
                .Select(i => new[] { i.Name, i.Category, i.Quantity.ToString(), i.Condition })
                .ToList();

            return Table(new[] { "Aset", "Kategori", "Jumlah", "Kondisi" }, rows, total);
        });
    }

    // --- School life -------------------------------------------------------

    [KernelFunction("daftar_kegiatan")]
    [Description("Menampilkan kegiatan sekolah dan ekstrakurikuler, secara bawaan yang akan datang.")]
    public async Task<string> EventsAsync(
        [Description("Isi true untuk menyertakan kegiatan yang sudah lewat")] bool termasukLampau = false,
        CancellationToken cancellationToken = default)
    {
        return await QueryAsync(async db =>
        {
            var query = db.Events.AsNoTracking().AsQueryable();

            if (!termasukLampau)
            {
                var today = SchoolClock.Today;
                query = query.Where(e => e.Date >= today);
            }

            var total = await query.CountAsync(cancellationToken);
            var rows = (await query
                    .OrderBy(e => e.Date)
                    .Take(MaxRows)
                    .ToListAsync(cancellationToken))
                .Select(e => new[] { e.Title, e.Date.ToString("yyyy-MM-dd"), e.Location, e.Type })
                .ToList();

            return Table(new[] { "Kegiatan", "Tanggal", "Lokasi", "Jenis" }, rows, total);
        });
    }

    [KernelFunction("notifikasi_terbaru")]
    [Description("Menampilkan pengumuman dan notifikasi sekolah terbaru.")]
    public async Task<string> NotificationsAsync(
        [Description("Sasaran: Siswa, Guru, atau Orang Tua")] string? sasaran = null,
        CancellationToken cancellationToken = default)
    {
        return await QueryAsync(async db =>
        {
            var query = db.Notifications.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(sasaran))
            {
                query = query.Where(n => n.Audience.Contains(sasaran));
            }

            var total = await query.CountAsync(cancellationToken);
            var rows = (await query
                    .OrderByDescending(n => n.Date)
                    .Take(15)
                    .ToListAsync(cancellationToken))
                .Select(n => new[] { n.Date.ToString("yyyy-MM-dd"), n.Title, n.Message, n.Audience })
                .ToList();

            return Table(new[] { "Tanggal", "Judul", "Isi", "Sasaran" }, rows, total);
        });
    }

    // --- Helpers -----------------------------------------------------------

    /// <summary>
    /// Runs a query on its own scoped DbContext. Tool calls can overlap, and
    /// sharing the circuit's context across them throws a concurrency exception.
    /// </summary>
    private async Task<string> QueryAsync(Func<ApplicationDbContext, Task<string>> work)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        try
        {
            return await work(db);
        }
        catch (Exception ex)
        {
            return $"Gagal mengambil data: {ex.Message}";
        }
    }

    private string Denied(string what, string who) =>
        $"Maaf, {what} hanya dapat diakses oleh {who}. " +
        $"Akun Anda saat ini memiliki peran: {_user.RoleLabel}.";

    private static DateTime? ParseDate(string? value) =>
        DateTime.TryParseExact(value?.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var parsed)
            ? parsed
            : null;

    /// <summary>Markdown table — the chat surface renders these, and models read them reliably.</summary>
    private static string Table(string[] headers, List<string[]> rows, int totalMatches)
    {
        if (rows.Count == 0)
        {
            return "Tidak ada data yang cocok dengan kriteria tersebut.";
        }

        var sb = new StringBuilder();
        sb.Append("| ").Append(string.Join(" | ", headers)).AppendLine(" |");
        sb.Append("| ").Append(string.Join(" | ", headers.Select(_ => "---"))).AppendLine(" |");

        foreach (var row in rows)
        {
            sb.Append("| ").Append(string.Join(" | ", row.Select(Escape))).AppendLine(" |");
        }

        if (totalMatches > rows.Count)
        {
            sb.AppendLine().AppendLine($"_Menampilkan {rows.Count} dari {totalMatches} data yang cocok._");
        }

        return sb.ToString();
    }

    /// <summary>A pipe inside a cell would split the markdown row into extra columns.</summary>
    private static string Escape(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? "-"
            : value.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
}
