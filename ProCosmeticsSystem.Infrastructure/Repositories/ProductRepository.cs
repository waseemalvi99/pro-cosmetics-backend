using Dapper;
using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.DTOs.Products;
using ProCosmeticsSystem.Application.Interfaces;
using ProCosmeticsSystem.Domain.Entities;
using ProCosmeticsSystem.Infrastructure.Persistence;

namespace ProCosmeticsSystem.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly DbConnectionFactory _db;

    public ProductRepository(DbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<PagedResult<ProductDto>> GetAllAsync(int page, int pageSize, string? search, int? categoryId)
    {
        using var conn = _db.CreateConnection();

        var where = "WHERE p.IsDeleted = 0";
        if (!string.IsNullOrWhiteSpace(search))
            where += " AND (p.Name LIKE @Search OR p.SKU LIKE @Search OR p.Barcode LIKE @Search)";
        if (categoryId.HasValue)
            where += " AND p.CategoryId = @CategoryId";

        var countSql = $"SELECT COUNT(*) FROM Products p {where}";
        var totalCount = await conn.ExecuteScalarAsync<int>(countSql, new { Search = $"%{search}%", CategoryId = categoryId });

        var sql = $@"SELECT p.Id, p.Name, p.SKU, p.Barcode, p.Description, p.CategoryId, c.Name AS CategoryName,
                     p.CostPrice, p.SalePrice, p.ReorderLevel, p.IsActive, p.CreatedAt,
                     ISNULL(i.QuantityOnHand, 0) AS QuantityOnHand
                     FROM Products p
                     LEFT JOIN Categories c ON p.CategoryId = c.Id
                     LEFT JOIN Inventory i ON p.Id = i.ProductId
                     {where}
                     ORDER BY p.CreatedAt DESC
                     OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var items = (await conn.QueryAsync<ProductDto>(sql, new
        {
            Search = $"%{search}%",
            CategoryId = categoryId,
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        })).ToList();

        if (items.Count > 0)
        {
            var productIds = items.Select(p => p.Id).ToList();
            var images = await conn.QueryAsync<ProductImageDto>(
                @"SELECT Id, ProductId, FileName, '/uploads/products/' + FilePath AS Url, IsPrimary, SortOrder
                  FROM ProductImages
                  WHERE ProductId IN @ProductIds
                  ORDER BY SortOrder",
                new { ProductIds = productIds });

            var imagesByProduct = images.GroupBy(i => i.ProductId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var item in items)
            {
                if (imagesByProduct.TryGetValue(item.Id, out var productImages))
                    item.Images = productImages;
            }
        }

        return new PagedResult<ProductDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ProductDto?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        var product = await conn.QueryFirstOrDefaultAsync<ProductDto>(
            @"SELECT p.Id, p.Name, p.SKU, p.Barcode, p.Description, p.CategoryId, c.Name AS CategoryName,
              p.CostPrice, p.SalePrice, p.ReorderLevel, p.IsActive, p.CreatedAt,
              ISNULL(i.QuantityOnHand, 0) AS QuantityOnHand
              FROM Products p
              LEFT JOIN Categories c ON p.CategoryId = c.Id
              LEFT JOIN Inventory i ON p.Id = i.ProductId
              WHERE p.Id = @Id AND p.IsDeleted = 0",
            new { Id = id });

        if (product is not null)
        {
            var images = await conn.QueryAsync<ProductImageDto>(
                @"SELECT Id, ProductId, FileName, '/uploads/products/' + FilePath AS Url, IsPrimary, SortOrder
                  FROM ProductImages
                  WHERE ProductId = @Id
                  ORDER BY SortOrder",
                new { Id = id });
            product.Images = images.ToList();
        }

        return product;
    }

    public async Task<BarcodeLookupResult?> GetByBarcodeAsync(string barcode)
    {
        using var conn = _db.CreateConnection();
        var product = await conn.QueryFirstOrDefaultAsync<BarcodeLookupResult>(
            @"SELECT p.Id AS ProductId, p.Name AS ProductName, p.SKU, p.Barcode, p.CostPrice, p.SalePrice,
              ISNULL(i.QuantityOnHand, 0) AS QuantityOnHand
              FROM Products p
              LEFT JOIN Inventory i ON p.Id = i.ProductId
              WHERE p.Barcode = @Barcode AND p.IsDeleted = 0 AND p.IsActive = 1",
            new { Barcode = barcode });

        if (product is not null)
        {
            var images = await conn.QueryAsync<ProductImageDto>(
                @"SELECT Id, ProductId, FileName, '/uploads/products/' + FilePath AS Url, IsPrimary, SortOrder
                  FROM ProductImages
                  WHERE ProductId = @ProductId
                  ORDER BY SortOrder",
                new { ProductId = product.ProductId });
            product.Images = images.ToList();
        }

        return product;
    }

    public async Task<List<BarcodeLabelItem>> GetByIdsForLabelsAsync(List<int> ids)
    {
        using var conn = _db.CreateConnection();
        var results = await conn.QueryAsync<BarcodeLabelItem>(
            @"SELECT p.Id AS ProductId, p.Name AS ProductName, p.SKU, p.Barcode, p.SalePrice
              FROM Products p
              WHERE p.Id IN @Ids AND p.IsDeleted = 0",
            new { Ids = ids });
        return results.ToList();
    }

    public async Task<int> CreateAsync(Product product)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            @"INSERT INTO Products (Name, SKU, Barcode, Description, CategoryId, CostPrice, SalePrice, ReorderLevel, IsActive, IsDeleted, CreatedAt, CreatedBy)
              VALUES (@Name, @SKU, @Barcode, @Description, @CategoryId, @CostPrice, @SalePrice, @ReorderLevel, 1, 0, @CreatedAt, @CreatedBy);
              SELECT CAST(SCOPE_IDENTITY() AS INT)",
            product);
    }

    public async Task UpdateAsync(Product product)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            @"UPDATE Products SET Name = @Name, SKU = @SKU, Barcode = @Barcode, Description = @Description,
              CategoryId = @CategoryId, CostPrice = @CostPrice, SalePrice = @SalePrice, ReorderLevel = @ReorderLevel,
              IsActive = @IsActive, UpdatedAt = @UpdatedAt, UpdatedBy = @UpdatedBy
              WHERE Id = @Id AND IsDeleted = 0",
            product);
    }

    public async Task SoftDeleteAsync(int id)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync("UPDATE Products SET IsDeleted = 1, UpdatedAt = @UpdatedAt WHERE Id = @Id",
            new { Id = id, UpdatedAt = DateTime.UtcNow });
    }
}
