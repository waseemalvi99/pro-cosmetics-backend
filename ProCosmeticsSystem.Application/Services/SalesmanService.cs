using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.DTOs.Sales;
using ProCosmeticsSystem.Application.Exceptions;
using ProCosmeticsSystem.Application.Interfaces;
using ProCosmeticsSystem.Domain.Entities;

namespace ProCosmeticsSystem.Application.Services;

public class SalesmanService
{
    private readonly ISalesmanRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public SalesmanService(ISalesmanRepository repo, ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public Task<PagedResult<SalesmanDto>> GetAllAsync(int page, int pageSize, string? search)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize > 100 ? 100 : pageSize;
        return _repo.GetAllAsync(page, pageSize, search);
    }

    public async Task<SalesmanDto> GetByIdAsync(int id) =>
        await _repo.GetByIdAsync(id) ?? throw new NotFoundException("Salesman", id);

    public async Task<int> CreateAsync(CreateSalesmanRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException("Name", "Salesman name is required.");

        return await _repo.CreateAsync(new Salesman
        {
            Name = request.Name,
            Phone = request.Phone,
            Email = request.Email,
            CommissionRate = request.CommissionRate,
            CreatedBy = _currentUser.UserId
        });
    }

    public async Task UpdateAsync(int id, UpdateSalesmanRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException("Name", "Salesman name is required.");

        _ = await _repo.GetByIdAsync(id) ?? throw new NotFoundException("Salesman", id);

        await _repo.UpdateAsync(new Salesman
        {
            Id = id,
            Name = request.Name,
            Phone = request.Phone,
            Email = request.Email,
            CommissionRate = request.CommissionRate,
            IsActive = request.IsActive,
            UpdatedBy = _currentUser.UserId,
            UpdatedAt = DateTime.UtcNow
        });
    }

    public async Task DeleteAsync(int id)
    {
        _ = await _repo.GetByIdAsync(id) ?? throw new NotFoundException("Salesman", id);
        await _repo.SoftDeleteAsync(id);
    }
}
