using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.DTOs.Payments;
using ProCosmeticsSystem.Domain.Entities;

namespace ProCosmeticsSystem.Application.Interfaces;

public interface IPaymentRepository
{
    Task<PagedResult<PaymentDto>> GetAllAsync(int page, int pageSize, int? customerId, int? supplierId);
    Task<PaymentDto?> GetByIdAsync(int id);
    Task<int> CreateAsync(Payment payment);
    Task SoftDeleteAsync(int id);
}
