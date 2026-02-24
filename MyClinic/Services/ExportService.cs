using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using CsvHelper;
using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using MyClinic.Data;

namespace MyClinic.Services
{
    public class ExportService
    {
        public ExportService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] ExportToCsv<T>(IEnumerable<T> data)
        {
            using var memoryStream = new MemoryStream();
            using var streamWriter = new StreamWriter(memoryStream);
            using var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture);
            csvWriter.WriteRecords(data);
            streamWriter.Flush();
            return memoryStream.ToArray();
        }

        public byte[] ExportToExcel(IEnumerable<MyClinic.Data.MonthlyRevenue> data, string sheetName)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(sheetName);
            worksheet.Cell(1, 1).InsertTable(data);
            worksheet.Columns().AdjustToContents();

            using var memoryStream = new MemoryStream();
            workbook.SaveAs(memoryStream);
            return memoryStream.ToArray();
        }

        public byte[] ExportComprehensiveExcel(
            int year,
            IEnumerable<MonthlyRevenue> revenue,
            IEnumerable<PatientVisitStats> visits,
            IEnumerable<PaymentMethodStats> payments,
            IEnumerable<DiagnosisStats> diagnoses,
            IEnumerable<DoctorPerformanceStats> doctors)
        {
            using var workbook = new XLWorkbook();

            // Sheet 1: Revenue
            var ws1 = workbook.Worksheets.Add("Pendapatan");
            ws1.Cell(1, 1).Value = $"Laporan Pendapatan Tahun {year}";
            ws1.Range(1, 1, 1, 3).Merge().Style.Font.SetBold();
            ws1.Cell(3, 1).InsertTable(revenue);
            ws1.Columns().AdjustToContents();

            // Sheet 2: Visits
            var ws2 = workbook.Worksheets.Add("Kunjungan Pasien");
            ws2.Cell(1, 1).Value = $"Statistik Kunjungan Tahun {year}";
            ws2.Range(1, 1, 1, 2).Merge().Style.Font.SetBold();
            ws2.Cell(3, 1).InsertTable(visits);
            ws2.Columns().AdjustToContents();

            // Sheet 3: Payments
            var ws3 = workbook.Worksheets.Add("Metode Pembayaran");
            ws3.Cell(1, 1).Value = $"Metode Pembayaran {year}";
            ws3.Range(1, 1, 1, 2).Merge().Style.Font.SetBold();
            ws3.Cell(3, 1).InsertTable(payments);
            ws3.Columns().AdjustToContents();

            // Sheet 4: Diagnoses
            var ws4 = workbook.Worksheets.Add("Diagnosa Terbanyak");
            ws4.Cell(1, 1).Value = $"Top Diagnosa {year}";
            ws4.Range(1, 1, 1, 2).Merge().Style.Font.SetBold();
            ws4.Cell(3, 1).InsertTable(diagnoses);
            ws4.Columns().AdjustToContents();

            // Sheet 5: Doctors
            var ws5 = workbook.Worksheets.Add("Kinerja Dokter");
            ws5.Cell(1, 1).Value = $"Kinerja Dokter {year}";
            ws5.Range(1, 1, 1, 2).Merge().Style.Font.SetBold();
            ws5.Cell(3, 1).InsertTable(doctors);
            ws5.Columns().AdjustToContents();

            using var memoryStream = new MemoryStream();
            workbook.SaveAs(memoryStream);
            return memoryStream.ToArray();
        }

        public byte[] ExportRevenueToPdf(IEnumerable<MyClinic.Data.MonthlyRevenue> data)
        {
            // Fallback simplistic PDF
            return ExportComprehensivePdf(DateTime.Now.Year, data, new List<PatientVisitStats>(), new List<PaymentMethodStats>(), new List<DiagnosisStats>(), new List<DoctorPerformanceStats>());
        }

        public byte[] ExportComprehensivePdf(
            int year,
            IEnumerable<MonthlyRevenue> revenue,
            IEnumerable<PatientVisitStats> visits,
            IEnumerable<PaymentMethodStats> payments,
            IEnumerable<DiagnosisStats> diagnoses,
            IEnumerable<DoctorPerformanceStats> doctors)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Text($"Laporan Manajemen Klinik - Tahun {year}")
                        .SemiBold().FontSize(18).FontColor(Colors.Teal.Darken2);

                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                    {
                        col.Spacing(20);

                        // 1. Pendapatan
                        col.Item().Text("1. Pendapatan Bulanan").Bold().FontSize(12);
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderStyle).Text("Bulan");
                                header.Cell().Element(HeaderStyle).Text("Transaksi");
                                header.Cell().Element(HeaderStyle).Text("Pendapatan (Rp)");
                            });

                            foreach (var item in revenue)
                            {
                                table.Cell().Element(CellStyle).Text(item.Month);
                                table.Cell().Element(CellStyle).Text(item.PaidBillsCount.ToString());
                                table.Cell().Element(CellStyle).Text(item.TotalRevenue.ToString("N0"));
                            }
                        });

                        // 2. Kunjungan
                        col.Item().Text("2. Kunjungan Pasien").Bold().FontSize(12);
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderStyle).Text("Bulan");
                                header.Cell().Element(HeaderStyle).Text("Jumlah Pasien");
                            });

                            foreach (var item in visits)
                            {
                                table.Cell().Element(CellStyle).Text(item.Month);
                                table.Cell().Element(CellStyle).Text(item.VisitCount.ToString());
                            }
                        });

                        // 3. Kinerja Dokter
                        col.Item().Text("3. Kinerja Dokter").Bold().FontSize(12);
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderStyle).Text("Nama Dokter");
                                header.Cell().Element(HeaderStyle).Text("Pasien Ditangani");
                            });

                            foreach (var item in doctors)
                            {
                                table.Cell().Element(CellStyle).Text(item.DoctorName);
                                table.Cell().Element(CellStyle).Text(item.PatientCount.ToString());
                            }
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }

        static IContainer HeaderStyle(IContainer container)
        {
            return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5).DefaultTextStyle(x => x.SemiBold());
        }

        static IContainer CellStyle(IContainer container)
        {
            return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten4).PaddingVertical(5);
        }
    }
}
