using QRCoder;

namespace HotelBuffetPass.Services
{
    public class QRCodeService
    {
        // Returns a base64 PNG string ready to embed in <img src="data:image/png;base64,...">
        public string GenerateQRCodeBase64(string content)
        {
            using var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeBytes = qrCode.GetGraphic(10);
            return Convert.ToBase64String(qrCodeBytes);
        }
    }
}
