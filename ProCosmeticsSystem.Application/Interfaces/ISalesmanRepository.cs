using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.DTOs.Sales;
using ProCosmeticsSystem.Domain.Entities;

namespace ProCosmeticsSystem.Application.Interfaces;

public interface ISalesmanRepository
{
    Task<PagedResult<SalesmanDto>> GetAllAsync(int page, int pageSize, string? search);
    Task<SalesmanDto?> GetByIdAsync(int id);
    Task<int> CreateAsync(Salesman salesman);
    Task UpdateAsync(Salesman salesman);
    Task SoftDeleteAsync(int id);
}
