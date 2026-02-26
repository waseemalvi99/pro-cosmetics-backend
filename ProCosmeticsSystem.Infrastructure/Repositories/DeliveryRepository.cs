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

        var statusCase = @"CASE d.Status
                         WHEN 0 THEN 'Pending'
                         WHEN 1 THEN 'Assigned'
                         WHEN 2 THEN 'PickedUp'
                         WHEN 3 THEN 'InTransit'
                         WHEN 4 THEN 'Delivered'
                         WHEN 5 THEN 'Failed'
                     END";

        // Convert status string filter to int for DB comparison
        int? statusInt = status?.ToLower() switch
        {
            "pending" => 0,
            "assigned" => 1,
            "pickedup" => 2,
            "intransit" => 3,
            "delivered" => 4,
            "failed" => 5,
            _ => null
        };

        var where = "WHERE 1=1";
        if (deliveryManId.HasValue) where += " AND d.DeliveryManId = @DeliveryManId";
        if (statusInt.HasValue) where += " AND d.Status = @StatusInt";

        var totalCount = await conn.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*) FROM Deliveries d {where}",
            new { DeliveryManId = deliveryManId, StatusInt = statusInt });

        var sql = $@"SELECT d.Id, d.SaleId, s.SaleNumber, d.DeliveryManId, dm.Name AS DeliveryManName,
                     {statusCase} AS Status,
                     d.AssignedAt, d.PickedUpAt, d.DeliveredAt, d.DeliveryAddress, d.Notes, d.CreatedAt
                     FROM Deliveries d
                     LEFT JOIN Sales s ON d.SaleId = s.Id
                     LEFT JOIN DeliveryMen dm ON d.DeliveryManId = dm.Id
                     {where}
                     ORDER BY d.CreatedAt DESC
                     OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var items = await conn.QueryAsync<DeliveryDto>(sql, new
        {
            DeliveryManId = deliveryManId,
            StatusInt = statusInt,
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
              CASE d.Status
                  WHEN 0 THEN 'Pending'
                  WHEN 1 THEN 'Assigned'
                  WHEN 2 THEN 'PickedUp'
                  WHEN 3 THEN 'InTransit'
                  WHEN 4 THEN 'Delivered'
                  WHEN 5 THEN 'Failed'
              END AS Status,
              d.AssignedAt, d.PickedUpAt, d.DeliveredAt, d.DeliveryAddress, d.Notes, d.CreatedAt
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
