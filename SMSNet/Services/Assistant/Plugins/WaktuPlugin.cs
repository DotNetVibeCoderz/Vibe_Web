using System.ComponentModel;
using System.Globalization;
using Microsoft.SemanticKernel;

namespace SMSNet.Services.Assistant.Plugins;

/// <summary>
/// Date and time in school-local terms.
/// <para>
/// Without this the model answers "hari ini" from its training cutoff, which is
/// always wrong. Every temporal answer must come from here.
/// </para>
/// </summary>
public sealed class WaktuPlugin
{
    private static readonly CultureInfo Indonesian = CultureInfo.GetCultureInfo("id-ID");

    [KernelFunction("tanggal_hari_ini")]
    [Description("Mengembalikan tanggal dan waktu saat ini di zona waktu sekolah (WIB). " +
                 "Gunakan ini setiap kali pengguna menyebut 'hari ini', 'sekarang', 'besok', atau 'minggu ini'.")]
    public string Today()
    {
        var now = SchoolClock.Now;
        return $"""
                Tanggal  : {now.ToString("dddd, dd MMMM yyyy", Indonesian)}
                Jam      : {now:HH:mm} {SchoolClock.TimeZoneLabel}
                ISO      : {now:yyyy-MM-dd}
                Pekan ke : {ISOWeek.GetWeekOfYear(now.DateTime)} tahun {now.Year}
                """;
    }

    [KernelFunction("hitung_selisih_hari")]
    [Description("Menghitung jumlah hari antara dua tanggal. Format tanggal: YYYY-MM-DD.")]
    public string DaysBetween(
        [Description("Tanggal awal, format YYYY-MM-DD")] string tanggalAwal,
        [Description("Tanggal akhir, format YYYY-MM-DD")] string tanggalAkhir)
    {
        if (!TryParse(tanggalAwal, out var start))
        {
            return $"Tanggal awal '{tanggalAwal}' tidak dikenali. Gunakan format YYYY-MM-DD.";
        }

        if (!TryParse(tanggalAkhir, out var end))
        {
            return $"Tanggal akhir '{tanggalAkhir}' tidak dikenali. Gunakan format YYYY-MM-DD.";
        }

        var days = (end.Date - start.Date).Days;
        var arah = days < 0 ? "sebelum" : "setelah";

        return $"{Math.Abs(days)} hari ({tanggalAkhir} berada {arah} {tanggalAwal}).";
    }

    [KernelFunction("tambah_hari")]
    [Description("Menambah atau mengurangi sejumlah hari dari sebuah tanggal, lalu mengembalikan tanggal hasilnya.")]
    public string AddDays(
        [Description("Tanggal awal, format YYYY-MM-DD. Kosongkan untuk memakai hari ini.")] string? tanggal,
        [Description("Jumlah hari. Boleh negatif untuk mundur.")] int jumlahHari)
    {
        var start = string.IsNullOrWhiteSpace(tanggal)
            ? SchoolClock.Today
            : TryParse(tanggal, out var parsed) ? parsed : DateTime.MinValue;

        if (start == DateTime.MinValue)
        {
            return $"Tanggal '{tanggal}' tidak dikenali. Gunakan format YYYY-MM-DD.";
        }

        var result = start.AddDays(jumlahHari);
        return $"{result.ToString("dddd, dd MMMM yyyy", Indonesian)} ({result:yyyy-MM-dd})";
    }

    [KernelFunction("info_tahun_ajaran")]
    [Description("Mengembalikan tahun ajaran dan semester yang sedang berjalan menurut kalender sekolah Indonesia.")]
    public string AcademicYear()
    {
        var now = SchoolClock.Today;

        // Indonesian school years run July–June; semester 1 is Jul–Dec.
        var startYear = now.Month >= 7 ? now.Year : now.Year - 1;
        var semester = now.Month >= 7 ? 1 : 2;

        return $"Tahun ajaran {startYear}/{startYear + 1}, semester {semester} " +
               $"({(semester == 1 ? "Ganjil" : "Genap")}).";
    }

    private static bool TryParse(string value, out DateTime result) =>
        DateTime.TryParseExact(value?.Trim(), new[] { "yyyy-MM-dd", "dd-MM-yyyy", "dd/MM/yyyy" },
            CultureInfo.InvariantCulture, DateTimeStyles.None, out result)
        || DateTime.TryParse(value, Indonesian, DateTimeStyles.None, out result);
}
