using Dapper;
using ProCosmeticsSystem.Application.DTOs.Combos;
using ProCosmeticsSystem.Application.Interfaces;
using ProCosmeticsSystem.Infrastructure.Persistence;

namespace ProCosmeticsSystem.Infrastructure.Repositories;

public class ComboRepository : IComboRepository
{
    private readonly DbConnectionFactory _db;

    public ComboRepository(DbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<List<CustomerComboDto>> SearchCustomersAsync(string? search, int limit)
    {
        using var conn = _db.CreateConnection();
        var results = await conn.QueryAsync<CustomerComboDto>(
            @"SELECT TOP(@Limit) Id, FullName, CreditLimit
              FROM Customers
              WHERE IsDeleted = 0 AND IsActive = 1
                AND (@Search IS NULL OR FullName LIKE @Search)
              ORDER BY FullName",
            new { Limit = limit, Search = FormatSearch(search) });
        return results.ToList();
    }

    public async Task<List<SupplierComboDto>> SearchSuppliersAsync(string? search, int limit)
    {
        using var conn = _db.CreateConnection();
        var results = await conn.QueryAsync<SupplierComboDto>(
            @"SELECT TOP(@Limit) Id, Name, PaymentTermDays
              FROM Suppliers
              WHERE IsDeleted = 0 AND IsActive = 1
                AND (@Search IS NULL OR Name LIKE @Search)
              ORDER BY Name",
            new { Limit = limit, Search = FormatSearch(search) });
        return results.ToList();
    }

    public async Task<List<ProductComboDto>> SearchProductsAsync(string? search, int limit)
    {
        using var conn = _db.CreateConnection();
        var results = await conn.QueryAsync<ProductComboDto>(
            @"SELECT TOP(@Limit) p.Id, p.Name, p.SKU, p.SalePrice,
                     ISNULL(i.QuantityOnHand, 0) AS QuantityOnHand,
                     '/uploads/products/' + pi.FilePath AS ImagePath
              FROM Products p
              LEFT JOIN Inventory i ON p.Id = i.ProductId
              LEFT JOIN ProductImages pi ON p.Id = pi.ProductId AND pi.IsPrimary = 1
              WHERE p.IsDeleted = 0 AND p.IsActive = 1
                AND (@Search IS NULL OR p.Name LIKE @Search OR p.SKU LIKE @Search)
              ORDER BY p.Name",
            new { Limit = limit, Search = FormatSearch(search) });
        return results.ToList();
    }

    public async Task<List<ComboItemDto>> SearchSalesmenAsync(string? search, int limit)
    {
        using var conn = _db.CreateConnection();
        var results = await conn.QueryAsync<ComboItemDto>(
            @"SELECT TOP(@Limit) Id, Name
              FROM Salesmen
              WHERE IsDeleted = 0 AND IsActive = 1
                AND (@Search IS NULL OR Name LIKE @Search)
              ORDER BY Name",
            new { Limit = limit, Search = FormatSearch(search) });
        return results.ToList();
    }

    public async Task<List<ComboItemDto>> SearchCategoriesAsync(string? search, int limit)
    {
        using var conn = _db.CreateConnection();
        var results = await conn.QueryAsync<ComboItemDto>(
            @"SELECT TOP(@Limit) Id, Name
              FROM Categories
              WHERE IsDeleted = 0
                AND (@Search IS NULL OR Name LIKE @Search)
              ORDER BY Name",
            new { Limit = limit, Search = FormatSearch(search) });
        return results.ToList();
    }

    public async Task<List<DeliveryManComboDto>> SearchDeliveryMenAsync(string? search, int limit)
    {
        using var conn = _db.CreateConnection();
        var results = await conn.QueryAsync<DeliveryManComboDto>(
            @"SELECT TOP(@Limit) Id, Name, IsAvailable
              FROM DeliveryMen
              WHERE IsDeleted = 0 AND IsActive = 1
                AND (@Search IS NULL OR Name LIKE @Search)
              ORDER BY Name",
            new { Limit = limit, Search = FormatSearch(search) });
        return results.ToList();
    }

    public async Task<List<ComboItemDto>> SearchUsersAsync(string? search, int limit)
    {
        using var conn = _db.CreateConnection();
        var results = await conn.QueryAsync<ComboItemDto>(
            @"SELECT TOP(@Limit) Id, FullName AS Name
              FROM AspNetUsers
              WHERE IsActive = 1
                AND (@Search IS NULL OR FullName LIKE @Search)
              ORDER BY FullName",
            new { Limit = limit, Search = FormatSearch(search) });
        return results.ToList();
    }

    public async Task<List<ComboItemDto>> SearchRolesAsync(string? search, int limit)
    {
        using var conn = _db.CreateConnection();
        var results = await conn.QueryAsync<ComboItemDto>(
            @"SELECT TOP(@Limit) Id, Name
              FROM AspNetRoles
              WHERE (@Search IS NULL OR Name LIKE @Search)
              ORDER BY Name",
            new { Limit = limit, Search = FormatSearch(search) });
        return results.ToList();
    }

    public async Task<List<SaleComboDto>> SearchSalesAsync(int customerId, string? search, int limit)
    {
        using var conn = _db.CreateConnection();
        var results = await conn.QueryAsync<SaleComboDto>(
            @"SELECT TOP(@Limit) Id, SaleNumber, TotalAmount, SaleDate
              FROM Sales
              WHERE IsDeleted = 0 AND CustomerId = @CustomerId
                AND Status != 2
                AND (@Search IS NULL OR SaleNumber LIKE @Search)
              ORDER BY SaleDate DESC",
            new { Limit = limit, CustomerId = customerId, Search = FormatSearch(search) });
        return results.ToList();
    }

    public async Task<List<PurchaseOrderComboDto>> SearchPurchaseOrdersAsync(int supplierId, string? search, int limit)
    {
        using var conn = _db.CreateConnection();
        var results = await conn.QueryAsync<PurchaseOrderComboDto>(
            @"SELECT TOP(@Limit) Id, OrderNumber, TotalAmount, OrderDate
              FROM PurchaseOrders
              WHERE IsDeleted = 0 AND SupplierId = @SupplierId
                AND Status != 4
                AND (@Search IS NULL OR OrderNumber LIKE @Search)
              ORDER BY OrderDate DESC",
            new { Limit = limit, SupplierId = supplierId, Search = FormatSearch(search) });
        return results.ToList();
    }

    private static string? FormatSearch(string? search)
        => string.IsNullOrWhiteSpace(search) ? null : $"%{search}%";
}
