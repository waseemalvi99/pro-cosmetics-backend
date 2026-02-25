namespace ProCosmeticsSystem.Application.Interfaces;

public interface IFileStorageService
{
    Task<(string fileName, string filePath)> SaveFileAsync(Stream stream, string originalFileName, string subfolder);
    void DeleteFile(string filePath);
}
