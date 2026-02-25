using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.DTOs.Deliveries;
using ProCosmeticsSystem.Domain.Entities;

namespace ProCosmeticsSystem.Application.Interfaces;

public interface IDeliveryManRepository
{
    Task<PagedResult<DeliveryManDto>> GetAllAsync(int page, int pageSize, string? search);
    Task<DeliveryManDto?> GetByIdAsync(int id);
    Task<int> CreateAsync(DeliveryMan deliveryMan);
    Task UpdateAsync(DeliveryMan deliveryMan);
    Task SoftDeleteAsync(int id);
}
