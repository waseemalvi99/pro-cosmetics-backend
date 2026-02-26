using Dapper;
using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.DTOs.Sales;
using ProCosmeticsSystem.Application.Interfaces;
using ProCosmeticsSystem.Domain.Entities;
using ProCosmeticsSystem.Infrastructure.Persistence;

namespace ProCosmeticsSystem.Infrastructure.Repositories;

public class SaleRepository : ISaleRepository
{
    private readonly DbConnectionFactory _db;

    public SaleRepository(DbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<PagedResult<SaleDto>> GetAllAsync(int page, int pageSize, int? customerId, int? salesmanId)
    {
        using var conn = _db.CreateConnection();

        var where = "WHERE 1=1";
        if (customerId.HasValue) where += " AND s.CustomerId = @CustomerId";
        if (salesmanId.HasValue) where += " AND s.SalesmanId = @SalesmanId";

        var totalCount = await conn.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*) FROM Sales s {where}", new { CustomerId = customerId, SalesmanId = salesmanId });

        var sql = $@"SELECT s.Id, s.SaleNumber, s.CustomerId, c.FullName AS CustomerName,
                     s.SalesmanId, sm.Name AS SalesmanName, s.SaleDate, s.SubTotal, s.Discount,
                     s.Tax, s.TotalAmount, s.PaymentMethod,
                     CASE s.Status
                         WHEN 0 THEN 'Completed'
                         WHEN 1 THEN 'Pending'
                         WHEN 2 THEN 'Cancelled'
                         WHEN 3 THEN 'Refunded'
                     END AS Status,
                     s.Notes, s.DueDate, s.CreatedAt
                     FROM Sales s
                     LEFT JOIN Customers c ON s.CustomerId = c.Id
                     LEFT JOIN Salesmen sm ON s.SalesmanId = sm.Id
                     {where}
                     ORDER BY s.CreatedAt DESC
                     OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var items = await conn.QueryAsync<SaleDto>(sql, new
        {
            CustomerId = customerId,
            SalesmanId = salesmanId,
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        });

        return new PagedResult<SaleDto> { Items = items.ToList(), TotalCount = totalCount, Page = page, PageSize = pageSize };
    }

    public async Task<SaleDto?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<SaleDto>(
            @"SELECT s.Id, s.SaleNumber, s.CustomerId, c.FullName AS CustomerName,
              s.SalesmanId, sm.Name AS SalesmanName, s.SaleDate, s.SubTotal, s.Discount,
              s.Tax, s.TotalAmount, s.PaymentMethod,
              CASE s.Status
                  WHEN 0 THEN 'Completed'
                  WHEN 1 THEN 'Pending'
                  WHEN 2 THEN 'Cancelled'
                  WHEN 3 THEN 'Refunded'
              END AS Status,
              s.Notes, s.DueDate, s.CreatedAt
              FROM Sales s
              LEFT JOIN Customers c ON s.CustomerId = c.Id
              LEFT JOIN Salesmen sm ON s.SalesmanId = sm.Id
              WHERE s.Id = @Id",
            new { Id = id });
    }

    public async Task<int> CreateAsync(Sale sale)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            @"INSERT INTO Sales (SaleNumber, CustomerId, SalesmanId, SaleDate, SubTotal, Discount, Tax, TotalAmount, PaymentMethod, Status, Notes, DueDate, CreatedAt, CreatedBy, IsDeleted)
              VALUES (@SaleNumber, @CustomerId, @SalesmanId, @SaleDate, @SubTotal, @Discount, @Tax, @TotalAmount, @PaymentMethod, @Status, @Notes, @DueDate, @CreatedAt, @CreatedBy, 0);
              SELECT CAST(SCOPE_IDENTITY() AS INT)",
            sale);
    }

    public async Task AddItemsAsync(int saleId, IEnumerable<SaleItem> items)
    {
        using var conn = _db.CreateConnection();
        foreach (var item in items)
        {
            await conn.ExecuteAsync(
                @"INSERT INTO SaleItems (SaleId, ProductId, Quantity, UnitPrice, Discount, TotalPrice)
                  VALUES (@SaleId, @ProductId, @Quantity, @UnitPrice, @Discount, @TotalPrice)",
                item);
        }
    }

    public async Task<List<SaleItemDto>> GetItemsAsync(int saleId)
    {
        using var conn = _db.CreateConnection();
        var results = await conn.QueryAsync<SaleItemDto>(
            @"SELECT si.Id, si.ProductId, p.Name AS ProductName, si.Quantity, si.UnitPrice, si.Discount, si.TotalPrice
              FROM SaleItems si
              INNER JOIN Products p ON si.ProductId = p.Id
              WHERE si.SaleId = @SaleId",
            new { SaleId = saleId });
        return results.ToList();
    }

    public async Task UpdateStatusAsync(int id, int status)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync("UPDATE Sales SET Status = @Status, UpdatedAt = @UpdatedAt WHERE Id = @Id",
            new { Id = id, Status = status, UpdatedAt = DateTime.UtcNow });
    }

    public async Task UpdateDueDateAsync(int id, DateTime dueDate)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync("UPDATE Sales SET DueDate = @DueDate, UpdatedAt = @UpdatedAt WHERE Id = @Id",
            new { Id = id, DueDate = dueDate, UpdatedAt = DateTime.UtcNow });
    }
}
