using System.Globalization;
using System.Text.RegularExpressions;
using Ganss.Xss;
using Microsoft.EntityFrameworkCore;
using SMSNet.Data;
using SMSNet.Models;

namespace SMSNet.Services.Attendance;

/// <summary>
/// Loads, saves, and renders the printable card layout.
/// <para>
/// The default layout ships as a file under <c>wwwroot/templates/</c>. Saving from the
/// admin screen stores a row in <see cref="CardTemplate"/>, which then takes
/// precedence — a fresh install works with no database content, and a school can
/// restyle its cards without shell access to the server.
/// </para>
/// </summary>
public sealed partial class CardTemplateService
{
    public const string StudentKind = "siswa";
    public const string TeacherKind = "guru";

    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly IWebHostEnvironment _environment;
    private readonly QrCodeService _qr;
    private readonly ILogger<CardTemplateService> _logger;
    private readonly HtmlSanitizer _sanitizer;

    public CardTemplateService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        IWebHostEnvironment environment,
        QrCodeService qr,
        ILogger<CardTemplateService> logger)
    {
        _dbFactory = dbFactory;
        _environment = environment;
        _qr = qr;
        _logger = logger;
        _sanitizer = BuildSanitizer();
    }

    /// <summary>Placeholders an author may use, with a short description for the UI.</summary>
    public static readonly (string Token, string Meaning)[] Placeholders =
    {
        ("{{NAMA}}", "Nama lengkap"),
        ("{{KELAS}}", "Kelas (siswa) atau mata pelajaran (guru)"),
        ("{{NIS}}", "Nomor induk, dibangkitkan dari Id"),
        ("{{GENDER}}", "Laki-laki atau Perempuan"),
        ("{{WALI}}", "Nama wali (siswa) atau email (guru)"),
        ("{{TELEPON}}", "Nomor telepon"),
        ("{{KODE}}", "Kode QR dalam bentuk teks"),
        ("{{QR}}", "Gambar QR sebagai data URI — pakai di dalam src"),
        ("{{SEKOLAH}}", "Nama sekolah"),
        ("{{TAHUN_AJARAN}}", "Tahun ajaran berjalan"),
        ("{{STATUS}}", "Aktif atau Tidak aktif")
    };

    // --- Loading and saving ------------------------------------------------

    /// <summary>The layout in force: the saved override if present, else the shipped file.</summary>
    public async Task<string> GetHtmlAsync(string kind, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var saved = await db.CardTemplates.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Kind == kind, ct);

        if (saved is not null && !string.IsNullOrWhiteSpace(saved.Html))
        {
            return saved.Html;
        }

        return await ReadFileAsync(kind, ct);
    }

    /// <summary>The shipped default, ignoring any saved override. Backs the "reset" action.</summary>
    public async Task<string> GetFileDefaultAsync(string kind, CancellationToken ct = default) =>
        await ReadFileAsync(kind, ct);

    public async Task<CardTemplate?> GetSavedAsync(string kind, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.CardTemplates.AsNoTracking().FirstOrDefaultAsync(t => t.Kind == kind, ct);
    }

    public async Task SaveAsync(string kind, string html, string? updatedBy, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var existing = await db.CardTemplates.FirstOrDefaultAsync(t => t.Kind == kind, ct);

        if (existing is null)
        {
            db.CardTemplates.Add(new CardTemplate
            {
                Kind = kind,
                Name = kind == TeacherKind ? "Kartu Guru" : "Kartu Siswa",
                Html = html,
                UpdatedBy = updatedBy,
                UpdatedAt = SchoolClock.LocalNow
            });
        }
        else
        {
            existing.Html = html;
            existing.UpdatedBy = updatedBy;
            existing.UpdatedAt = SchoolClock.LocalNow;
        }

        await db.SaveChangesAsync(ct);
    }

    /// <summary>Drops the override so the shipped file takes over again.</summary>
    public async Task ResetAsync(string kind, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var existing = await db.CardTemplates.FirstOrDefaultAsync(t => t.Kind == kind, ct);
        if (existing is not null)
        {
            db.CardTemplates.Remove(existing);
            await db.SaveChangesAsync(ct);
        }
    }

    // --- Rendering ---------------------------------------------------------

    /// <summary>Fills a student's details into the layout and returns print-ready HTML.</summary>
    public string RenderStudent(string template, Student student, string schoolName)
    {
        var code = student.QrCode ?? string.Empty;

        return Render(template, new Dictionary<string, string>
        {
            ["NAMA"] = student.FullName,
            ["KELAS"] = Fallback(student.ClassName),
            ["NIS"] = $"{student.Id:00000}",
            ["GENDER"] = Fallback(student.Gender),
            ["WALI"] = Fallback(student.ParentName),
            ["TELEPON"] = Fallback(student.Phone),
            ["KODE"] = string.IsNullOrEmpty(code) ? "— belum diterbitkan —" : code,
            ["SEKOLAH"] = schoolName,
            ["TAHUN_AJARAN"] = AcademicYear(),
            ["STATUS"] = student.Status == "Active" ? "Aktif" : "Tidak aktif"
        }, code);
    }

    public string RenderTeacher(string template, Teacher teacher, string schoolName)
    {
        var code = teacher.QrCode ?? string.Empty;

        return Render(template, new Dictionary<string, string>
        {
            ["NAMA"] = teacher.FullName,
            ["KELAS"] = Fallback(teacher.Subject),
            ["NIS"] = $"{teacher.Id:00000}",
            ["GENDER"] = "—",
            ["WALI"] = Fallback(teacher.Email),
            ["TELEPON"] = Fallback(teacher.Phone),
            ["KODE"] = string.IsNullOrEmpty(code) ? "— belum diterbitkan —" : code,
            ["SEKOLAH"] = schoolName,
            ["TAHUN_AJARAN"] = AcademicYear(),
            ["STATUS"] = teacher.Status == "Active" ? "Aktif" : "Tidak aktif"
        }, code);
    }

    private string Render(string template, Dictionary<string, string> values, string qrPayload)
    {
        // Sanitise the layout, not the values: the template is author-supplied HTML and
        // an admin could paste a script into it, deliberately or from a copied snippet.
        var safeTemplate = _sanitizer.Sanitize(template);

        // The QR is substituted after sanitising — a data URI would otherwise be
        // stripped, and this value is generated by us rather than typed by anyone.
        var qr = string.IsNullOrEmpty(qrPayload) ? string.Empty : _qr.ToSvgDataUri(qrPayload);

        return TokenPattern().Replace(safeTemplate, match =>
        {
            var token = match.Groups["name"].Value.ToUpperInvariant();

            if (token == "QR")
            {
                return qr;
            }

            return values.TryGetValue(token, out var value)
                ? System.Net.WebUtility.HtmlEncode(value)
                : match.Value;
        });
    }

    // --- Helpers -----------------------------------------------------------

    private async Task<string> ReadFileAsync(string kind, CancellationToken ct)
    {
        var fileName = kind == TeacherKind ? "kartu-guru.html" : "kartu-siswa.html";
        var path = Path.Combine(_environment.WebRootPath, "templates", fileName);

        try
        {
            if (File.Exists(path))
            {
                return await File.ReadAllTextAsync(path, ct);
            }

            _logger.LogWarning("Card template file missing at {Path}", path);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "Could not read card template {Path}", path);
        }

        // A minimal layout so the page still prints something usable if the file is gone.
        return """
               <div class="kartu">
                 <div class="kartu__isi">
                   <div class="kartu__kiri">
                     <p class="kartu__nama">{{NAMA}}</p>
                     <dl class="kartu__data"><div><dt>Kelas</dt><dd>{{KELAS}}</dd></div></dl>
                   </div>
                   <div class="kartu__kanan">
                     <img class="kartu__qr" src="{{QR}}" alt="Kode QR" />
                     <p class="kartu__kode">{{KODE}}</p>
                   </div>
                 </div>
               </div>
               """;
    }

    private static string Fallback(string? value) => string.IsNullOrWhiteSpace(value) ? "—" : value;

    private static string AcademicYear()
    {
        var today = SchoolClock.Today;
        var start = today.Month >= 7 ? today.Year : today.Year - 1;
        return $"{start}/{start + 1}";
    }

    private static HtmlSanitizer BuildSanitizer()
    {
        var sanitizer = new HtmlSanitizer();

        // Layout markup only — no scripts, no frames, no event handlers.
        sanitizer.AllowedTags.Clear();
        foreach (var tag in new[]
                 {
                     "div", "span", "p", "img", "dl", "dt", "dd", "ul", "ol", "li",
                     "strong", "em", "small", "br", "hr", "section", "header", "footer",
                     "h1", "h2", "h3", "h4", "table", "thead", "tbody", "tr", "th", "td"
                 })
        {
            sanitizer.AllowedTags.Add(tag);
        }

        sanitizer.AllowedAttributes.Clear();
        foreach (var attribute in new[] { "class", "src", "alt", "title", "style", "colspan", "rowspan" })
        {
            sanitizer.AllowedAttributes.Add(attribute);
        }

        // Inline styles are allowed here — a card layout genuinely needs them, and this
        // markup is printed rather than shown inside the application shell.
        foreach (var property in new[]
                 {
                     "color", "background", "background-color", "font-size", "font-weight",
                     "font-family", "text-align", "padding", "margin", "border", "border-radius",
                     "width", "height", "display", "flex", "gap", "letter-spacing", "line-height",
                     "text-transform", "opacity", "position", "top", "left", "right", "bottom"
                 })
        {
            sanitizer.AllowedCssProperties.Add(property);
        }

        sanitizer.AllowedSchemes.Clear();
        sanitizer.AllowedSchemes.Add("https");
        sanitizer.AllowedSchemes.Add("data"); // the QR is a data URI

        return sanitizer;
    }

    [GeneratedRegex(@"\{\{\s*(?<name>[A-Za-z_]+)\s*\}\}")]
    private static partial Regex TokenPattern();
}
