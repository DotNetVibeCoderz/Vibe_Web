using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;

namespace QueueKiosk.Services;

public static class QRCodeGeneratorService
{
    public static string GenerateBase64QRCode(string text)
    {
        using QRCodeGenerator qrGenerator = new QRCodeGenerator();
        using QRCodeData qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        using PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
        byte[] qrCodeImage = qrCode.GetGraphic(20);
        return $"data:image/png;base64,{Convert.ToBase64String(qrCodeImage)}";
    }
}
