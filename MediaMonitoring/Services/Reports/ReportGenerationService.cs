using ClosedXML.Excel;
using MediaMonitoring.Data;
using MediaMonitoring.Models;
using Microsoft.EntityFrameworkCore;

namespace MediaMonitoring.Services.Reports
{
    public class ReportGenerationService
    {
        private readonly MediaMonitoringContext _context;
        private readonly OsintEngineService _osintService;

        public ReportGenerationService(MediaMonitoringContext context, OsintEngineService osintService)
        {
            _context = context;
            _osintService = osintService;
        }

        public async Task<byte[]> GenerateExcelReportAsync(DateTime startDate, DateTime endDate, string? category = null, string? sentiment = null)
        {
            var query = _context.MediaPosts
                .Where(p => p.CreatedAt >= startDate && p.CreatedAt <= endDate)
                .AsQueryable();

            if (!string.IsNullOrEmpty(category)) query = query.Where(p => p.Category == category);
            if (!string.IsNullOrEmpty(sentiment)) query = query.Where(p => p.Sentiment == sentiment);

            var posts = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Media Monitoring Report");

            // Headers
            var headers = new[] { "Date", "Time", "Source", "Author", "Content", "Sentiment", "Score", "Category", "Location" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.Black;
                cell.Style.Font.FontColor = XLColor.White;
            }

            // Data
            int row = 2;
            foreach (var post in posts)
            {
                worksheet.Cell(row, 1).Value = post.CreatedAt.ToString("yyyy-MM-dd");
                worksheet.Cell(row, 2).Value = post.CreatedAt.ToString("HH:mm:ss");
                worksheet.Cell(row, 3).Value = post.Source;
                worksheet.Cell(row, 4).Value = post.Author;
                worksheet.Cell(row, 5).Value = post.Content.Length > 200 ? post.Content.Substring(0, 200) + "..." : post.Content;
                worksheet.Cell(row, 6).Value = post.Sentiment;
                worksheet.Cell(row, 7).Value = post.SentimentScore;
                worksheet.Cell(row, 8).Value = post.Category;
                worksheet.Cell(row, 9).Value = post.Location;
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<string> GeneratePdfReportHtmlAsync(DateTime startDate, DateTime endDate)
        {
            var stats = await _osintService.GetDashboardStatsAsync();
            var posts = await _context.MediaPosts
                .Where(p => p.CreatedAt >= startDate && p.CreatedAt <= endDate)
                .OrderByDescending(p => p.CreatedAt)
                .Take(50)
                .ToListAsync();

            return $@"<!DOCTYPE html>
<html>
<head><style>
body {{ font-family: Arial, sans-serif; padding: 40px; }}
.header {{ border: 3px solid #000; padding: 20px; margin-bottom: 30px; }}
.stats {{ display: grid; grid-template-columns: repeat(4, 1fr); gap: 20px; margin: 30px 0; }}
.stat-box {{ border: 2px solid #000; padding: 20px; text-align: center; }}
table {{ width: 100%; border-collapse: collapse; }}
th, td {{ border: 1px solid #000; padding: 8px; }}
th {{ background: #000; color: white; }}
</style></head>
<body>
    <div class='header'>
        <h1>Media Monitoring OSINT Report</h1>
        <p>Period: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}</p>
    </div>
    <div class='stats'>
        <div class='stat-box'><h3>{stats["TotalPosts"]}</h3><p>Total Posts</p></div>
        <div class='stat-box'><h3 style='color:green'>{stats["Positive"]}</h3><p>Positive</p></div>
        <div class='stat-box'><h3 style='color:red'>{stats["Negative"]}</h3><p>Negative</p></div>
        <div class='stat-box'><h3>{stats["Neutral"]}</h3><p>Neutral</p></div>
    </div>
    <h2>Recent Posts</h2>
    <table>
        <thead><tr><th>Date</th><th>Source</th><th>Sentiment</th><th>Content</th></tr></thead>
        <tbody>
            {string.Join("", posts.Select(p => $"<tr><td>{p.CreatedAt:yyyy-MM-dd HH:mm}</td><td>{p.Source}</td><td>{p.Sentiment}</td><td>{p.Content.Substring(0, Math.Min(80, p.Content.Length))}...</td></tr>"))}
        </tbody>
    </table>
</body>
</html>";
        }
    }
}