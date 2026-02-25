using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.DTOs.Customers;
using ProCosmeticsSystem.Domain.Entities;

namespace ProCosmeticsSystem.Application.Interfaces;

public interface ICustomerRepository
{
    Task<PagedResult<CustomerDto>> GetAllAsync(int page, int pageSize, string? search);
    Task<CustomerDto?> GetByIdAsync(int id);
    Task<int> CreateAsync(Customer customer);
    Task UpdateAsync(Customer customer);
    Task SoftDeleteAsync(int id);
}
