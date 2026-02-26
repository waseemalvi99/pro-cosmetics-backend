using Dapper;
using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.DTOs.Payments;
using ProCosmeticsSystem.Application.Interfaces;
using ProCosmeticsSystem.Domain.Entities;
using ProCosmeticsSystem.Infrastructure.Persistence;

namespace ProCosmeticsSystem.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly DbConnectionFactory _db;

    public PaymentRepository(DbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<PagedResult<PaymentDto>> GetAllAsync(int page, int pageSize, int? customerId, int? supplierId)
    {
        using var conn = _db.CreateConnection();

        var where = "WHERE p.IsDeleted = 0";
        if (customerId.HasValue) where += " AND p.CustomerId = @CustomerId";
        if (supplierId.HasValue) where += " AND p.SupplierId = @SupplierId";

        var totalCount = await conn.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*) FROM Payments p {where}",
            new { CustomerId = customerId, SupplierId = supplierId });

        var sql = $@"SELECT p.Id, p.ReceiptNumber,
                     CASE p.PaymentType WHEN 0 THEN 'CustomerReceipt' WHEN 1 THEN 'SupplierPayment' END AS PaymentType,
                     p.CustomerId, c.FullName AS CustomerName,
                     p.SupplierId, s.Name AS SupplierName,
                     p.PaymentDate, p.Amount,
                     CASE p.PaymentMethod WHEN 0 THEN 'Cash' WHEN 1 THEN 'Cheque' WHEN 2 THEN 'BankTransfer' END AS PaymentMethod,
                     p.ChequeNumber, p.BankName, p.ChequeDate, p.BankAccountReference, p.Notes, p.CreatedAt
                     FROM Payments p
                     LEFT JOIN Customers c ON p.CustomerId = c.Id
                     LEFT JOIN Suppliers s ON p.SupplierId = s.Id
                     {where}
                     ORDER BY p.PaymentDate DESC, p.Id DESC
                     OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var items = await conn.QueryAsync<PaymentDto>(sql, new
        {
            CustomerId = customerId,
            SupplierId = supplierId,
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        });

        return new PagedResult<PaymentDto> { Items = items.ToList(), TotalCount = totalCount, Page = page, PageSize = pageSize };
    }

    public async Task<PaymentDto?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<PaymentDto>(
            @"SELECT p.Id, p.ReceiptNumber,
              CASE p.PaymentType WHEN 0 THEN 'CustomerReceipt' WHEN 1 THEN 'SupplierPayment' END AS PaymentType,
              p.CustomerId, c.FullName AS CustomerName,
              p.SupplierId, s.Name AS SupplierName,
              p.PaymentDate, p.Amount,
              CASE p.PaymentMethod WHEN 0 THEN 'Cash' WHEN 1 THEN 'Cheque' WHEN 2 THEN 'BankTransfer' END AS PaymentMethod,
              p.ChequeNumber, p.BankName, p.ChequeDate, p.BankAccountReference, p.Notes, p.CreatedAt
              FROM Payments p
              LEFT JOIN Customers c ON p.CustomerId = c.Id
              LEFT JOIN Suppliers s ON p.SupplierId = s.Id
              WHERE p.Id = @Id AND p.IsDeleted = 0",
            new { Id = id });
    }

    public async Task<int> CreateAsync(Payment payment)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            @"INSERT INTO Payments (ReceiptNumber, PaymentType, CustomerId, SupplierId, PaymentDate, Amount,
              PaymentMethod, ChequeNumber, BankName, ChequeDate, BankAccountReference, Notes, IsDeleted, CreatedAt, CreatedBy)
              VALUES (@ReceiptNumber, @PaymentType, @CustomerId, @SupplierId, @PaymentDate, @Amount,
              @PaymentMethod, @ChequeNumber, @BankName, @ChequeDate, @BankAccountReference, @Notes, 0, @CreatedAt, @CreatedBy);
              SELECT CAST(SCOPE_IDENTITY() AS INT)",
            payment);
    }

    public async Task SoftDeleteAsync(int id)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE Payments SET IsDeleted = 1, UpdatedAt = @UpdatedAt WHERE Id = @Id",
            new { Id = id, UpdatedAt = DateTime.UtcNow });
    }
}
