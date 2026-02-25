using Dapper;
using ProCosmeticsSystem.Application.DTOs.Products;
using ProCosmeticsSystem.Application.Interfaces;
using ProCosmeticsSystem.Domain.Entities;
using ProCosmeticsSystem.Infrastructure.Persistence;

namespace ProCosmeticsSystem.Infrastructure.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly DbConnectionFactory _db;

    public CategoryRepository(DbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<List<CategoryDto>> GetAllAsync()
    {
        using var conn = _db.CreateConnection();
        var results = await conn.QueryAsync<CategoryDto>(
            @"SELECT c.Id, c.Name, c.Description, c.ParentCategoryId, p.Name AS ParentCategoryName
              FROM Categories c
              LEFT JOIN Categories p ON c.ParentCategoryId = p.Id
              WHERE c.IsDeleted = 0
              ORDER BY c.Name");
        return results.ToList();
    }

    public async Task<CategoryDto?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<CategoryDto>(
            @"SELECT c.Id, c.Name, c.Description, c.ParentCategoryId, p.Name AS ParentCategoryName
              FROM Categories c
              LEFT JOIN Categories p ON c.ParentCategoryId = p.Id
              WHERE c.Id = @Id AND c.IsDeleted = 0",
            new { Id = id });
    }

    public async Task<int> CreateAsync(Category category)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            @"INSERT INTO Categories (Name, Description, ParentCategoryId, IsDeleted, CreatedAt)
              VALUES (@Name, @Description, @ParentCategoryId, 0, @CreatedAt);
              SELECT CAST(SCOPE_IDENTITY() AS INT)",
            category);
    }

    public async Task UpdateAsync(Category category)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            @"UPDATE Categories SET Name = @Name, Description = @Description,
              ParentCategoryId = @ParentCategoryId, UpdatedAt = @UpdatedAt
              WHERE Id = @Id AND IsDeleted = 0",
            category);
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync("UPDATE Categories SET IsDeleted = 1, UpdatedAt = @UpdatedAt WHERE Id = @Id",
            new { Id = id, UpdatedAt = DateTime.UtcNow });
    }
}
