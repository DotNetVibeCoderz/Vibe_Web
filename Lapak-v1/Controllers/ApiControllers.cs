using Lapak.Services.Payment;
using Microsoft.AspNetCore.Mvc;

namespace Lapak.Controllers;

/// <summary>
/// Payment gateway callback/notification endpoints
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

    /// <summary>
    /// Midtrans payment notification callback
    /// </summary>
    [HttpPost("midtrans-callback")]
    public async Task<IActionResult> MidtransCallback(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var rawBody = await reader.ReadToEndAsync(ct);

        _logger.LogInformation("Midtrans callback received: {Body}", rawBody);

        var result = await _paymentService.ProcessCallbackAsync("midtrans", rawBody, ct);

        if (result.Success)
            return Ok(new { status = "success", message = "Callback processed" });

        return BadRequest(new { status = "error", message = result.ErrorMessage });
    }

    /// <summary>
    /// Xendit payment notification callback
    /// </summary>
    [HttpPost("xendit-callback")]
    public async Task<IActionResult> XenditCallback(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var rawBody = await reader.ReadToEndAsync(ct);

        // Verify Xendit callback token header
        var callbackToken = Request.Headers["x-callback-token"].FirstOrDefault();
        _logger.LogInformation("Xendit callback received. Token: {Token}", callbackToken);

        var result = await _paymentService.ProcessCallbackAsync("xendit", rawBody, ct);

        if (result.Success)
            return Ok(new { status = "success", message = "Callback processed" });

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
