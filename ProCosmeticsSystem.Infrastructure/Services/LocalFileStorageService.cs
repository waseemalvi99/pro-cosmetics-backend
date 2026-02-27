using ProCosmeticsSystem.Application.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

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

    public async Task<(string fileName, string filePath)> SaveImageAsync(Stream stream, string originalFileName, string subfolder, int maxWidth = 1200, int maxHeight = 1200, int quality = 80)
    {
        var fileName = $"{Guid.NewGuid()}.webp";
        var directory = Path.Combine(_basePath, subfolder);
        Directory.CreateDirectory(directory);

        using var image = await Image.LoadAsync(stream);

        // Resize if exceeds max dimensions while preserving aspect ratio
        if (image.Width > maxWidth || image.Height > maxHeight)
        {
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(maxWidth, maxHeight),
                Mode = ResizeMode.Max
            }));
        }

        // Strip EXIF metadata and auto-orient
        image.Mutate(x => x.AutoOrient());
        image.Metadata.ExifProfile = null;

        var fullPath = Path.Combine(directory, fileName);
        var encoder = new WebpEncoder { Quality = quality };
        await image.SaveAsync(fullPath, encoder);

        return (fileName, fileName);
    }

    public void DeleteFile(string filePath)
    {
        var fullPath = Path.Combine(_basePath, filePath);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }
}
