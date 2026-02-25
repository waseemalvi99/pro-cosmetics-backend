using ProCosmeticsSystem.API.Middlewares;
using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.DTOs.Customers;
using ProCosmeticsSystem.Application.Services;

namespace ProCosmeticsSystem.API.Endpoints;

public static class CustomerEndpoints
{
    public static void MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/customers").WithTags("Customers").RequireAuthorization();

        group.MapGet("/", async (int? page, int? pageSize, string? search, CustomerService service) =>
        {
            var result = await service.GetAllAsync(page ?? 1, pageSize ?? 20, search);
            return Results.Ok(ApiResponse<PagedResult<CustomerDto>>.Ok(result));
        }).RequirePermission("Customers:View");

        group.MapGet("/{id:int}", async (int id, CustomerService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return Results.Ok(ApiResponse<CustomerDto>.Ok(result));
        }).RequirePermission("Customers:View");

        group.MapPost("/", async (CreateCustomerRequest request, CustomerService service) =>
        {
            var id = await service.CreateAsync(request);
            return Results.Created($"/api/customers/{id}", ApiResponse<int>.Ok(id, "Customer created successfully."));
        }).RequirePermission("Customers:Create");

        group.MapPut("/{id:int}", async (int id, UpdateCustomerRequest request, CustomerService service) =>
        {
            await service.UpdateAsync(id, request);
            return Results.Ok(ApiResponse.Ok("Customer updated successfully."));
        }).RequirePermission("Customers:Edit");

        group.MapDelete("/{id:int}", async (int id, CustomerService service) =>
        {
            await service.DeleteAsync(id);
            return Results.Ok(ApiResponse.Ok("Customer deleted successfully."));
        }).RequirePermission("Customers:Delete");
    }
}
