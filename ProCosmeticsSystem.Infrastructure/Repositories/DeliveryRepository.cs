using Dapper;
using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.DTOs.Deliveries;
using ProCosmeticsSystem.Application.Interfaces;
using ProCosmeticsSystem.Domain.Entities;
using ProCosmeticsSystem.Infrastructure.Persistence;

namespace ProCosmeticsSystem.Infrastructure.Repositories;

public class DeliveryRepository : IDeliveryRepository
{
    private readonly DbConnectionFactory _db;

    public DeliveryRepository(DbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<PagedResult<DeliveryDto>> GetAllAsync(int page, int pageSize, int? deliveryManId, string? status)
    {
        using var conn = _db.CreateConnection();

        var where = "WHERE 1=1";
        if (deliveryManId.HasValue) where += " AND d.DeliveryManId = @DeliveryManId";
        if (!string.IsNullOrWhiteSpace(status)) where += " AND d.Status = @Status";

        var totalCount = await conn.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*) FROM Deliveries d {where}",
            new { DeliveryManId = deliveryManId, Status = status });

        var sql = $@"SELECT d.Id, d.SaleId, s.SaleNumber, d.DeliveryManId, dm.Name AS DeliveryManName,
                     d.Status, d.AssignedAt, d.PickedUpAt, d.DeliveredAt, d.DeliveryAddress, d.Notes, d.CreatedAt
                     FROM Deliveries d
                     LEFT JOIN Sales s ON d.SaleId = s.Id
                     LEFT JOIN DeliveryMen dm ON d.DeliveryManId = dm.Id
                     {where}
                     ORDER BY d.CreatedAt DESC
                     OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var items = await conn.QueryAsync<DeliveryDto>(sql, new
        {
            DeliveryManId = deliveryManId,
            Status = status,
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        });

        return new PagedResult<DeliveryDto> { Items = items.ToList(), TotalCount = totalCount, Page = page, PageSize = pageSize };
    }

    public async Task<DeliveryDto?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<DeliveryDto>(
            @"SELECT d.Id, d.SaleId, s.SaleNumber, d.DeliveryManId, dm.Name AS DeliveryManName,
              d.Status, d.AssignedAt, d.PickedUpAt, d.DeliveredAt, d.DeliveryAddress, d.Notes, d.CreatedAt
              FROM Deliveries d
              LEFT JOIN Sales s ON d.SaleId = s.Id
              LEFT JOIN DeliveryMen dm ON d.DeliveryManId = dm.Id
              WHERE d.Id = @Id",
            new { Id = id });
    }

    public async Task<int> CreateAsync(Delivery delivery)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            @"INSERT INTO Deliveries (SaleId, DeliveryManId, Status, AssignedAt, DeliveryAddress, Notes, CreatedAt, CreatedBy, IsDeleted)
              VALUES (@SaleId, @DeliveryManId, @Status, @AssignedAt, @DeliveryAddress, @Notes, @CreatedAt, @CreatedBy, 0);
              SELECT CAST(SCOPE_IDENTITY() AS INT)",
            delivery);
    }

    public async Task UpdateAsync(Delivery delivery)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            @"UPDATE Deliveries SET DeliveryManId = @DeliveryManId, Status = @Status,
              AssignedAt = @AssignedAt, PickedUpAt = @PickedUpAt, DeliveredAt = @DeliveredAt,
              DeliveryAddress = @DeliveryAddress, Notes = @Notes, UpdatedAt = @UpdatedAt, UpdatedBy = @UpdatedBy
              WHERE Id = @Id",
            delivery);
    }
}
