using System.Globalization;
using System.Reflection;
using System.Text;
using ClosedXML.Excel;

namespace VirtualDoctor.Services.Export;

public interface IExportService
{
    byte[] ToCsv<T>(IEnumerable<T> data);
    byte[] ToExcel<T>(IEnumerable<T> data, string sheetName);
}

public class ExportService : IExportService
{
    public byte[] ToCsv<T>(IEnumerable<T> data)
    {
        var items = data.ToList();
        var props = GetExportableProps(typeof(T));

        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", props.Select(p => EscapeCsv(p.Name))));

        foreach (var item in items)
        {
            var values = props.Select(p => FormatCsvValue(p.GetValue(item)));
            sb.AppendLine(string.Join(",", values));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public byte[] ToExcel<T>(IEnumerable<T> data, string sheetName)
    {
        var items = data.ToList();
        var props = GetExportableProps(typeof(T));

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(sheetName);

        for (int i = 0; i < props.Length; i++)
        {
            ws.Cell(1, i + 1).Value = props[i].Name;
            ws.Cell(1, i + 1).Style.Font.Bold = true;
        }

        for (int r = 0; r < items.Count; r++)
        {
            for (int c = 0; c < props.Length; c++)
            {
                var val = props[c].GetValue(items[r]);
                ws.Cell(r + 2, c + 1).Value = val?.ToString() ?? string.Empty;
            }
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static PropertyInfo[] GetExportableProps(Type type)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => IsSimple(p.PropertyType))
            .ToArray();
    }

    private static bool IsSimple(Type type)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;
        return t.IsPrimitive || t.IsEnum || t == typeof(string) || t == typeof(decimal) || t == typeof(DateTime) || t == typeof(DateTimeOffset) || t == typeof(Guid) || t == typeof(TimeSpan);
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains('"')) value = value.Replace("\"", "\"\"");
        if (value.Contains(',') || value.Contains('\n') || value.Contains('\r')) value = $"\"{value}\"";
        return value;
    }

    private static string FormatCsvValue(object? value)
    {
        if (value == null) return string.Empty;
        if (value is DateTime dt) return EscapeCsv(dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        if (value is DateTimeOffset dto) return EscapeCsv(dto.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        if (value is TimeSpan ts) return EscapeCsv(ts.ToString());
        return EscapeCsv(value.ToString() ?? string.Empty);
    }
}
