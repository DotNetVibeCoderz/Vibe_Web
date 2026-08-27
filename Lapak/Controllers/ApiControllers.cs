using Lapak.Services.Payment;
using Microsoft.AspNetCore.Mvc;

namespace Lapak.Controllers;

/// <summary>
/// Payment gateway webhook endpoints. Each gateway gets its own route because the
/// URL is registered in that gateway's dashboard, but they share one handler —
/// signature and token checks live in the provider, not here.
/// </summary>
[ApiController]
[Route("api/payment")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(IPaymentService paymentService, ILogger<PaymentController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    [HttpPost("midtrans-callback")]
    public Task<IActionResult> MidtransCallback(CancellationToken ct) => HandleCallback("Midtrans", ct);

    [HttpPost("xendit-callback")]
    public Task<IActionResult> XenditCallback(CancellationToken ct) => HandleCallback("Xendit", ct);

    [HttpPost("stripe-callback")]
    public Task<IActionResult> StripeCallback(CancellationToken ct) => HandleCallback("Stripe", ct);

    private async Task<IActionResult> HandleCallback(string gateway, CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var rawBody = await reader.ReadToEndAsync(ct);

        var context = new PaymentCallbackContext
        {
            RawBody = rawBody,
            // Signature material travels in headers for Xendit and Stripe. Never log
            // these values — they authenticate the request.
            Headers = Request.Headers.ToDictionary(
                h => h.Key,
                h => h.Value.ToString(),
                StringComparer.OrdinalIgnoreCase)
        };

        var result = await _paymentService.ProcessCallbackAsync(gateway, context, ct);

        if (result.Success)
        {
            _logger.LogInformation("{Gateway} callback processed for order {OrderNumber}", gateway, result.OrderNumber);
            return Ok(new { status = "success", order = result.OrderNumber, state = result.State.ToString() });
        }

        if (result.Unauthorized)
        {
            _logger.LogWarning("{Gateway} callback rejected: {Reason}", gateway, result.ErrorMessage);
            return Unauthorized(new { status = "rejected", message = result.ErrorMessage });
        }

        _logger.LogWarning("{Gateway} callback failed: {Reason}", gateway, result.ErrorMessage);
        return BadRequest(new { status = "error", message = result.ErrorMessage });
    }
}

/// <summary>
/// File upload API controller for chat attachments
/// </summary>
[ApiController]
[Route("api/upload")]
public class UploadController : ControllerBase
{
    private readonly Lapak.Services.Storage.StorageServiceFactory _storageFactory;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<UploadController> _logger;

    public UploadController(
        Lapak.Services.Storage.StorageServiceFactory storageFactory,
        IWebHostEnvironment env,
        ILogger<UploadController> logger)
    {
        _storageFactory = storageFactory;
        _env = env;
        _logger = logger;
    }

    /// <summary>
    /// Upload file (image/document) for chat
    /// </summary>
    [HttpPost("chat-file")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10MB
    public async Task<IActionResult> UploadChatFile(IFormFile file, [FromQuery] string chatType = "TonyKurus", CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "File tidak boleh kosong." });

        try
        {
            var storage = _storageFactory.GetStorageService();
            var fileName = file.FileName;
            var contentType = file.ContentType ?? "application/octet-stream";

            using var stream = file.OpenReadStream();
            var storedPath = await storage.UploadAsync(fileName, stream, contentType);
            var publicUrl = await storage.GetPublicUrlAsync(storedPath);

            var isImage = contentType.StartsWith("image/");

            _logger.LogInformation("Chat file uploaded: {File} -> {Url} (type: {Type})", fileName, publicUrl, chatType);

            return Ok(new
            {
                url = publicUrl,
                fileName = fileName,
                contentType = contentType,
                isImage = isImage,
                size = file.Length
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File upload failed");
            return StatusCode(500, new { error = $"Upload gagal: {ex.Message}" });
        }
    }
}
