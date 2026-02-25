using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.DTOs.Suppliers;
using ProCosmeticsSystem.Domain.Entities;

namespace ProCosmeticsSystem.Application.Interfaces;

public interface ISupplierRepository
{
    Task<PagedResult<SupplierDto>> GetAllAsync(int page, int pageSize, string? search);
    Task<SupplierDto?> GetByIdAsync(int id);
    Task<int> CreateAsync(Supplier supplier);
    Task UpdateAsync(Supplier supplier);
    Task SoftDeleteAsync(int id);
}
