using ProCosmeticsSystem.API.Middlewares;
using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.DTOs.Products;
using ProCosmeticsSystem.Application.Services;

namespace ProCosmeticsSystem.API.Endpoints;

public static class CategoryEndpoints
{
    public static void MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/categories").WithTags("Categories").RequireAuthorization();

        group.MapGet("/", async (CategoryService service) =>
        {
            var result = await service.GetAllAsync();
            return Results.Ok(ApiResponse<List<CategoryDto>>.Ok(result));
        }).RequirePermission("Products:View");

        group.MapGet("/{id:int}", async (int id, CategoryService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return Results.Ok(ApiResponse<CategoryDto>.Ok(result));
        }).RequirePermission("Products:View");

        group.MapPost("/", async (CreateCategoryRequest request, CategoryService service) =>
        {
            var id = await service.CreateAsync(request);
            return Results.Created($"/api/categories/{id}", ApiResponse<int>.Ok(id, "Category created successfully."));
        }).RequirePermission("Products:Create");

        group.MapPut("/{id:int}", async (int id, UpdateCategoryRequest request, CategoryService service) =>
        {
            await service.UpdateAsync(id, request);
            return Results.Ok(ApiResponse.Ok("Category updated successfully."));
        }).RequirePermission("Products:Edit");

        group.MapDelete("/{id:int}", async (int id, CategoryService service) =>
        {
            await service.DeleteAsync(id);
            return Results.Ok(ApiResponse.Ok("Category deleted successfully."));
        }).RequirePermission("Products:Delete");
    }
}
