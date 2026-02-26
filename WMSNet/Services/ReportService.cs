using ClosedXML.Excel;
using System.Data;
using System.IO;

namespace WMSNet.Services
{
    public class ReportService
    {
        public byte[] ExportToExcel<T>(List<T> data, string sheetName)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add(sheetName);
                
                // Reflection to get properties
                var properties = typeof(T).GetProperties();
                
                // Header
                for (int i = 0; i < properties.Length; i++)
                {
                    worksheet.Cell(1, i + 1).Value = properties[i].Name;
                }

                // Data
                for (int r = 0; r < data.Count; r++)
                {
                    for (int c = 0; c < properties.Length; c++)
                    {
                        var val = properties[c].GetValue(data[r]);
                        worksheet.Cell(r + 2, c + 1).Value = val?.ToString();
                    }
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }
    }
}