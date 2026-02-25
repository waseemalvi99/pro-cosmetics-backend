using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.DTOs.Suppliers;
using ProCosmeticsSystem.Application.Exceptions;
using ProCosmeticsSystem.Application.Interfaces;
using ProCosmeticsSystem.Domain.Entities;

namespace ProCosmeticsSystem.Application.Services;

public class SupplierService
{
    private readonly ISupplierRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public SupplierService(ISupplierRepository repo, ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public Task<PagedResult<SupplierDto>> GetAllAsync(int page, int pageSize, string? search)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize > 100 ? 100 : pageSize;
        return _repo.GetAllAsync(page, pageSize, search);
    }

    public async Task<SupplierDto> GetByIdAsync(int id)
    {
        return await _repo.GetByIdAsync(id) ?? throw new NotFoundException("Supplier", id);
    }

    public async Task<int> CreateAsync(CreateSupplierRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException("Name", "Supplier name is required.");

        var supplier = new Supplier
        {
            Name = request.Name,
            ContactPerson = request.ContactPerson,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address,
            Notes = request.Notes,
            CreatedBy = _currentUser.UserId
        };

        return await _repo.CreateAsync(supplier);
    }

    public async Task UpdateAsync(int id, UpdateSupplierRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException("Name", "Supplier name is required.");

        _ = await _repo.GetByIdAsync(id) ?? throw new NotFoundException("Supplier", id);

        var supplier = new Supplier
        {
            Id = id,
            Name = request.Name,
            ContactPerson = request.ContactPerson,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address,
            Notes = request.Notes,
            IsActive = request.IsActive,
            UpdatedBy = _currentUser.UserId,
            UpdatedAt = DateTime.UtcNow
        };

        await _repo.UpdateAsync(supplier);
    }

    public async Task DeleteAsync(int id)
    {
        _ = await _repo.GetByIdAsync(id) ?? throw new NotFoundException("Supplier", id);
        await _repo.SoftDeleteAsync(id);
    }
}
