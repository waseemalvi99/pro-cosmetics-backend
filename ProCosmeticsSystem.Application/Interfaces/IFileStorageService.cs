namespace ProCosmeticsSystem.Application.Interfaces;

public interface IFileStorageService
{
    Task<(string fileName, string filePath)> SaveFileAsync(Stream stream, string originalFileName, string subfolder);
    Task<(string fileName, string filePath)> SaveImageAsync(Stream stream, string originalFileName, string subfolder, int maxWidth = 1200, int maxHeight = 1200, int quality = 80);
    void DeleteFile(string filePath);
}
