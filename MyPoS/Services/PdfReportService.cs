using System;
using System.Collections.Generic;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MyPoS.Services
{
    /// <summary>Satu kolom pada tabel laporan PDF.</summary>
    /// <param name="Header">Judul kolom.</param>
    /// <param name="Width">Bobot lebar relatif terhadap kolom lain.</param>
    /// <param name="AlignRight">true untuk kolom nominal dan angka.</param>
    /// <param name="Value">Cara mengambil nilai sel dari satu baris data.</param>
    public record PdfColumn<T>(string Header, float Width, bool AlignRight, Func<T, string> Value);

    /// <summary>Satu angka ringkasan yang tampil di kepala laporan.</summary>
    public record PdfSummary(string Label, string Value);

    /// <summary>
    /// Menyusun laporan PDF dengan tata letak yang sama seperti antarmuka: judul tebal,
    /// baris ringkasan, tabel bergaris tipis, dan nominal rata kanan dengan huruf monospace
    /// supaya kolom angkanya lurus. Nomor halaman dan waktu cetak selalu disertakan agar
    /// berkas yang tercetak dapat dipertanggungjawabkan.
    /// </summary>
    public class PdfReportService
    {
        // Warna diambil dari token yang sama dengan antarmuka aplikasi.
        private const string Ink = "#1A1714";
        private const string InkSoft = "#756B5F";
        private const string Line = "#E2D9CB";
        private const string Brand = "#B3382B";
        private const string Sunken = "#FAF7F1";

        private readonly SettingsService _settings;

        static PdfReportService()
        {
            // QuestPDF Community License: bebas dipakai organisasi dengan pendapatan
            // tahunan di bawah 1 juta USD. Lihat https://www.questpdf.com/license.
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public PdfReportService(SettingsService settings)
        {
            _settings = settings;
        }

        public byte[] Build<T>(
            string title,
            string subtitle,
            IReadOnlyList<T> rows,
            IReadOnlyList<PdfColumn<T>> columns,
            IReadOnlyList<PdfSummary>? summaries = null,
            IReadOnlyList<string>? filters = null,
            bool landscape = false)
        {
            var store = _settings.Current;
            var printedAt = DateTime.Now;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(landscape ? PageSizes.A4.Landscape() : PageSizes.A4);
                    page.Margin(28);
                    page.DefaultTextStyle(x => x.FontSize(9).FontColor(Ink).FontFamily(Fonts.Calibri));

                    page.Header().Element(header => ComposeHeader(header, store, title, subtitle, printedAt));
                    page.Content().PaddingVertical(10).Element(content =>
                        ComposeContent(content, rows, columns, summaries, filters));
                    page.Footer().Element(footer => ComposeFooter(footer, store));
                });
            }).GeneratePdf();
        }

        private static void ComposeHeader(IContainer container, PosSettings store, string title, string subtitle, DateTime printedAt)
        {
            container.Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(left =>
                    {
                        left.Item().Text(store.StoreName.ToUpperInvariant())
                            .FontSize(15).Bold().FontColor(Brand).LetterSpacing(0.05f);

                        if (!string.IsNullOrWhiteSpace(store.StoreAddress))
                            left.Item().Text(store.StoreAddress).FontSize(8).FontColor(InkSoft);

                        var contact = string.Join("  ·  ", new[]
                        {
                            string.IsNullOrWhiteSpace(store.StorePhone) ? null : $"Telp. {store.StorePhone}",
                            string.IsNullOrWhiteSpace(store.StoreTaxId) ? null : $"NPWP {store.StoreTaxId}"
                        }.Where(x => x is not null));

                        if (contact.Length > 0)
                            left.Item().Text(contact).FontSize(8).FontColor(InkSoft);
                    });

                    row.ConstantItem(190).Column(right =>
                    {
                        right.Item().AlignRight().Text(title).FontSize(13).Bold();
                        right.Item().AlignRight().Text(subtitle).FontSize(8).FontColor(InkSoft);
                        right.Item().AlignRight().Text($"Dicetak {printedAt:dd/MM/yyyy HH:mm}")
                            .FontSize(8).FontColor(InkSoft);
                    });
                });

                column.Item().PaddingTop(8).LineHorizontal(1.2f).LineColor(Brand);
            });
        }

        private void ComposeContent<T>(
            IContainer container,
            IReadOnlyList<T> rows,
            IReadOnlyList<PdfColumn<T>> columns,
            IReadOnlyList<PdfSummary>? summaries,
            IReadOnlyList<string>? filters)
        {
            container.Column(column =>
            {
                if (filters is { Count: > 0 })
                {
                    column.Item().PaddingBottom(8).Text(string.Join("   |   ", filters))
                        .FontSize(8).FontColor(InkSoft);
                }

                if (summaries is { Count: > 0 })
                {
                    column.Item().PaddingBottom(12).Row(row =>
                    {
                        foreach (var summary in summaries)
                        {
                            row.RelativeItem().Border(1).BorderColor(Line).Background(Sunken)
                                .PaddingVertical(7).PaddingHorizontal(9)
                                .Column(cell =>
                                {
                                    cell.Item().Text(summary.Label.ToUpperInvariant())
                                        .FontSize(7).Bold().FontColor(InkSoft).LetterSpacing(0.08f);
                                    cell.Item().PaddingTop(2).Text(summary.Value)
                                        .FontSize(12).Bold().FontFamily(Fonts.Consolas);
                                });
                            row.ConstantItem(6);
                        }
                    });
                }

                if (rows.Count == 0)
                {
                    column.Item().PaddingVertical(40).AlignCenter()
                        .Text("Tidak ada data pada rentang yang dipilih.")
                        .FontSize(10).FontColor(InkSoft);
                    return;
                }

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(definition =>
                    {
                        foreach (var col in columns)
                            definition.RelativeColumn(col.Width);
                    });

                    // Kepala tabel diulang di setiap halaman supaya laporan panjang tetap terbaca.
                    table.Header(header =>
                    {
                        foreach (var col in columns)
                        {
                            // Perataan harus menjadi bagian dari rantai sebelum Text dipanggil:
                            // QuestPDF hanya menerima satu anak per kontainer, jadi memanggil
                            // Text lalu AlignRight pada kontainer yang sama akan menimpa isinya.
                            var cell = header.Cell().Background(Sunken)
                                .BorderBottom(1).BorderColor(Line)
                                .PaddingVertical(5).PaddingHorizontal(4);

                            if (col.AlignRight) cell = cell.AlignRight();

                            cell.Text(col.Header.ToUpperInvariant())
                                .FontSize(7).Bold().FontColor(InkSoft).LetterSpacing(0.06f);
                        }
                    });

                    foreach (var row in rows)
                    {
                        foreach (var col in columns)
                        {
                            var cell = table.Cell()
                                .BorderBottom(0.5f).BorderColor(Line)
                                .PaddingVertical(4).PaddingHorizontal(4);

                            if (col.AlignRight)
                            {
                                cell.AlignRight().Text(col.Value(row))
                                    .FontSize(8.5f).FontFamily(Fonts.Consolas);
                            }
                            else
                            {
                                cell.Text(col.Value(row)).FontSize(8.5f);
                            }
                        }
                    }
                });

                column.Item().PaddingTop(6).Text($"{rows.Count} baris")
                    .FontSize(7.5f).FontColor(InkSoft);
            });
        }

        private static void ComposeFooter(IContainer container, PosSettings store)
        {
            container.Column(column =>
            {
                column.Item().PaddingBottom(4).LineHorizontal(0.5f).LineColor(Line);
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text(store.StoreName).FontSize(7.5f).FontColor(InkSoft);
                    row.RelativeItem().AlignRight().Text(text =>
                    {
                        text.DefaultTextStyle(x => x.FontSize(7.5f).FontColor(InkSoft));
                        text.Span("Halaman ");
                        text.CurrentPageNumber();
                        text.Span(" dari ");
                        text.TotalPages();
                    });
                });
            });
        }
    }
}
