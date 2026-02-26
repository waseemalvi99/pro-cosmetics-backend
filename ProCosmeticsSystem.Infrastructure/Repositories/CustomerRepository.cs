using Dapper;
using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.DTOs.Customers;
using ProCosmeticsSystem.Application.Interfaces;
using ProCosmeticsSystem.Domain.Entities;
using ProCosmeticsSystem.Infrastructure.Persistence;

namespace ProCosmeticsSystem.Infrastructure.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly DbConnectionFactory _db;

    public CustomerRepository(DbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<PagedResult<CustomerDto>> GetAllAsync(int page, int pageSize, string? search)
    {
        using var conn = _db.CreateConnection();

        var where = "WHERE IsDeleted = 0";
        if (!string.IsNullOrWhiteSpace(search))
            where += " AND (FullName LIKE @Search OR Email LIKE @Search OR Phone LIKE @Search OR City LIKE @Search)";

        var countSql = $"SELECT COUNT(*) FROM Customers {where}";
        var totalCount = await conn.ExecuteScalarAsync<int>(countSql, new { Search = $"%{search}%" });

        var sql = $@"SELECT Id, FullName, Email, Phone, Address, City, Notes, IsActive, CreditDays, CreditLimit, CreatedAt
                     FROM Customers {where}
                     ORDER BY CreatedAt DESC
                     OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var items = await conn.QueryAsync<CustomerDto>(sql, new
        {
            Search = $"%{search}%",
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        });

        return new PagedResult<CustomerDto>
        {
            Items = items.ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<CustomerDto?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<CustomerDto>(
            "SELECT Id, FullName, Email, Phone, Address, City, Notes, IsActive, CreditDays, CreditLimit, CreatedAt FROM Customers WHERE Id = @Id AND IsDeleted = 0",
            new { Id = id });
    }

    public async Task<int> CreateAsync(Customer customer)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            @"INSERT INTO Customers (FullName, Email, Phone, Address, City, Notes, IsActive, CreditDays, CreditLimit, IsDeleted, CreatedAt, CreatedBy)
              VALUES (@FullName, @Email, @Phone, @Address, @City, @Notes, 1, @CreditDays, @CreditLimit, 0, @CreatedAt, @CreatedBy);
              SELECT CAST(SCOPE_IDENTITY() AS INT)",
            customer);
    }

    public async Task UpdateAsync(Customer customer)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            @"UPDATE Customers SET FullName = @FullName, Email = @Email, Phone = @Phone,
              Address = @Address, City = @City, Notes = @Notes, IsActive = @IsActive,
              CreditDays = @CreditDays, CreditLimit = @CreditLimit,
              UpdatedAt = @UpdatedAt, UpdatedBy = @UpdatedBy
              WHERE Id = @Id AND IsDeleted = 0",
            customer);
    }

    public async Task SoftDeleteAsync(int id)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE Customers SET IsDeleted = 1, UpdatedAt = @UpdatedAt WHERE Id = @Id",
            new { Id = id, UpdatedAt = DateTime.UtcNow });
    }
}
