using ProCosmeticsSystem.API.Middlewares;
using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.DTOs.Products;
using ProCosmeticsSystem.Application.Services;

namespace ProCosmeticsSystem.API.Endpoints;

public static class ProductImageEndpoints
{
    public static void MapProductImageEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products/{productId:int}/images").WithTags("Product Images").RequireAuthorization();

        group.MapPost("/", async (int productId, IFormFile file, ProductImageService service) =>
        {
            using var stream = file.OpenReadStream();
            var result = await service.UploadAsync(productId, stream, file.FileName);
            return Results.Ok(ApiResponse<ProductImageDto>.Ok(result, "Image uploaded successfully."));
        }).RequirePermission("Products:Edit").DisableAntiforgery();

        group.MapGet("/", async (int productId, ProductImageService service) =>
        {
            var result = await service.GetByProductIdAsync(productId);
            return Results.Ok(ApiResponse<List<ProductImageDto>>.Ok(result));
        }).RequirePermission("Products:View");

        group.MapDelete("/{imageId:int}", async (int productId, int imageId, ProductImageService service) =>
        {
            await service.DeleteAsync(productId, imageId);
            return Results.Ok(ApiResponse.Ok("Image deleted successfully."));
        }).RequirePermission("Products:Edit");

        group.MapPut("/{imageId:int}/primary", async (int productId, int imageId, ProductImageService service) =>
        {
            await service.SetPrimaryAsync(productId, imageId);
            return Results.Ok(ApiResponse.Ok("Primary image updated successfully."));
        }).RequirePermission("Products:Edit");
    }
}
