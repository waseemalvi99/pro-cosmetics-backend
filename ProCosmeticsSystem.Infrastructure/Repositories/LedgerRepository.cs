using Dapper;
using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.DTOs.Ledger;
using ProCosmeticsSystem.Application.Interfaces;
using ProCosmeticsSystem.Domain.Entities;
using ProCosmeticsSystem.Infrastructure.Persistence;

namespace ProCosmeticsSystem.Infrastructure.Repositories;

public class LedgerRepository : ILedgerRepository
{
    private readonly DbConnectionFactory _db;

    public LedgerRepository(DbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<PagedResult<LedgerEntryDto>> GetAllAsync(int page, int pageSize, int? customerId, int? supplierId)
    {
        using var conn = _db.CreateConnection();

        var where = "WHERE le.IsDeleted = 0";
        if (customerId.HasValue) where += " AND le.CustomerId = @CustomerId";
        if (supplierId.HasValue) where += " AND le.SupplierId = @SupplierId";

        var totalCount = await conn.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*) FROM LedgerEntries le {where}",
            new { CustomerId = customerId, SupplierId = supplierId });

        var sql = $@"SELECT le.Id, le.EntryDate,
                     CASE le.AccountType WHEN 0 THEN 'CustomerReceivable' WHEN 1 THEN 'SupplierPayable' END AS AccountType,
                     le.CustomerId, c.FullName AS CustomerName,
                     le.SupplierId, s.Name AS SupplierName,
                     le.ReferenceType, le.ReferenceId, le.Description,
                     le.DebitAmount, le.CreditAmount, le.IsReversed, le.CreatedAt
                     FROM LedgerEntries le
                     LEFT JOIN Customers c ON le.CustomerId = c.Id
                     LEFT JOIN Suppliers s ON le.SupplierId = s.Id
                     {where}
                     ORDER BY le.EntryDate DESC, le.Id DESC
                     OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var items = await conn.QueryAsync<LedgerEntryDto>(sql, new
        {
            CustomerId = customerId,
            SupplierId = supplierId,
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        });

        return new PagedResult<LedgerEntryDto> { Items = items.ToList(), TotalCount = totalCount, Page = page, PageSize = pageSize };
    }

    public async Task<LedgerEntryDto?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<LedgerEntryDto>(
            @"SELECT le.Id, le.EntryDate,
              CASE le.AccountType WHEN 0 THEN 'CustomerReceivable' WHEN 1 THEN 'SupplierPayable' END AS AccountType,
              le.CustomerId, c.FullName AS CustomerName,
              le.SupplierId, s.Name AS SupplierName,
              le.ReferenceType, le.ReferenceId, le.Description,
              le.DebitAmount, le.CreditAmount, le.IsReversed, le.CreatedAt
              FROM LedgerEntries le
              LEFT JOIN Customers c ON le.CustomerId = c.Id
              LEFT JOIN Suppliers s ON le.SupplierId = s.Id
              WHERE le.Id = @Id AND le.IsDeleted = 0",
            new { Id = id });
    }

    public async Task<int> CreateAsync(LedgerEntry entry)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            @"INSERT INTO LedgerEntries (EntryDate, AccountType, CustomerId, SupplierId, ReferenceType, ReferenceId,
              Description, DebitAmount, CreditAmount, IsReversed, ReversedByEntryId, IsDeleted, CreatedAt, CreatedBy)
              VALUES (@EntryDate, @AccountType, @CustomerId, @SupplierId, @ReferenceType, @ReferenceId,
              @Description, @DebitAmount, @CreditAmount, 0, NULL, 0, @CreatedAt, @CreatedBy);
              SELECT CAST(SCOPE_IDENTITY() AS INT)",
            entry);
    }

    public async Task<decimal> GetBalanceAsync(int? customerId, int? supplierId)
    {
        using var conn = _db.CreateConnection();

        var where = "WHERE IsDeleted = 0 AND IsReversed = 0";
        if (customerId.HasValue) where += " AND CustomerId = @CustomerId";
        if (supplierId.HasValue) where += " AND SupplierId = @SupplierId";

        var balance = await conn.ExecuteScalarAsync<decimal>(
            $"SELECT ISNULL(SUM(DebitAmount) - SUM(CreditAmount), 0) FROM LedgerEntries {where}",
            new { CustomerId = customerId, SupplierId = supplierId });

        return balance;
    }

    public async Task<List<LedgerEntryDto>> GetByReferenceAsync(string referenceType, int referenceId)
    {
        using var conn = _db.CreateConnection();
        var results = await conn.QueryAsync<LedgerEntryDto>(
            @"SELECT Id, EntryDate,
              CASE AccountType WHEN 0 THEN 'CustomerReceivable' WHEN 1 THEN 'SupplierPayable' END AS AccountType,
              CustomerId, SupplierId, ReferenceType, ReferenceId, Description,
              DebitAmount, CreditAmount, IsReversed, CreatedAt
              FROM LedgerEntries
              WHERE ReferenceType = @ReferenceType AND ReferenceId = @ReferenceId AND IsDeleted = 0",
            new { ReferenceType = referenceType, ReferenceId = referenceId });
        return results.ToList();
    }

    public async Task MarkReversedAsync(int entryId, int reversedByEntryId)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE LedgerEntries SET IsReversed = 1, ReversedByEntryId = @ReversedByEntryId, UpdatedAt = @UpdatedAt WHERE Id = @Id",
            new { Id = entryId, ReversedByEntryId = reversedByEntryId, UpdatedAt = DateTime.UtcNow });
    }
}
