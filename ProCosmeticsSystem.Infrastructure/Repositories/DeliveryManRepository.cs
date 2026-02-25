using Dapper;
using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.DTOs.Deliveries;
using ProCosmeticsSystem.Application.Interfaces;
using ProCosmeticsSystem.Domain.Entities;
using ProCosmeticsSystem.Infrastructure.Persistence;

namespace ProCosmeticsSystem.Infrastructure.Repositories;

public class DeliveryManRepository : IDeliveryManRepository
{
    private readonly DbConnectionFactory _db;

    public DeliveryManRepository(DbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<PagedResult<DeliveryManDto>> GetAllAsync(int page, int pageSize, string? search)
    {
        using var conn = _db.CreateConnection();

        var where = "WHERE IsDeleted = 0";
        if (!string.IsNullOrWhiteSpace(search))
            where += " AND (Name LIKE @Search OR Email LIKE @Search OR Phone LIKE @Search)";

        var totalCount = await conn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM DeliveryMen {where}", new { Search = $"%{search}%" });

        var items = await conn.QueryAsync<DeliveryManDto>(
            $@"SELECT Id, Name, Phone, Email, IsAvailable, IsActive, CreatedAt FROM DeliveryMen {where}
               ORDER BY CreatedAt DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY",
            new { Search = $"%{search}%", Offset = (page - 1) * pageSize, PageSize = pageSize });

        return new PagedResult<DeliveryManDto> { Items = items.ToList(), TotalCount = totalCount, Page = page, PageSize = pageSize };
    }

    public async Task<DeliveryManDto?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<DeliveryManDto>(
            "SELECT Id, Name, Phone, Email, IsAvailable, IsActive, CreatedAt FROM DeliveryMen WHERE Id = @Id AND IsDeleted = 0",
            new { Id = id });
    }

    public async Task<int> CreateAsync(DeliveryMan deliveryMan)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            @"INSERT INTO DeliveryMen (Name, Phone, Email, IsAvailable, IsActive, IsDeleted, CreatedAt, CreatedBy)
              VALUES (@Name, @Phone, @Email, 1, 1, 0, @CreatedAt, @CreatedBy);
              SELECT CAST(SCOPE_IDENTITY() AS INT)",
            deliveryMan);
    }

    public async Task UpdateAsync(DeliveryMan deliveryMan)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            @"UPDATE DeliveryMen SET Name = @Name, Phone = @Phone, Email = @Email,
              IsAvailable = @IsAvailable, IsActive = @IsActive, UpdatedAt = @UpdatedAt, UpdatedBy = @UpdatedBy
              WHERE Id = @Id AND IsDeleted = 0",
            deliveryMan);
    }

    public async Task SoftDeleteAsync(int id)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync("UPDATE DeliveryMen SET IsDeleted = 1, UpdatedAt = @UpdatedAt WHERE Id = @Id",
            new { Id = id, UpdatedAt = DateTime.UtcNow });
    }
}
