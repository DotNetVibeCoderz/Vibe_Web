namespace StockAnalyzer.Services.LLM;

/// <summary>
/// Main LLM service implementation.
/// Routes analysis requests to appropriate providers.
/// </summary>
public class LLMService : ILLMService
{
    private readonly ILLMProviderFactory _factory;
    private readonly ILogger<LLMService> _logger;

    public LLMService(ILLMProviderFactory factory, ILogger<LLMService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    /// <summary>
    /// Send analysis request to default provider.
    /// </summary>
    public async Task<LLMResponse> AnalyzeAsync(LLMRequest request)
    {
        var provider = _factory.GetDefaultProvider();
        if (provider == null)
            return new LLMResponse { IsSuccess = false, ErrorMessage = "No default LLM provider configured" };

        return await provider.SendMessageAsync(request);
    }

    /// <summary>
    /// Send analysis request to a specific provider.
    /// </summary>
    public async Task<LLMResponse> AnalyzeWithProviderAsync(LLMRequest request, string providerName)
    {
        var provider = _factory.GetProvider(providerName);
        if (provider == null)
            return new LLMResponse { IsSuccess = false, ErrorMessage = $"Provider '{providerName}' not found" };

        return await provider.SendMessageAsync(request);
    }

    /// <summary>
    /// Get technical analysis review from LLM.
    /// </summary>
    public async Task<LLMResponse> GetTechnicalReviewAsync(string stockCode, string technicalData)
    {
        var provider = _factory.GetProviderForAnalysisType("TechnicalReview");
        if (provider == null)
            return new LLMResponse { IsSuccess = false, ErrorMessage = "No provider for technical review" };

        var request = new LLMRequest
        {
            SystemPrompt = "Anda adalah analis teknikal saham profesional. Berikan ulasan teknikal berdasarkan data yang diberikan dalam Bahasa Indonesia. " +
                          "Sertakan analisa tren, support/resistance, indikator (RSI, MACD, MA, Bollinger Bands), dan sinyal trading.",
            UserPrompt = $"Berikut data teknikal untuk saham {stockCode}:\n\n{technicalData}\n\n" +
                        "Berikan ulasan teknikal lengkap dalam Bahasa Indonesia."
        };

        return await provider.SendMessageAsync(request);
    }

    /// <summary>
    /// Get fundamental analysis review from LLM.
    /// </summary>
    public async Task<LLMResponse> GetFundamentalReviewAsync(string stockCode, string fundamentalData)
    {
        var provider = _factory.GetProviderForAnalysisType("FundamentalReview");
        if (provider == null)
            return new LLMResponse { IsSuccess = false, ErrorMessage = "No provider for fundamental review" };

        var request = new LLMRequest
        {
            SystemPrompt = "Anda adalah analis fundamental saham profesional. Berikan ulasan fundamental berdasarkan data rasio keuangan " +
                          "yang diberikan dalam Bahasa Indonesia. Sertakan analisa valuasi, profitabilitas, solvabilitas, dan pertumbuhan.",
            UserPrompt = $"Berikut data fundamental untuk saham {stockCode}:\n\n{fundamentalData}\n\n" +
                        "Berikan ulasan fundamental lengkap dalam Bahasa Indonesia."
        };

        return await provider.SendMessageAsync(request);
    }

    /// <summary>
    /// Get sentiment analysis from LLM.
    /// </summary>
    public async Task<LLMResponse> GetSentimentAnalysisAsync(string stockCode, string newsData)
    {
        var provider = _factory.GetProviderForAnalysisType("SentimentAnalysis");
        if (provider == null)
            return new LLMResponse { IsSuccess = false, ErrorMessage = "No provider for sentiment analysis" };

        var request = new LLMRequest
        {
            SystemPrompt = "Anda adalah analis sentimen pasar saham. Analisa sentimen dari berita yang diberikan dalam Bahasa Indonesia. " +
                          "Berikan kesimpulan apakah sentimen bullish, bearish, atau neutral beserta alasan.",
            UserPrompt = $"Berikut data berita untuk saham {stockCode}:\n\n{newsData}\n\n" +
                        "Berikan analisa sentimen dalam Bahasa Indonesia."
        };

        return await provider.SendMessageAsync(request);
    }

    /// <summary>
    /// Get comprehensive stock recommendation from LLM.
    /// </summary>
    public async Task<LLMResponse> GetStockRecommendationAsync(string stockCode, string technicalData,
        string fundamentalData, string sentimentData)
    {
        var provider = _factory.GetProviderForAnalysisType("StockRecommendation");
        if (provider == null)
            return new LLMResponse { IsSuccess = false, ErrorMessage = "No provider for stock recommendation" };

        var request = new LLMRequest
        {
            SystemPrompt = "Anda adalah penasihat investasi saham profesional. Berikan rekomendasi investasi berdasarkan " +
                          "data teknikal, fundamental, dan sentimen yang diberikan dalam Bahasa Indonesia. " +
                          "Format jawaban:\n" +
                          "1. Rekomendasi: [StrongBuy/Buy/Hold/Sell/StrongSell]\n" +
                          "2. Target Harga: [angka]\n" +
                          "3. Stop Loss: [angka]\n" +
                          "4. Level Risiko: [Low/Medium/High]\n" +
                          "5. Alasan: [penjelasan singkat]\n" +
                          "6. Katalis Positif: [...]\n" +
                          "7. Risiko: [...]",
            UserPrompt = $"Analisa saham {stockCode}:\n\n" +
                        $"DATA TEKNIKAL:\n{technicalData}\n\n" +
                        $"DATA FUNDAMENTAL:\n{fundamentalData}\n\n" +
                        $"DATA SENTIMEN:\n{sentimentData}\n\n" +
                        "Berikan rekomendasi lengkap dalam Bahasa Indonesia."
        };

        return await provider.SendMessageAsync(request);
    }

    /// <summary>
    /// Check if any provider is available.
    /// </summary>
    public async Task<bool> IsAnyProviderAvailableAsync()
    {
        foreach (var provider in _factory.GetAllProviders())
        {
            if (await provider.IsAvailableAsync())
                return true;
        }
        return false;
    }

    /// <summary>
    /// Get list of available provider names.
    /// </summary>
    public async Task<List<string>> GetAvailableProvidersAsync()
    {
        var available = new List<string>();
        foreach (var provider in _factory.GetAllProviders())
        {
            if (await provider.IsAvailableAsync())
                available.Add(provider.ProviderName);
        }
        return available;
    }
}
