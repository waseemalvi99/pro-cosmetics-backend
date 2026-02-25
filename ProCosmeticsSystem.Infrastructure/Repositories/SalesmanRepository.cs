using Dapper;
using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.DTOs.Sales;
using ProCosmeticsSystem.Application.Interfaces;
using ProCosmeticsSystem.Domain.Entities;
using ProCosmeticsSystem.Infrastructure.Persistence;

namespace ProCosmeticsSystem.Infrastructure.Repositories;

public class SalesmanRepository : ISalesmanRepository
{
    private readonly DbConnectionFactory _db;

    public SalesmanRepository(DbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<PagedResult<SalesmanDto>> GetAllAsync(int page, int pageSize, string? search)
    {
        using var conn = _db.CreateConnection();

        var where = "WHERE IsDeleted = 0";
        if (!string.IsNullOrWhiteSpace(search))
            where += " AND (Name LIKE @Search OR Email LIKE @Search OR Phone LIKE @Search)";

        var totalCount = await conn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM Salesmen {where}", new { Search = $"%{search}%" });

        var items = await conn.QueryAsync<SalesmanDto>(
            $@"SELECT Id, Name, Phone, Email, CommissionRate, IsActive, CreatedAt FROM Salesmen {where}
               ORDER BY CreatedAt DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY",
            new { Search = $"%{search}%", Offset = (page - 1) * pageSize, PageSize = pageSize });

        return new PagedResult<SalesmanDto> { Items = items.ToList(), TotalCount = totalCount, Page = page, PageSize = pageSize };
    }

    public async Task<SalesmanDto?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<SalesmanDto>(
            "SELECT Id, Name, Phone, Email, CommissionRate, IsActive, CreatedAt FROM Salesmen WHERE Id = @Id AND IsDeleted = 0",
            new { Id = id });
    }

    public async Task<int> CreateAsync(Salesman salesman)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            @"INSERT INTO Salesmen (Name, Phone, Email, CommissionRate, IsActive, IsDeleted, CreatedAt, CreatedBy)
              VALUES (@Name, @Phone, @Email, @CommissionRate, 1, 0, @CreatedAt, @CreatedBy);
              SELECT CAST(SCOPE_IDENTITY() AS INT)",
            salesman);
    }

    public async Task UpdateAsync(Salesman salesman)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            @"UPDATE Salesmen SET Name = @Name, Phone = @Phone, Email = @Email,
              CommissionRate = @CommissionRate, IsActive = @IsActive, UpdatedAt = @UpdatedAt, UpdatedBy = @UpdatedBy
              WHERE Id = @Id AND IsDeleted = 0",
            salesman);
    }

    public async Task SoftDeleteAsync(int id)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync("UPDATE Salesmen SET IsDeleted = 1, UpdatedAt = @UpdatedAt WHERE Id = @Id",
            new { Id = id, UpdatedAt = DateTime.UtcNow });
    }
}
