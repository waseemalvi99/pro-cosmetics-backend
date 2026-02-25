using ProCosmeticsSystem.Application.Interfaces;

namespace ProCosmeticsSystem.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;

    public LocalFileStorageService(string webRootPath)
    {
        _basePath = Path.Combine(webRootPath, "uploads");
    }

    public async Task<(string fileName, string filePath)> SaveFileAsync(Stream stream, string originalFileName, string subfolder)
    {
        var extension = Path.GetExtension(originalFileName);
        var fileName = $"{Guid.NewGuid()}{extension}";
        var directory = Path.Combine(_basePath, subfolder);

        Directory.CreateDirectory(directory);

        var fullPath = Path.Combine(directory, fileName);
        using var fileStream = new FileStream(fullPath, FileMode.Create);
        await stream.CopyToAsync(fileStream);

        return (fileName, fileName);
    }

    public void DeleteFile(string filePath)
    {
        var fullPath = Path.Combine(_basePath, filePath);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }
}
