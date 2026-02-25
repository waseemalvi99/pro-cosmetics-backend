using Dapper;
using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.DTOs.Suppliers;
using ProCosmeticsSystem.Application.Interfaces;
using ProCosmeticsSystem.Domain.Entities;
using ProCosmeticsSystem.Infrastructure.Persistence;

namespace ProCosmeticsSystem.Infrastructure.Repositories;

public class SupplierRepository : ISupplierRepository
{
    private readonly DbConnectionFactory _db;

    public SupplierRepository(DbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<PagedResult<SupplierDto>> GetAllAsync(int page, int pageSize, string? search)
    {
        using var conn = _db.CreateConnection();

        var where = "WHERE IsDeleted = 0";
        if (!string.IsNullOrWhiteSpace(search))
            where += " AND (Name LIKE @Search OR ContactPerson LIKE @Search OR Email LIKE @Search)";

        var totalCount = await conn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM Suppliers {where}", new { Search = $"%{search}%" });

        var sql = $@"SELECT Id, Name, ContactPerson, Email, Phone, Address, Notes, IsActive, CreatedAt
                     FROM Suppliers {where}
                     ORDER BY CreatedAt DESC
                     OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var items = await conn.QueryAsync<SupplierDto>(sql, new
        {
            Search = $"%{search}%",
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        });

        return new PagedResult<SupplierDto> { Items = items.ToList(), TotalCount = totalCount, Page = page, PageSize = pageSize };
    }

    public async Task<SupplierDto?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<SupplierDto>(
            "SELECT Id, Name, ContactPerson, Email, Phone, Address, Notes, IsActive, CreatedAt FROM Suppliers WHERE Id = @Id AND IsDeleted = 0",
            new { Id = id });
    }

    public async Task<int> CreateAsync(Supplier supplier)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            @"INSERT INTO Suppliers (Name, ContactPerson, Email, Phone, Address, Notes, IsActive, IsDeleted, CreatedAt, CreatedBy)
              VALUES (@Name, @ContactPerson, @Email, @Phone, @Address, @Notes, 1, 0, @CreatedAt, @CreatedBy);
              SELECT CAST(SCOPE_IDENTITY() AS INT)",
            supplier);
    }

    public async Task UpdateAsync(Supplier supplier)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            @"UPDATE Suppliers SET Name = @Name, ContactPerson = @ContactPerson, Email = @Email,
              Phone = @Phone, Address = @Address, Notes = @Notes, IsActive = @IsActive,
              UpdatedAt = @UpdatedAt, UpdatedBy = @UpdatedBy
              WHERE Id = @Id AND IsDeleted = 0",
            supplier);
    }

    public async Task SoftDeleteAsync(int id)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync("UPDATE Suppliers SET IsDeleted = 1, UpdatedAt = @UpdatedAt WHERE Id = @Id",
            new { Id = id, UpdatedAt = DateTime.UtcNow });
    }
}
