using ProCosmeticsSystem.Domain.Entities;

namespace ProCosmeticsSystem.Application.Interfaces;

public interface IProductImageRepository
{
    Task<List<ProductImage>> GetByProductIdAsync(int productId);
    Task<ProductImage?> GetByIdAsync(int id);
    Task<int> CreateAsync(ProductImage image);
    Task DeleteAsync(int id);
    Task SetPrimaryAsync(int productId, int imageId);
    Task<int> GetMaxSortOrderAsync(int productId);
}
