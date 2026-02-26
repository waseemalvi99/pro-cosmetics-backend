using Dapper;
using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.DTOs.CreditDebitNotes;
using ProCosmeticsSystem.Application.Interfaces;
using ProCosmeticsSystem.Domain.Entities;
using ProCosmeticsSystem.Infrastructure.Persistence;

namespace ProCosmeticsSystem.Infrastructure.Repositories;

public class CreditDebitNoteRepository : ICreditDebitNoteRepository
{
    private readonly DbConnectionFactory _db;

    public CreditDebitNoteRepository(DbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<PagedResult<CreditDebitNoteDto>> GetAllAsync(int page, int pageSize, int? customerId, int? supplierId)
    {
        using var conn = _db.CreateConnection();

        var where = "WHERE n.IsDeleted = 0";
        if (customerId.HasValue) where += " AND n.CustomerId = @CustomerId";
        if (supplierId.HasValue) where += " AND n.SupplierId = @SupplierId";

        var totalCount = await conn.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*) FROM CreditDebitNotes n {where}",
            new { CustomerId = customerId, SupplierId = supplierId });

        var sql = $@"SELECT n.Id, n.NoteNumber,
                     CASE n.NoteType WHEN 0 THEN 'CreditNote' WHEN 1 THEN 'DebitNote' END AS NoteType,
                     CASE n.AccountType WHEN 0 THEN 'Customer' WHEN 1 THEN 'Supplier' END AS AccountType,
                     n.CustomerId, c.FullName AS CustomerName,
                     n.SupplierId, s.Name AS SupplierName,
                     n.NoteDate, n.Amount, n.Reason,
                     n.SaleId, sl.SaleNumber,
                     n.PurchaseOrderId, po.OrderNumber AS PurchaseOrderNumber,
                     n.CreatedAt
                     FROM CreditDebitNotes n
                     LEFT JOIN Customers c ON n.CustomerId = c.Id
                     LEFT JOIN Suppliers s ON n.SupplierId = s.Id
                     LEFT JOIN Sales sl ON n.SaleId = sl.Id
                     LEFT JOIN PurchaseOrders po ON n.PurchaseOrderId = po.Id
                     {where}
                     ORDER BY n.NoteDate DESC, n.Id DESC
                     OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var items = await conn.QueryAsync<CreditDebitNoteDto>(sql, new
        {
            CustomerId = customerId,
            SupplierId = supplierId,
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        });

        return new PagedResult<CreditDebitNoteDto> { Items = items.ToList(), TotalCount = totalCount, Page = page, PageSize = pageSize };
    }

    public async Task<CreditDebitNoteDto?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<CreditDebitNoteDto>(
            @"SELECT n.Id, n.NoteNumber,
              CASE n.NoteType WHEN 0 THEN 'CreditNote' WHEN 1 THEN 'DebitNote' END AS NoteType,
              CASE n.AccountType WHEN 0 THEN 'Customer' WHEN 1 THEN 'Supplier' END AS AccountType,
              n.CustomerId, c.FullName AS CustomerName,
              n.SupplierId, s.Name AS SupplierName,
              n.NoteDate, n.Amount, n.Reason,
              n.SaleId, sl.SaleNumber,
              n.PurchaseOrderId, po.OrderNumber AS PurchaseOrderNumber,
              n.CreatedAt
              FROM CreditDebitNotes n
              LEFT JOIN Customers c ON n.CustomerId = c.Id
              LEFT JOIN Suppliers s ON n.SupplierId = s.Id
              LEFT JOIN Sales sl ON n.SaleId = sl.Id
              LEFT JOIN PurchaseOrders po ON n.PurchaseOrderId = po.Id
              WHERE n.Id = @Id AND n.IsDeleted = 0",
            new { Id = id });
    }

    public async Task<int> CreateAsync(CreditDebitNote note)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            @"INSERT INTO CreditDebitNotes (NoteNumber, NoteType, AccountType, CustomerId, SupplierId,
              NoteDate, Amount, Reason, SaleId, PurchaseOrderId, IsDeleted, CreatedAt, CreatedBy)
              VALUES (@NoteNumber, @NoteType, @AccountType, @CustomerId, @SupplierId,
              @NoteDate, @Amount, @Reason, @SaleId, @PurchaseOrderId, 0, @CreatedAt, @CreatedBy);
              SELECT CAST(SCOPE_IDENTITY() AS INT)",
            note);
    }

    public async Task SoftDeleteAsync(int id)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE CreditDebitNotes SET IsDeleted = 1, UpdatedAt = @UpdatedAt WHERE Id = @Id",
            new { Id = id, UpdatedAt = DateTime.UtcNow });
    }
}
