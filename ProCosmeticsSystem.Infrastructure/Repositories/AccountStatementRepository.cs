using Dapper;
using ProCosmeticsSystem.Application.DTOs.Accounts;
using ProCosmeticsSystem.Application.Interfaces;
using ProCosmeticsSystem.Infrastructure.Persistence;

namespace ProCosmeticsSystem.Infrastructure.Repositories;

public class AccountStatementRepository : IAccountStatementRepository
{
    private readonly DbConnectionFactory _db;

    public AccountStatementRepository(DbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<AccountStatementDto> GetCustomerStatementAsync(int customerId, DateTime fromDate, DateTime toDate)
    {
        using var conn = _db.CreateConnection();

        var customer = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT Id, FullName FROM Customers WHERE Id = @Id AND IsDeleted = 0",
            new { Id = customerId });

        // Opening balance: sum of all entries before fromDate
        var openingBalance = await conn.ExecuteScalarAsync<decimal>(
            @"SELECT ISNULL(SUM(DebitAmount) - SUM(CreditAmount), 0)
              FROM LedgerEntries
              WHERE CustomerId = @CustomerId AND AccountType = 0
              AND IsDeleted = 0 AND EntryDate < @FromDate",
            new { CustomerId = customerId, FromDate = fromDate });

        // Statement lines within the period
        var lines = await conn.QueryAsync<AccountStatementLineDto>(
            @"SELECT Id, EntryDate, ReferenceType, ReferenceId, Description,
              DebitAmount, CreditAmount, 0 AS RunningBalance
              FROM LedgerEntries
              WHERE CustomerId = @CustomerId AND AccountType = 0
              AND IsDeleted = 0 AND EntryDate >= @FromDate AND EntryDate <= @ToDate
              ORDER BY EntryDate ASC, Id ASC",
            new { CustomerId = customerId, FromDate = fromDate, ToDate = toDate });

        var linesList = lines.ToList();

        // Calculate running balance
        var running = openingBalance;
        foreach (var line in linesList)
        {
            running += line.DebitAmount - line.CreditAmount;
            line.RunningBalance = running;
        }

        var totalDebits = linesList.Sum(l => l.DebitAmount);
        var totalCredits = linesList.Sum(l => l.CreditAmount);

        return new AccountStatementDto
        {
            AccountId = customerId,
            AccountName = customer?.FullName ?? "Unknown",
            AccountType = "Customer",
            FromDate = fromDate,
            ToDate = toDate,
            OpeningBalance = openingBalance,
            TotalDebits = totalDebits,
            TotalCredits = totalCredits,
            ClosingBalance = openingBalance + totalDebits - totalCredits,
            Lines = linesList
        };
    }

    public async Task<AccountStatementDto> GetSupplierStatementAsync(int supplierId, DateTime fromDate, DateTime toDate)
    {
        using var conn = _db.CreateConnection();

        var supplier = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT Id, Name FROM Suppliers WHERE Id = @Id AND IsDeleted = 0",
            new { Id = supplierId });

        // Opening balance: sum of all entries before fromDate (credits - debits for payables)
        var openingBalance = await conn.ExecuteScalarAsync<decimal>(
            @"SELECT ISNULL(SUM(CreditAmount) - SUM(DebitAmount), 0)
              FROM LedgerEntries
              WHERE SupplierId = @SupplierId AND AccountType = 1
              AND IsDeleted = 0 AND EntryDate < @FromDate",
            new { SupplierId = supplierId, FromDate = fromDate });

        var lines = await conn.QueryAsync<AccountStatementLineDto>(
            @"SELECT Id, EntryDate, ReferenceType, ReferenceId, Description,
              DebitAmount, CreditAmount, 0 AS RunningBalance
              FROM LedgerEntries
              WHERE SupplierId = @SupplierId AND AccountType = 1
              AND IsDeleted = 0 AND EntryDate >= @FromDate AND EntryDate <= @ToDate
              ORDER BY EntryDate ASC, Id ASC",
            new { SupplierId = supplierId, FromDate = fromDate, ToDate = toDate });

        var linesList = lines.ToList();

        // Calculate running balance (for payables: credit increases, debit decreases)
        var running = openingBalance;
        foreach (var line in linesList)
        {
            running += line.CreditAmount - line.DebitAmount;
            line.RunningBalance = running;
        }

        var totalDebits = linesList.Sum(l => l.DebitAmount);
        var totalCredits = linesList.Sum(l => l.CreditAmount);

        return new AccountStatementDto
        {
            AccountId = supplierId,
            AccountName = supplier?.Name ?? "Unknown",
            AccountType = "Supplier",
            FromDate = fromDate,
            ToDate = toDate,
            OpeningBalance = openingBalance,
            TotalDebits = totalDebits,
            TotalCredits = totalCredits,
            ClosingBalance = openingBalance + totalCredits - totalDebits,
            Lines = linesList
        };
    }

    public async Task<AgingReportDto> GetReceivablesAgingAsync()
    {
        using var conn = _db.CreateConnection();

        var sql = @"SELECT
            c.Id AS AccountId,
            c.FullName AS AccountName,
            ISNULL(SUM(CASE WHEN DATEDIFF(DAY, le.EntryDate, GETUTCDATE()) <= 0 THEN le.DebitAmount - le.CreditAmount ELSE 0 END), 0) AS [Current],
            ISNULL(SUM(CASE WHEN DATEDIFF(DAY, le.EntryDate, GETUTCDATE()) BETWEEN 1 AND 30 THEN le.DebitAmount - le.CreditAmount ELSE 0 END), 0) AS Days1To30,
            ISNULL(SUM(CASE WHEN DATEDIFF(DAY, le.EntryDate, GETUTCDATE()) BETWEEN 31 AND 60 THEN le.DebitAmount - le.CreditAmount ELSE 0 END), 0) AS Days31To60,
            ISNULL(SUM(CASE WHEN DATEDIFF(DAY, le.EntryDate, GETUTCDATE()) BETWEEN 61 AND 90 THEN le.DebitAmount - le.CreditAmount ELSE 0 END), 0) AS Days61To90,
            ISNULL(SUM(CASE WHEN DATEDIFF(DAY, le.EntryDate, GETUTCDATE()) > 90 THEN le.DebitAmount - le.CreditAmount ELSE 0 END), 0) AS Over90Days,
            ISNULL(SUM(le.DebitAmount - le.CreditAmount), 0) AS Total
            FROM LedgerEntries le
            INNER JOIN Customers c ON le.CustomerId = c.Id
            WHERE le.AccountType = 0 AND le.IsDeleted = 0 AND le.IsReversed = 0
            GROUP BY c.Id, c.FullName
            HAVING SUM(le.DebitAmount - le.CreditAmount) <> 0
            ORDER BY Total DESC";

        var details = (await conn.QueryAsync<AgingDetailDto>(sql)).ToList();

        return new AgingReportDto
        {
            ReportType = "Receivables",
            AsOfDate = DateTime.UtcNow,
            TotalCurrent = details.Sum(d => d.Current),
            Total1To30 = details.Sum(d => d.Days1To30),
            Total31To60 = details.Sum(d => d.Days31To60),
            Total61To90 = details.Sum(d => d.Days61To90),
            TotalOver90 = details.Sum(d => d.Over90Days),
            GrandTotal = details.Sum(d => d.Total),
            Details = details
        };
    }

    public async Task<AgingReportDto> GetPayablesAgingAsync()
    {
        using var conn = _db.CreateConnection();

        var sql = @"SELECT
            s.Id AS AccountId,
            s.Name AS AccountName,
            ISNULL(SUM(CASE WHEN DATEDIFF(DAY, le.EntryDate, GETUTCDATE()) <= 0 THEN le.CreditAmount - le.DebitAmount ELSE 0 END), 0) AS [Current],
            ISNULL(SUM(CASE WHEN DATEDIFF(DAY, le.EntryDate, GETUTCDATE()) BETWEEN 1 AND 30 THEN le.CreditAmount - le.DebitAmount ELSE 0 END), 0) AS Days1To30,
            ISNULL(SUM(CASE WHEN DATEDIFF(DAY, le.EntryDate, GETUTCDATE()) BETWEEN 31 AND 60 THEN le.CreditAmount - le.DebitAmount ELSE 0 END), 0) AS Days31To60,
            ISNULL(SUM(CASE WHEN DATEDIFF(DAY, le.EntryDate, GETUTCDATE()) BETWEEN 61 AND 90 THEN le.CreditAmount - le.DebitAmount ELSE 0 END), 0) AS Days61To90,
            ISNULL(SUM(CASE WHEN DATEDIFF(DAY, le.EntryDate, GETUTCDATE()) > 90 THEN le.CreditAmount - le.DebitAmount ELSE 0 END), 0) AS Over90Days,
            ISNULL(SUM(le.CreditAmount - le.DebitAmount), 0) AS Total
            FROM LedgerEntries le
            INNER JOIN Suppliers s ON le.SupplierId = s.Id
            WHERE le.AccountType = 1 AND le.IsDeleted = 0 AND le.IsReversed = 0
            GROUP BY s.Id, s.Name
            HAVING SUM(le.CreditAmount - le.DebitAmount) <> 0
            ORDER BY Total DESC";

        var details = (await conn.QueryAsync<AgingDetailDto>(sql)).ToList();

        return new AgingReportDto
        {
            ReportType = "Payables",
            AsOfDate = DateTime.UtcNow,
            TotalCurrent = details.Sum(d => d.Current),
            Total1To30 = details.Sum(d => d.Days1To30),
            Total31To60 = details.Sum(d => d.Days31To60),
            Total61To90 = details.Sum(d => d.Days61To90),
            TotalOver90 = details.Sum(d => d.Over90Days),
            GrandTotal = details.Sum(d => d.Total),
            Details = details
        };
    }
}
