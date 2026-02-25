namespace ProCosmeticsSystem.Application.Interfaces;

public interface IBarcodeService
{
    byte[] GenerateBarcodeImage(string content, int width = 300, int height = 100);
}
