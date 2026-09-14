using System.Security.Cryptography;
using QRCoder;
using Restaurant.Application.Interfaces;

namespace Restaurant.Infrastructure.Services;

public class QrCodeService : IQrCodeService
{
    public string GenerateSecureToken()
    {
        var randomBytes = new byte[24];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        return Convert.ToBase64String(randomBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    public byte[] GenerateQrCodeImage(string url)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        return qrCode.GetGraphic(20);
    }
}
