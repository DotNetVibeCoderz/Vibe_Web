using VirtualDoctor.Services.RAG;

namespace VirtualDoctor.Workers;

/// <summary>
/// Background worker untuk indexing PDF folder ke vector database secara berkala
/// </summary>
public class PdfIndexingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PdfIndexingWorker> _logger;
    private readonly Models.IndexingConfig _config;

    public PdfIndexingWorker(IServiceScopeFactory scopeFactory, ILogger<PdfIndexingWorker> logger, Models.AppConfig config)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _config = config.Indexing;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[PdfIndexingWorker] Started at {Time}", DateTimeOffset.Now);

        // Pastikan folder PDF ada
        var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", _config.PdfFolderPath);
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            _logger.LogInformation("[PdfIndexingWorker] Created PDF folder: {Folder}", folderPath);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var indexingService = scope.ServiceProvider.GetRequiredService<IDocumentIndexingService>();

                _logger.LogInformation("[PdfIndexingWorker] Starting indexing cycle...");
                await indexingService.IndexPdfFolderAsync(folderPath);
                _logger.LogInformation("[PdfIndexingWorker] Indexing cycle completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PdfIndexingWorker] Error during indexing cycle");
            }

            await Task.Delay(TimeSpan.FromMinutes(_config.IntervalMinutes), stoppingToken);
        }
    }
}
