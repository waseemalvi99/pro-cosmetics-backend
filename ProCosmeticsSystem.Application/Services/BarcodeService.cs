using ProCosmeticsSystem.Application.Interfaces;
using ZXing;
using ZXing.Common;
using ZXing.SkiaSharp;
using SkiaSharp;

namespace ProCosmeticsSystem.Application.Services;

public class BarcodeService : IBarcodeService
{
    public byte[] GenerateBarcodeImage(string content, int width = 300, int height = 100)
    {
        var writer = new BarcodeWriter
        {
            Format = BarcodeFormat.CODE_128,
            Options = new EncodingOptions
            {
                Width = width,
                Height = height,
                Margin = 10,
                PureBarcode = false
            }
        };

        using var bitmap = writer.Write(content);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
