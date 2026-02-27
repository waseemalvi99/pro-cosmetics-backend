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

        // Single image upload
        group.MapPost("/", async (int productId, IFormFile file, ProductImageService service) =>
        {
            using var stream = file.OpenReadStream();
            var result = await service.UploadAsync(productId, stream, file.FileName, file.ContentType, file.Length);
            return Results.Ok(ApiResponse<ProductImageDto>.Ok(result, "Image uploaded and optimized successfully."));
        }).RequirePermission("Products:Edit").DisableAntiforgery();

        // Multiple images upload
        group.MapPost("/bulk", async (int productId, HttpRequest request, ProductImageService service) =>
        {
            var form = await request.ReadFormAsync();
            var files = form.Files.GetFiles("files");

            if (files.Count == 0)
                return Results.BadRequest(ApiResponse.Fail("No files provided. Use form field name 'files'."));

            var fileList = files.Select(f => (
                stream: (Stream)f.OpenReadStream(),
                fileName: f.FileName,
                contentType: (string?)f.ContentType,
                size: f.Length
            )).ToList();

            try
            {
                var results = await service.UploadMultipleAsync(productId, fileList);
                return Results.Ok(ApiResponse<List<ProductImageDto>>.Ok(results, $"{results.Count} image(s) uploaded and optimized successfully."));
            }
            finally
            {
                foreach (var (stream, _, _, _) in fileList)
                    stream.Dispose();
            }
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
