using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.DTOs.Deliveries;
using ProCosmeticsSystem.Domain.Entities;

namespace ProCosmeticsSystem.Application.Interfaces;

public interface IDeliveryRepository
{
    Task<PagedResult<DeliveryDto>> GetAllAsync(int page, int pageSize, int? deliveryManId, string? status);
    Task<DeliveryDto?> GetByIdAsync(int id);
    Task<int> CreateAsync(Delivery delivery);
    Task UpdateAsync(Delivery delivery);
}
