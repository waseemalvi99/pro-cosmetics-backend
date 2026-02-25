using Dapper;
using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.DTOs.Purchases;
using ProCosmeticsSystem.Application.Interfaces;
using ProCosmeticsSystem.Domain.Entities;
using ProCosmeticsSystem.Infrastructure.Persistence;

namespace ProCosmeticsSystem.Infrastructure.Repositories;

public class PurchaseOrderRepository : IPurchaseOrderRepository
{
    private readonly DbConnectionFactory _db;

    public PurchaseOrderRepository(DbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<PagedResult<PurchaseOrderDto>> GetAllAsync(int page, int pageSize, int? supplierId)
    {
        using var conn = _db.CreateConnection();

        var where = "WHERE 1=1";
        if (supplierId.HasValue)
            where += " AND po.SupplierId = @SupplierId";

        var totalCount = await conn.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*) FROM PurchaseOrders po {where}", new { SupplierId = supplierId });

        var sql = $@"SELECT po.Id, po.SupplierId, s.Name AS SupplierName, po.OrderNumber, po.OrderDate,
                     po.ExpectedDeliveryDate, po.Status, po.TotalAmount, po.Notes, po.CreatedAt
                     FROM PurchaseOrders po
                     INNER JOIN Suppliers s ON po.SupplierId = s.Id
                     {where}
                     ORDER BY po.CreatedAt DESC
                     OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var items = await conn.QueryAsync<PurchaseOrderDto>(sql, new
        {
            SupplierId = supplierId,
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        });

        return new PagedResult<PurchaseOrderDto> { Items = items.ToList(), TotalCount = totalCount, Page = page, PageSize = pageSize };
    }

    public async Task<PurchaseOrderDto?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<PurchaseOrderDto>(
            @"SELECT po.Id, po.SupplierId, s.Name AS SupplierName, po.OrderNumber, po.OrderDate,
              po.ExpectedDeliveryDate, po.Status, po.TotalAmount, po.Notes, po.CreatedAt
              FROM PurchaseOrders po
              INNER JOIN Suppliers s ON po.SupplierId = s.Id
              WHERE po.Id = @Id",
            new { Id = id });
    }

    public async Task<int> CreateAsync(PurchaseOrder order)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            @"INSERT INTO PurchaseOrders (SupplierId, OrderNumber, OrderDate, ExpectedDeliveryDate, Status, TotalAmount, Notes, CreatedAt, CreatedBy, IsDeleted)
              VALUES (@SupplierId, @OrderNumber, @OrderDate, @ExpectedDeliveryDate, @Status, @TotalAmount, @Notes, @CreatedAt, @CreatedBy, 0);
              SELECT CAST(SCOPE_IDENTITY() AS INT)",
            order);
    }

    public async Task AddItemsAsync(int orderId, IEnumerable<PurchaseOrderItem> items)
    {
        using var conn = _db.CreateConnection();
        foreach (var item in items)
        {
            await conn.ExecuteAsync(
                @"INSERT INTO PurchaseOrderItems (PurchaseOrderId, ProductId, Quantity, UnitPrice, TotalPrice)
                  VALUES (@PurchaseOrderId, @ProductId, @Quantity, @UnitPrice, @TotalPrice)",
                item);
        }
    }

    public async Task<List<PurchaseOrderItemDto>> GetItemsAsync(int orderId)
    {
        using var conn = _db.CreateConnection();
        var results = await conn.QueryAsync<PurchaseOrderItemDto>(
            @"SELECT poi.Id, poi.ProductId, p.Name AS ProductName, poi.Quantity, poi.UnitPrice, poi.TotalPrice
              FROM PurchaseOrderItems poi
              INNER JOIN Products p ON poi.ProductId = p.Id
              WHERE poi.PurchaseOrderId = @OrderId",
            new { OrderId = orderId });
        return results.ToList();
    }

    public async Task UpdateStatusAsync(int id, int status)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync("UPDATE PurchaseOrders SET Status = @Status, UpdatedAt = @UpdatedAt WHERE Id = @Id",
            new { Id = id, Status = status, UpdatedAt = DateTime.UtcNow });
    }

    public async Task UpdateTotalAsync(int id, decimal total)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync("UPDATE PurchaseOrders SET TotalAmount = @Total, UpdatedAt = @UpdatedAt WHERE Id = @Id",
            new { Id = id, Total = total, UpdatedAt = DateTime.UtcNow });
    }
}
