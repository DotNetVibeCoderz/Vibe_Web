using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ClosedXML.Excel;

namespace MyPoS.Services.Import
{
    /// <summary>Satu daftar nilai yang dipasang sebagai dropdown pada template.</summary>
    /// <param name="Header">Judul kolom yang mendapat dropdown.</param>
    /// <param name="Values">Pilihan yang tersedia.</param>
    public record ImportReferenceList(string Header, IReadOnlyList<string> Values);

    /// <summary>
    /// Pembuat template dan pembaca berkas Excel yang dipakai bersama oleh semua importer.
    ///
    /// Template selalu berisi tiga hal: lembar Data yang siap diisi, satu baris contoh yang
    /// diberi keterangan agar tidak ikut terimpor, dan lembar Petunjuk yang menjelaskan
    /// setiap kolom. Tanpa lembar Petunjuk, pengguna harus menebak arti kolom seperti
    /// "Stok Minimum" atau format tanggal yang diterima.
    /// </summary>
    public static class ExcelImportHelper
    {
        public const string DataSheetName = "Data";
        private const string GuideSheetName = "Petunjuk";
        private const string ReferenceSheetName = "Referensi";

        private const string HeaderBackground = "#B3382B";
        private const string HeaderText = "#FFFFFF";
        private const string SampleText = "#9A8F82";

        /// <summary>Baris contoh ditandai dengan nilai ini pada kolom pertama supaya mudah dikenali dan dihapus.</summary>
        public const string SampleMarker = "CONTOH";

        // ------------------------------------------------------------- Template

        public static byte[] BuildTemplate(
            string title,
            IReadOnlyList<ImportColumn> columns,
            IReadOnlyList<IReadOnlyList<string>> sampleRows,
            IReadOnlyList<ImportReferenceList>? references = null)
        {
            using var workbook = new XLWorkbook();

            var data = workbook.Worksheets.Add(DataSheetName);

            for (var i = 0; i < columns.Count; i++)
            {
                var cell = data.Cell(1, i + 1);
                cell.Value = columns[i].Header;
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.FromHtml(HeaderText);
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml(HeaderBackground);
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // Kolom wajib ditandai di komentar sel, bukan pada judulnya, supaya judul
                // tetap sama persis dengan yang dicari pembaca berkas.
                cell.CreateComment().AddText(columns[i].Required
                    ? $"WAJIB DIISI. {columns[i].Description}"
                    : $"Opsional. {columns[i].Description}");
            }

            for (var r = 0; r < sampleRows.Count; r++)
            {
                for (var c = 0; c < sampleRows[r].Count && c < columns.Count; c++)
                {
                    var cell = data.Cell(r + 2, c + 1);
                    cell.Value = sampleRows[r][c];
                    cell.Style.Font.Italic = true;
                    cell.Style.Font.FontColor = XLColor.FromHtml(SampleText);
                }
            }

            data.SheetView.FreezeRows(1);
            data.Columns().AdjustToContents(1, 20, 12, 42);

            // ---------- Dropdown ----------
            if (references is { Count: > 0 })
            {
                var reference = workbook.Worksheets.Add(ReferenceSheetName);
                var column = 1;

                foreach (var list in references)
                {
                    var headerIndex = IndexOfHeader(columns, list.Header);
                    if (headerIndex < 0 || list.Values.Count == 0) continue;

                    reference.Cell(1, column).Value = list.Header;
                    for (var i = 0; i < list.Values.Count; i++)
                        reference.Cell(i + 2, column).Value = list.Values[i];

                    var source = reference.Range(2, column, list.Values.Count + 1, column);
                    var target = data.Range(2, headerIndex + 1, 500, headerIndex + 1);

                    try
                    {
                        var validation = target.CreateDataValidation();
                        validation.List(source, true);
                        validation.IgnoreBlanks = true;
                        validation.ErrorStyle = XLErrorStyle.Warning;
                        validation.ErrorTitle = list.Header;
                        validation.ErrorMessage = $"Pilih salah satu {list.Header.ToLowerInvariant()} yang tersedia.";
                    }
                    catch (Exception)
                    {
                        // Dropdown hanya kenyamanan; kegagalan memasangnya tidak boleh
                        // menggagalkan pembuatan template. Nilainya tetap dapat diketik.
                    }

                    column++;
                }

                reference.Columns().AdjustToContents();
                reference.Hide();
            }

            // ---------- Petunjuk ----------
            var guide = workbook.Worksheets.Add(GuideSheetName);

            guide.Cell(1, 1).Value = $"Template impor {title}";
            guide.Cell(1, 1).Style.Font.Bold = true;
            guide.Cell(1, 1).Style.Font.FontSize = 14;

            var notes = new[]
            {
                $"1. Isi data pada lembar \"{DataSheetName}\". Jangan mengubah judul kolom di baris pertama.",
                $"2. Hapus baris contoh yang bertanda {SampleMarker} sebelum mengunggah, atau biarkan — baris tersebut akan dilewati.",
                "3. Urutan kolom boleh diubah; yang dicocokkan adalah judulnya.",
                "4. Baris yang seluruh selnya kosong akan dilewati.",
                "5. Angka boleh ditulis 15000 maupun 15.000. Jangan sertakan simbol mata uang.",
                "6. Setelah diunggah, isi berkas diperiksa lebih dulu dan hasilnya ditampilkan sebagai pratinjau sebelum disimpan."
            };

            for (var i = 0; i < notes.Length; i++)
                guide.Cell(3 + i, 1).Value = notes[i];

            var tableTop = 4 + notes.Length;
            var headers = new[] { "Kolom", "Wajib", "Penjelasan", "Contoh" };

            for (var i = 0; i < headers.Length; i++)
            {
                var cell = guide.Cell(tableTop, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.FromHtml(HeaderText);
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml(HeaderBackground);
            }

            for (var i = 0; i < columns.Count; i++)
            {
                guide.Cell(tableTop + 1 + i, 1).Value = columns[i].Header;
                guide.Cell(tableTop + 1 + i, 2).Value = columns[i].Required ? "Ya" : "Tidak";
                guide.Cell(tableTop + 1 + i, 3).Value = columns[i].Description;
                guide.Cell(tableTop + 1 + i, 4).Value = columns[i].Example;
            }

            guide.Columns().AdjustToContents(1, 4, 10, 70);
            guide.Column(3).Style.Alignment.WrapText = true;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private static int IndexOfHeader(IReadOnlyList<ImportColumn> columns, string header)
        {
            for (var i = 0; i < columns.Count; i++)
                if (Matches(columns[i].Header, header))
                    return i;
            return -1;
        }

        // -------------------------------------------------------------- Pembaca

        /// <summary>
        /// Lembar Data yang sudah terbuka beserta pemetaan judul kolom ke nomor kolom.
        /// Membuang objek ini akan menutup workbook-nya.
        /// </summary>
        public sealed class SheetHandle : IDisposable
        {
            public required IXLWorksheet Sheet { get; init; }
            public required Dictionary<string, int> HeaderMap { get; init; }
            public required XLWorkbook Workbook { private get; init; }

            public void Dispose() => Workbook.Dispose();
        }

        /// <summary>
        /// Membuka berkas dan memetakan judul kolom ke nomor kolom. Mengembalikan null bila
        /// berkasnya tidak dapat dipakai; alasannya ditulis ke <paramref name="preview"/>.
        /// </summary>
        public static SheetHandle? OpenDataSheet(
            Stream file,
            IReadOnlyList<ImportColumn> columns,
            ImportPreview preview)
        {
            XLWorkbook workbook;

            try
            {
                workbook = new XLWorkbook(file);
            }
            catch (Exception)
            {
                preview.FileErrors.Add("Berkas tidak dapat dibaca. Pastikan formatnya .xlsx dan tidak rusak.");
                return null;
            }

            var sheet = workbook.Worksheets.FirstOrDefault(w => Matches(w.Name, DataSheetName))
                        ?? workbook.Worksheets.FirstOrDefault();

            if (sheet is null)
            {
                preview.FileErrors.Add("Berkas tidak memiliki lembar kerja sama sekali.");
                workbook.Dispose();
                return null;
            }

            var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var lastColumn = sheet.LastColumnUsed()?.ColumnNumber() ?? 0;

            for (var c = 1; c <= lastColumn; c++)
            {
                var header = sheet.Cell(1, c).GetString().Trim();
                if (header.Length == 0) continue;

                var known = columns.FirstOrDefault(col => Matches(col.Header, header));
                if (known is not null) headerMap[known.Header] = c;
            }

            var missing = columns.Where(c => c.Required && !headerMap.ContainsKey(c.Header)).ToList();
            if (missing.Count > 0)
            {
                preview.FileErrors.Add(
                    $"Kolom wajib tidak ditemukan pada baris pertama: {string.Join(", ", missing.Select(m => m.Header))}. " +
                    "Gunakan berkas template agar judul kolomnya sesuai.");
                workbook.Dispose();
                return null;
            }

            return new SheetHandle { Sheet = sheet, HeaderMap = headerMap, Workbook = workbook };
        }

        /// <summary>Nomor baris terakhir yang berisi data.</summary>
        public static int LastDataRow(IXLWorksheet sheet) => sheet.LastRowUsed()?.RowNumber() ?? 1;

        /// <summary>true bila seluruh sel pada baris tersebut kosong.</summary>
        public static bool IsRowEmpty(IXLWorksheet sheet, int row, Dictionary<string, int> headerMap)
            => headerMap.Values.All(c => sheet.Cell(row, c).IsEmpty());

        public static string ReadText(IXLWorksheet sheet, int row, Dictionary<string, int> headerMap, string header)
        {
            if (!headerMap.TryGetValue(header, out var column)) return "";

            var cell = sheet.Cell(row, column);
            if (cell.IsEmpty()) return "";

            // Angka yang dipakai sebagai teks — barcode, nomor telepon — tidak boleh berubah
            // menjadi notasi ilmiah atau kehilangan angka nol di depan.
            if (cell.Value.IsNumber)
            {
                var number = cell.Value.GetNumber();
                return number == Math.Floor(number) && Math.Abs(number) < 1e15
                    ? ((long)number).ToString(CultureInfo.InvariantCulture)
                    : number.ToString(CultureInfo.InvariantCulture);
            }

            return cell.GetString().Trim();
        }

        /// <summary>
        /// Membaca angka. Sel bertipe angka dipakai apa adanya; teks dicoba dengan format
        /// Indonesia lebih dulu, lalu format invarian — pengguna lazim menulis 15.000
        /// maupun 15000, dan keduanya harus berarti sama.
        /// </summary>
        public static bool TryReadDecimal(
            IXLWorksheet sheet, int row, Dictionary<string, int> headerMap, string header, out decimal value)
        {
            value = 0m;
            if (!headerMap.TryGetValue(header, out var column)) return false;

            var cell = sheet.Cell(row, column);
            if (cell.IsEmpty()) return false;

            if (cell.Value.IsNumber)
            {
                value = (decimal)cell.Value.GetNumber();
                return true;
            }

            var text = cell.GetString().Trim();
            if (text.Length == 0) return false;

            // Buang simbol mata uang bila pengguna menyalin dari tempat lain.
            text = text.Replace("Rp", "", StringComparison.OrdinalIgnoreCase).Trim();

            var indonesian = CultureInfo.GetCultureInfo("id-ID");
            if (decimal.TryParse(text, NumberStyles.Number, indonesian, out value)) return true;
            if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value)) return true;

            return false;
        }

        public static bool TryReadInt(
            IXLWorksheet sheet, int row, Dictionary<string, int> headerMap, string header, out int value)
        {
            value = 0;
            if (!TryReadDecimal(sheet, row, headerMap, header, out var number)) return false;

            value = (int)Math.Round(number, MidpointRounding.AwayFromZero);
            return true;
        }

        /// <summary>Menerima ya/tidak, aktif/nonaktif, true/false, dan 1/0.</summary>
        public static bool? ReadBoolean(IXLWorksheet sheet, int row, Dictionary<string, int> headerMap, string header)
        {
            var text = ReadText(sheet, row, headerMap, header);
            if (text.Length == 0) return null;

            return text.Trim().ToLowerInvariant() switch
            {
                "ya" or "y" or "true" or "1" or "aktif" or "benar" => true,
                "tidak" or "t" or "n" or "false" or "0" or "nonaktif" or "salah" => false,
                _ => null
            };
        }

        /// <summary>true bila baris ini adalah baris contoh bawaan template.</summary>
        public static bool IsSampleRow(IXLWorksheet sheet, int row, Dictionary<string, int> headerMap, string firstHeader)
            => ReadText(sheet, row, headerMap, firstHeader)
                .StartsWith(SampleMarker, StringComparison.OrdinalIgnoreCase);

        private static bool Matches(string a, string b)
            => string.Equals(Normalise(a), Normalise(b), StringComparison.OrdinalIgnoreCase);

        private static string Normalise(string value)
            => new string(value.Where(ch => !char.IsWhiteSpace(ch)).ToArray());
    }
}
