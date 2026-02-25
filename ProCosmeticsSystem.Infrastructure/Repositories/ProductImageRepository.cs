using Dapper;
using ProCosmeticsSystem.Application.Interfaces;
using ProCosmeticsSystem.Domain.Entities;
using ProCosmeticsSystem.Infrastructure.Persistence;

namespace ProCosmeticsSystem.Infrastructure.Repositories;

public class ProductImageRepository : IProductImageRepository
{
    private readonly DbConnectionFactory _db;

    public ProductImageRepository(DbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<List<ProductImage>> GetByProductIdAsync(int productId)
    {
        using var conn = _db.CreateConnection();
        var results = await conn.QueryAsync<ProductImage>(
            "SELECT Id, ProductId, FileName, FilePath, IsPrimary, SortOrder, CreatedAt FROM ProductImages WHERE ProductId = @ProductId ORDER BY SortOrder",
            new { ProductId = productId });
        return results.ToList();
    }

    public async Task<ProductImage?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<ProductImage>(
            "SELECT Id, ProductId, FileName, FilePath, IsPrimary, SortOrder, CreatedAt FROM ProductImages WHERE Id = @Id",
            new { Id = id });
    }

    public async Task<int> CreateAsync(ProductImage image)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            @"INSERT INTO ProductImages (ProductId, FileName, FilePath, IsPrimary, SortOrder, CreatedAt)
              VALUES (@ProductId, @FileName, @FilePath, @IsPrimary, @SortOrder, @CreatedAt);
              SELECT CAST(SCOPE_IDENTITY() AS INT)",
            image);
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM ProductImages WHERE Id = @Id", new { Id = id });
    }

    public async Task SetPrimaryAsync(int productId, int imageId)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync("UPDATE ProductImages SET IsPrimary = 0 WHERE ProductId = @ProductId", new { ProductId = productId });
        await conn.ExecuteAsync("UPDATE ProductImages SET IsPrimary = 1 WHERE Id = @Id", new { Id = imageId });
    }

    public async Task<int> GetMaxSortOrderAsync(int productId)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "SELECT ISNULL(MAX(SortOrder), 0) FROM ProductImages WHERE ProductId = @ProductId",
            new { ProductId = productId });
    }
}
