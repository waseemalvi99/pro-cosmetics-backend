using Dapper;
using ProCosmeticsSystem.Application.DTOs.Reports;
using ProCosmeticsSystem.Application.Interfaces;
using ProCosmeticsSystem.Infrastructure.Persistence;

namespace ProCosmeticsSystem.Infrastructure.Repositories;

public class ReportRepository : IReportRepository
{
    private readonly DbConnectionFactory _db;

    public ReportRepository(DbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<SalesReportDto> GetSalesReportAsync(DateTime from, DateTime to, string groupBy)
    {
        using var conn = _db.CreateConnection();

        var dateFormat = groupBy.ToLower() switch
        {
            "week" => "DATEPART(YEAR, SaleDate), DATEPART(WEEK, SaleDate)",
            "month" => "FORMAT(SaleDate, 'yyyy-MM')",
            _ => "CAST(SaleDate AS DATE)"
        };

        var periodSelect = groupBy.ToLower() switch
        {
            "week" => "CONCAT(DATEPART(YEAR, SaleDate), '-W', DATEPART(WEEK, SaleDate))",
            "month" => "FORMAT(SaleDate, 'yyyy-MM')",
            _ => "CAST(SaleDate AS VARCHAR(10))"
        };

        var sql = $@"SELECT {periodSelect} AS Period,
                     COUNT(*) AS OrderCount,
                     SUM(TotalAmount) AS Revenue,
                     SUM(Discount) AS Discount,
                     SUM(TotalAmount - Discount) AS NetRevenue
                     FROM Sales
                     WHERE SaleDate >= @From AND SaleDate <= @To AND Status = 0
                     GROUP BY {dateFormat}
                     ORDER BY MIN(SaleDate)";

        var items = (await conn.QueryAsync<SalesReportItem>(sql, new { From = from, To = to })).ToList();

        return new SalesReportDto
        {
            Items = items,
            TotalRevenue = items.Sum(i => i.Revenue),
            TotalOrders = items.Sum(i => i.OrderCount),
            AverageOrderValue = items.Count > 0 ? items.Sum(i => i.Revenue) / items.Sum(i => i.OrderCount) : 0
        };
    }

    public async Task<List<TopProductDto>> GetTopProductsAsync(DateTime from, DateTime to, int top)
    {
        using var conn = _db.CreateConnection();
        var results = await conn.QueryAsync<TopProductDto>(
            @"SELECT TOP (@Top) si.ProductId, p.Name AS ProductName, SUM(si.Quantity) AS QuantitySold, SUM(si.TotalPrice) AS Revenue
              FROM SaleItems si
              INNER JOIN Products p ON si.ProductId = p.Id
              INNER JOIN Sales s ON si.SaleId = s.Id
              WHERE s.SaleDate >= @From AND s.SaleDate <= @To AND s.Status = 0
              GROUP BY si.ProductId, p.Name
              ORDER BY Revenue DESC",
            new { From = from, To = to, Top = top });
        return results.ToList();
    }

    public async Task<List<SalesmanPerformanceDto>> GetSalesmanPerformanceAsync(DateTime from, DateTime to)
    {
        using var conn = _db.CreateConnection();
        var results = await conn.QueryAsync<SalesmanPerformanceDto>(
            @"SELECT sm.Id AS SalesmanId, sm.Name AS SalesmanName, COUNT(s.Id) AS TotalSales,
              SUM(s.TotalAmount) AS TotalRevenue, sm.CommissionRate,
              SUM(s.TotalAmount) * sm.CommissionRate / 100 AS CommissionAmount
              FROM Sales s
              INNER JOIN Salesmen sm ON s.SalesmanId = sm.Id
              WHERE s.SaleDate >= @From AND s.SaleDate <= @To AND s.Status = 0
              GROUP BY sm.Id, sm.Name, sm.CommissionRate
              ORDER BY TotalRevenue DESC",
            new { From = from, To = to });
        return results.ToList();
    }

    public async Task<InventoryReportDto> GetInventoryReportAsync()
    {
        using var conn = _db.CreateConnection();

        var items = (await conn.QueryAsync<InventoryReportItem>(
            @"SELECT p.Id AS ProductId, p.Name AS ProductName, p.SKU, i.QuantityOnHand, p.CostPrice,
              i.QuantityOnHand * p.CostPrice AS StockValue, p.ReorderLevel,
              CASE WHEN i.QuantityOnHand <= p.ReorderLevel THEN 1 ELSE 0 END AS IsLowStock
              FROM Products p
              INNER JOIN Inventory i ON p.Id = i.ProductId
              WHERE p.IsDeleted = 0
              ORDER BY i.QuantityOnHand ASC")).ToList();

        return new InventoryReportDto
        {
            TotalProducts = items.Count,
            LowStockCount = items.Count(i => i.IsLowStock),
            OutOfStockCount = items.Count(i => i.QuantityOnHand == 0),
            TotalStockValue = items.Sum(i => i.StockValue),
            Items = items
        };
    }

    public async Task<PurchaseReportDto> GetPurchaseReportAsync(DateTime from, DateTime to)
    {
        using var conn = _db.CreateConnection();

        var items = (await conn.QueryAsync<PurchaseReportItem>(
            @"SELECT s.Id AS SupplierId, s.Name AS SupplierName, COUNT(po.Id) AS OrderCount, SUM(po.TotalAmount) AS TotalSpent
              FROM PurchaseOrders po
              INNER JOIN Suppliers s ON po.SupplierId = s.Id
              WHERE po.OrderDate >= @From AND po.OrderDate <= @To
              GROUP BY s.Id, s.Name
              ORDER BY TotalSpent DESC",
            new { From = from, To = to })).ToList();

        return new PurchaseReportDto
        {
            TotalOrders = items.Sum(i => i.OrderCount),
            TotalSpent = items.Sum(i => i.TotalSpent),
            Items = items
        };
    }

    public async Task<DeliveryReportDto> GetDeliveryReportAsync(DateTime from, DateTime to)
    {
        using var conn = _db.CreateConnection();

        var result = await conn.QueryFirstOrDefaultAsync<DeliveryReportDto>(
            @"SELECT COUNT(*) AS TotalDeliveries,
              SUM(CASE WHEN Status = 4 THEN 1 ELSE 0 END) AS DeliveredCount,
              SUM(CASE WHEN Status = 5 THEN 1 ELSE 0 END) AS FailedCount,
              SUM(CASE WHEN Status = 0 THEN 1 ELSE 0 END) AS PendingCount,
              CASE WHEN COUNT(*) > 0 THEN
                CAST(SUM(CASE WHEN Status = 4 THEN 1 ELSE 0 END) AS DECIMAL) / COUNT(*) * 100
              ELSE 0 END AS SuccessRate,
              AVG(CASE WHEN Status = 4 AND DeliveredAt IS NOT NULL AND AssignedAt IS NOT NULL
                THEN DATEDIFF(MINUTE, AssignedAt, DeliveredAt) / 60.0 ELSE NULL END) AS AverageDeliveryTimeHours
              FROM Deliveries
              WHERE CreatedAt >= @From AND CreatedAt <= @To",
            new { From = from, To = to });

        return result ?? new DeliveryReportDto();
    }

    public async Task<FinancialSummaryDto> GetFinancialSummaryAsync(DateTime from, DateTime to)
    {
        using var conn = _db.CreateConnection();

        var revenue = await conn.ExecuteScalarAsync<decimal>(
            "SELECT ISNULL(SUM(TotalAmount), 0) FROM Sales WHERE SaleDate >= @From AND SaleDate <= @To AND Status = 0",
            new { From = from, To = to });

        var costs = await conn.ExecuteScalarAsync<decimal>(
            "SELECT ISNULL(SUM(TotalAmount), 0) FROM PurchaseOrders WHERE OrderDate >= @From AND OrderDate <= @To AND Status = 3",
            new { From = from, To = to });

        var salesCount = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Sales WHERE SaleDate >= @From AND SaleDate <= @To AND Status = 0",
            new { From = from, To = to });

        var purchasesCount = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM PurchaseOrders WHERE OrderDate >= @From AND OrderDate <= @To",
            new { From = from, To = to });

        var grossProfit = revenue - costs;

        return new FinancialSummaryDto
        {
            TotalRevenue = revenue,
            TotalCosts = costs,
            GrossProfit = grossProfit,
            ProfitMargin = revenue > 0 ? Math.Round(grossProfit / revenue * 100, 2) : 0,
            TotalSales = salesCount,
            TotalPurchases = purchasesCount
        };
    }
}
