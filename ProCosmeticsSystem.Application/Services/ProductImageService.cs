using ProCosmeticsSystem.Application.DTOs.Products;
using ProCosmeticsSystem.Application.Exceptions;
using ProCosmeticsSystem.Application.Interfaces;
using ProCosmeticsSystem.Domain.Entities;

namespace ProCosmeticsSystem.Application.Services;

public class ProductImageService
{
    private readonly IProductImageRepository _imageRepo;
    private readonly IProductRepository _productRepo;
    private readonly IFileStorageService _fileStorage;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".tiff"
    };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/gif", "image/webp", "image/bmp", "image/tiff"
    };

    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
    private const int MaxImagesPerProduct = 10;

    public ProductImageService(IProductImageRepository imageRepo, IProductRepository productRepo, IFileStorageService fileStorage)
    {
        _imageRepo = imageRepo;
        _productRepo = productRepo;
        _fileStorage = fileStorage;
    }

    public async Task<List<ProductImageDto>> GetByProductIdAsync(int productId)
    {
        var images = await _imageRepo.GetByProductIdAsync(productId);
        return images.Select(i => new ProductImageDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            FileName = i.FileName,
            Url = $"/uploads/products/{i.FilePath}",
            IsPrimary = i.IsPrimary,
            SortOrder = i.SortOrder
        }).ToList();
    }

    public async Task<ProductImageDto> UploadAsync(int productId, Stream fileStream, string originalFileName, string? contentType, long fileSize)
    {
        _ = await _productRepo.GetByIdAsync(productId) ?? throw new NotFoundException("Product", productId);

        ValidateImage(originalFileName, contentType, fileSize);

        var existingImages = await _imageRepo.GetByProductIdAsync(productId);
        if (existingImages.Count >= MaxImagesPerProduct)
            throw new ValidationException("images", $"A product can have a maximum of {MaxImagesPerProduct} images.");

        var (fileName, filePath) = await _fileStorage.SaveImageAsync(fileStream, originalFileName, "products");
        var sortOrder = await _imageRepo.GetMaxSortOrderAsync(productId) + 1;

        var image = new ProductImage
        {
            ProductId = productId,
            FileName = fileName,
            FilePath = filePath,
            IsPrimary = existingImages.Count == 0,
            SortOrder = sortOrder
        };

        var id = await _imageRepo.CreateAsync(image);

        return new ProductImageDto
        {
            Id = id,
            ProductId = productId,
            FileName = fileName,
            Url = $"/uploads/products/{filePath}",
            IsPrimary = image.IsPrimary,
            SortOrder = sortOrder
        };
    }

    public async Task<List<ProductImageDto>> UploadMultipleAsync(int productId, IList<(Stream stream, string fileName, string? contentType, long size)> files)
    {
        _ = await _productRepo.GetByIdAsync(productId) ?? throw new NotFoundException("Product", productId);

        var existingImages = await _imageRepo.GetByProductIdAsync(productId);
        if (existingImages.Count + files.Count > MaxImagesPerProduct)
            throw new ValidationException("images", $"A product can have a maximum of {MaxImagesPerProduct} images. Current: {existingImages.Count}, uploading: {files.Count}.");

        foreach (var (_, fileName, contentType, size) in files)
            ValidateImage(fileName, contentType, size);

        var results = new List<ProductImageDto>();
        var sortOrder = await _imageRepo.GetMaxSortOrderAsync(productId);

        for (int i = 0; i < files.Count; i++)
        {
            var (stream, originalFileName, _, _) = files[i];
            sortOrder++;

            var (savedFileName, filePath) = await _fileStorage.SaveImageAsync(stream, originalFileName, "products");

            var image = new ProductImage
            {
                ProductId = productId,
                FileName = savedFileName,
                FilePath = filePath,
                IsPrimary = existingImages.Count == 0 && i == 0,
                SortOrder = sortOrder
            };

            var id = await _imageRepo.CreateAsync(image);

            results.Add(new ProductImageDto
            {
                Id = id,
                ProductId = productId,
                FileName = savedFileName,
                Url = $"/uploads/products/{filePath}",
                IsPrimary = image.IsPrimary,
                SortOrder = sortOrder
            });
        }

        return results;
    }

    public async Task DeleteAsync(int productId, int imageId)
    {
        var image = await _imageRepo.GetByIdAsync(imageId)
            ?? throw new NotFoundException("ProductImage", imageId);

        if (image.ProductId != productId)
            throw new AppException("Image does not belong to this product.");

        _fileStorage.DeleteFile(Path.Combine("products", image.FilePath));
        await _imageRepo.DeleteAsync(imageId);
    }

    public async Task SetPrimaryAsync(int productId, int imageId)
    {
        var image = await _imageRepo.GetByIdAsync(imageId)
            ?? throw new NotFoundException("ProductImage", imageId);

        if (image.ProductId != productId)
            throw new AppException("Image does not belong to this product.");

        await _imageRepo.SetPrimaryAsync(productId, imageId);
    }

    private static void ValidateImage(string fileName, string? contentType, long fileSize)
    {
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
            throw new ValidationException("file", $"Invalid image file type '{extension}'. Allowed: {string.Join(", ", AllowedExtensions)}");

        if (!string.IsNullOrEmpty(contentType) && !AllowedContentTypes.Contains(contentType))
            throw new ValidationException("file", $"Invalid content type '{contentType}'.");

        if (fileSize <= 0)
            throw new ValidationException("file", "File is empty.");

        if (fileSize > MaxFileSizeBytes)
            throw new ValidationException("file", $"File size {fileSize / (1024 * 1024.0):F1} MB exceeds the maximum of {MaxFileSizeBytes / (1024 * 1024)} MB.");
    }
}
