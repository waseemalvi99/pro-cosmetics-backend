using ProCosmeticsSystem.Application.DTOs.Products;
using ProCosmeticsSystem.Application.Exceptions;
using ProCosmeticsSystem.Application.Interfaces;
using ProCosmeticsSystem.Domain.Entities;

namespace ProCosmeticsSystem.Application.Services;

public class CategoryService
{
    private readonly ICategoryRepository _repo;

    public CategoryService(ICategoryRepository repo)
    {
        _repo = repo;
    }

    public Task<List<CategoryDto>> GetAllAsync() => _repo.GetAllAsync();

    public async Task<CategoryDto> GetByIdAsync(int id)
    {
        return await _repo.GetByIdAsync(id) ?? throw new NotFoundException("Category", id);
    }

    public async Task<int> CreateAsync(CreateCategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException("Name", "Category name is required.");

        var category = new Category
        {
            Name = request.Name,
            Description = request.Description,
            ParentCategoryId = request.ParentCategoryId
        };

        return await _repo.CreateAsync(category);
    }

    public async Task UpdateAsync(int id, UpdateCategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException("Name", "Category name is required.");

        _ = await _repo.GetByIdAsync(id) ?? throw new NotFoundException("Category", id);

        var category = new Category
        {
            Id = id,
            Name = request.Name,
            Description = request.Description,
            ParentCategoryId = request.ParentCategoryId,
            UpdatedAt = DateTime.UtcNow
        };

        await _repo.UpdateAsync(category);
    }

    public async Task DeleteAsync(int id)
    {
        _ = await _repo.GetByIdAsync(id) ?? throw new NotFoundException("Category", id);
        await _repo.DeleteAsync(id);
    }
}
