using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace MyPoS.Services
{
    public class ExportService
    {
        public byte[] ExportToExcel<T>(IEnumerable<T> data, string sheetName = "Sheet1")
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add(sheetName);
            ws.Cell(1, 1).InsertTable(data);
            
            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        public byte[] ExportToCsv<T>(IEnumerable<T> data)
        {
            using var ms = new MemoryStream();
            using var writer = new StreamWriter(ms);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            
            csv.WriteRecords(data);
            writer.Flush();
            return ms.ToArray();
        }
    }
}
