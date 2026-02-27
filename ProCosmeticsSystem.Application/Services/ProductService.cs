using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.DTOs.Products;
using ProCosmeticsSystem.Application.Exceptions;
using ProCosmeticsSystem.Application.Interfaces;
using ProCosmeticsSystem.Domain.Entities;

namespace ProCosmeticsSystem.Application.Services;

public class ProductService
{
    private readonly IProductRepository _repo;
    private readonly IInventoryRepository _inventoryRepo;
    private readonly ICurrentUserService _currentUser;

    public ProductService(IProductRepository repo, IInventoryRepository inventoryRepo, ICurrentUserService currentUser)
    {
        _repo = repo;
        _inventoryRepo = inventoryRepo;
        _currentUser = currentUser;
    }

    public Task<PagedResult<ProductDto>> GetAllAsync(int page, int pageSize, string? search, int? categoryId)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize > 100 ? 100 : pageSize;
        return _repo.GetAllAsync(page, pageSize, search, categoryId);
    }

    public async Task<ProductDto> GetByIdAsync(int id)
    {
        return await _repo.GetByIdAsync(id) ?? throw new NotFoundException("Product", id);
    }

    public async Task<BarcodeLookupResult> GetByBarcodeAsync(string barcode)
    {
        return await _repo.GetByBarcodeAsync(barcode) ?? throw new NotFoundException("Product", barcode);
    }

    public async Task<int> CreateAsync(CreateProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException("Name", "Product name is required.");
        if (request.SalePrice < 0)
            throw new ValidationException("SalePrice", "Sale price cannot be negative.");

        var product = new Product
        {
            Name = request.Name,
            SKU = request.SKU,
            Barcode = request.Barcode,
            Description = request.Description,
            CategoryId = request.CategoryId,
            CostPrice = request.CostPrice,
            SalePrice = request.SalePrice,
            ReorderLevel = request.ReorderLevel,
            CreatedBy = _currentUser.UserId
        };

        var id = await _repo.CreateAsync(product);

        await _inventoryRepo.CreateAsync(new Inventory
        {
            ProductId = id,
            QuantityOnHand = 0,
            QuantityReserved = 0
        });

        return id;
    }

    public async Task UpdateAsync(int id, UpdateProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException("Name", "Product name is required.");

        _ = await _repo.GetByIdAsync(id) ?? throw new NotFoundException("Product", id);

        var product = new Product
        {
            Id = id,
            Name = request.Name,
            SKU = request.SKU,
            Barcode = request.Barcode,
            Description = request.Description,
            CategoryId = request.CategoryId,
            CostPrice = request.CostPrice,
            SalePrice = request.SalePrice,
            ReorderLevel = request.ReorderLevel,
            IsActive = request.IsActive,
            UpdatedBy = _currentUser.UserId,
            UpdatedAt = DateTime.UtcNow
        };

        await _repo.UpdateAsync(product);
    }

    public async Task DeleteAsync(int id)
    {
        _ = await _repo.GetByIdAsync(id) ?? throw new NotFoundException("Product", id);
        await _repo.SoftDeleteAsync(id);
    }

    public async Task<List<BarcodeLabelItem>> GetByIdsForLabelsAsync(List<int> productIds)
    {
        if (productIds == null || productIds.Count == 0)
            throw new ValidationException("ProductIds", "At least one product ID is required.");
        if (productIds.Count > 100)
            throw new ValidationException("ProductIds", "Cannot print labels for more than 100 products at once.");

        var items = await _repo.GetByIdsForLabelsAsync(productIds);
        if (items.Count == 0)
            throw new NotFoundException("Products", string.Join(", ", productIds));

        return items;
    }
}
