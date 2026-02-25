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

    public async Task<ProductImageDto> UploadAsync(int productId, Stream fileStream, string originalFileName)
    {
        _ = await _productRepo.GetByIdAsync(productId) ?? throw new NotFoundException("Product", productId);

        var (fileName, filePath) = await _fileStorage.SaveFileAsync(fileStream, originalFileName, "products");
        var sortOrder = await _imageRepo.GetMaxSortOrderAsync(productId) + 1;
        var existingImages = await _imageRepo.GetByProductIdAsync(productId);

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
}
