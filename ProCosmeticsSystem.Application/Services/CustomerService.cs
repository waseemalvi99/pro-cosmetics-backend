using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.DTOs.Customers;
using ProCosmeticsSystem.Application.Exceptions;
using ProCosmeticsSystem.Application.Interfaces;
using ProCosmeticsSystem.Domain.Entities;

namespace ProCosmeticsSystem.Application.Services;

public class CustomerService
{
    private readonly ICustomerRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public CustomerService(ICustomerRepository repo, ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public Task<PagedResult<CustomerDto>> GetAllAsync(int page, int pageSize, string? search)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize > 100 ? 100 : pageSize;
        return _repo.GetAllAsync(page, pageSize, search);
    }

    public async Task<CustomerDto> GetByIdAsync(int id)
    {
        return await _repo.GetByIdAsync(id)
            ?? throw new NotFoundException("Customer", id);
    }

    public async Task<int> CreateAsync(CreateCustomerRequest request)
    {
        ValidateCustomer(request.FullName);

        var customer = new Customer
        {
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address,
            City = request.City,
            Notes = request.Notes,
            CreatedBy = _currentUser.UserId
        };

        return await _repo.CreateAsync(customer);
    }

    public async Task UpdateAsync(int id, UpdateCustomerRequest request)
    {
        ValidateCustomer(request.FullName);

        var existing = await _repo.GetByIdAsync(id)
            ?? throw new NotFoundException("Customer", id);

        var customer = new Customer
        {
            Id = id,
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address,
            City = request.City,
            Notes = request.Notes,
            IsActive = request.IsActive,
            UpdatedBy = _currentUser.UserId,
            UpdatedAt = DateTime.UtcNow
        };

        await _repo.UpdateAsync(customer);
    }

    public async Task DeleteAsync(int id)
    {
        var existing = await _repo.GetByIdAsync(id)
            ?? throw new NotFoundException("Customer", id);
        await _repo.SoftDeleteAsync(id);
    }

    private static void ValidateCustomer(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ValidationException("FullName", "Customer name is required.");
    }
}
