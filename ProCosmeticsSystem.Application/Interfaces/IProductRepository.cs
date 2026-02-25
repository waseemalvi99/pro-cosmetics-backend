using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.DTOs.Products;
using ProCosmeticsSystem.Domain.Entities;

namespace ProCosmeticsSystem.Application.Interfaces;

public interface IProductRepository
{
    Task<PagedResult<ProductDto>> GetAllAsync(int page, int pageSize, string? search, int? categoryId);
    Task<ProductDto?> GetByIdAsync(int id);
    Task<BarcodeLookupResult?> GetByBarcodeAsync(string barcode);
    Task<int> CreateAsync(Product product);
    Task UpdateAsync(Product product);
    Task SoftDeleteAsync(int id);
}
