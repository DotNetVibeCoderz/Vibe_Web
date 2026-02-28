using System.Collections.Generic;
using System.IO;
using CsvHelper;
using ClosedXML.Excel;
using System.Globalization;

namespace ERM.Services
{
    public class ExportService
    {
        public byte[] ExportToExcel<T>(IEnumerable<T> data, string sheetName)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(sheetName);
            worksheet.Cell(1, 1).InsertTable(data);
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public byte[] ExportToCsv<T>(IEnumerable<T> data)
        {
            using var stream = new MemoryStream();
            using var writer = new StreamWriter(stream);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.WriteRecords(data);
            writer.Flush();
            return stream.ToArray();
        }
    }
}