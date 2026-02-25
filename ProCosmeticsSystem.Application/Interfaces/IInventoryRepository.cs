using ProCosmeticsSystem.Application.DTOs.Products;
using ProCosmeticsSystem.Domain.Entities;

namespace ProCosmeticsSystem.Application.Interfaces;

public interface IInventoryRepository
{
    Task<List<InventoryDto>> GetAllAsync();
    Task<InventoryDto?> GetByProductIdAsync(int productId);
    Task<List<InventoryDto>> GetLowStockAsync();
    Task CreateAsync(Inventory inventory);
    Task UpdateQuantityAsync(int productId, int quantityChange);
    Task AddTransactionAsync(InventoryTransaction transaction);
}
