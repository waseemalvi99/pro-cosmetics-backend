using Dapper;
using ProCosmeticsSystem.Application.DTOs.Products;
using ProCosmeticsSystem.Application.Interfaces;
using ProCosmeticsSystem.Domain.Entities;
using ProCosmeticsSystem.Infrastructure.Persistence;

namespace ProCosmeticsSystem.Infrastructure.Repositories;

public class InventoryRepository : IInventoryRepository
{
    private readonly DbConnectionFactory _db;

    public InventoryRepository(DbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<List<InventoryDto>> GetAllAsync()
    {
        using var conn = _db.CreateConnection();
        var results = await conn.QueryAsync<InventoryDto>(
            @"SELECT i.ProductId, p.Name AS ProductName, p.SKU, i.QuantityOnHand, i.QuantityReserved,
              p.ReorderLevel, i.LastRestockedAt
              FROM Inventory i
              INNER JOIN Products p ON i.ProductId = p.Id
              WHERE p.IsDeleted = 0
              ORDER BY p.Name");
        return results.ToList();
    }

    public async Task<InventoryDto?> GetByProductIdAsync(int productId)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<InventoryDto>(
            @"SELECT i.ProductId, p.Name AS ProductName, p.SKU, i.QuantityOnHand, i.QuantityReserved,
              p.ReorderLevel, i.LastRestockedAt
              FROM Inventory i
              INNER JOIN Products p ON i.ProductId = p.Id
              WHERE i.ProductId = @ProductId AND p.IsDeleted = 0",
            new { ProductId = productId });
    }

    public async Task<List<InventoryDto>> GetLowStockAsync()
    {
        using var conn = _db.CreateConnection();
        var results = await conn.QueryAsync<InventoryDto>(
            @"SELECT i.ProductId, p.Name AS ProductName, p.SKU, i.QuantityOnHand, i.QuantityReserved,
              p.ReorderLevel, i.LastRestockedAt
              FROM Inventory i
              INNER JOIN Products p ON i.ProductId = p.Id
              WHERE p.IsDeleted = 0 AND i.QuantityOnHand <= p.ReorderLevel
              ORDER BY i.QuantityOnHand ASC");
        return results.ToList();
    }

    public async Task CreateAsync(Inventory inventory)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            @"INSERT INTO Inventory (ProductId, QuantityOnHand, QuantityReserved, LastRestockedAt)
              VALUES (@ProductId, @QuantityOnHand, @QuantityReserved, @LastRestockedAt)",
            inventory);
    }

    public async Task UpdateQuantityAsync(int productId, int quantityChange)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            @"UPDATE Inventory SET QuantityOnHand = QuantityOnHand + @QuantityChange,
              LastRestockedAt = CASE WHEN @QuantityChange > 0 THEN GETUTCDATE() ELSE LastRestockedAt END
              WHERE ProductId = @ProductId",
            new { ProductId = productId, QuantityChange = quantityChange });
    }

    public async Task AddTransactionAsync(InventoryTransaction transaction)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            @"INSERT INTO InventoryTransactions (ProductId, TransactionType, Quantity, ReferenceType, ReferenceId, Notes, CreatedAt)
              VALUES (@ProductId, @TransactionType, @Quantity, @ReferenceType, @ReferenceId, @Notes, @CreatedAt)",
            transaction);
    }
}
