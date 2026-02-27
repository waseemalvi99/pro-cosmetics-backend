using ProCosmeticsSystem.API.Middlewares;
using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.DTOs.Products;
using ProCosmeticsSystem.Application.Interfaces;
using ProCosmeticsSystem.Application.Services;

namespace ProCosmeticsSystem.API.Endpoints;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products").WithTags("Products").RequireAuthorization();

        group.MapGet("/", async (int? page, int? pageSize, string? search, int? categoryId, ProductService service) =>
        {
            var result = await service.GetAllAsync(page ?? 1, pageSize ?? 20, search, categoryId);
            return Results.Ok(ApiResponse<PagedResult<ProductDto>>.Ok(result));
        }).RequirePermission("Products:View");

        group.MapGet("/{id:int}", async (int id, ProductService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return Results.Ok(ApiResponse<ProductDto>.Ok(result));
        }).RequirePermission("Products:View");

        group.MapGet("/barcode/{code}", async (string code, ProductService service) =>
        {
            var result = await service.GetByBarcodeAsync(code);
            return Results.Ok(ApiResponse<BarcodeLookupResult>.Ok(result));
        }).RequirePermission("Products:View");

        group.MapGet("/{id:int}/barcode-image", async (int id, ProductService service, IBarcodeService barcodeService) =>
        {
            var product = await service.GetByIdAsync(id);
            var barcodeContent = product.Barcode ?? product.SKU ?? product.Id.ToString();
            var image = barcodeService.GenerateBarcodeImage(barcodeContent);
            return Results.File(image, "image/png", $"barcode-{barcodeContent}.png");
        }).RequirePermission("Products:View");

        group.MapPost("/barcode-labels", async (PrintBarcodesRequest request, ProductService service, IPdfService pdfService, IBarcodeService barcodeService) =>
        {
            var items = await service.GetByIdsForLabelsAsync(request.ProductIds);
            var pdf = pdfService.GenerateBarcodeLabelsPdf(items, barcodeService);
            return Results.File(pdf, "application/pdf", "barcode-labels.pdf");
        }).RequirePermission("Products:View");

        group.MapPost("/", async (CreateProductRequest request, ProductService service) =>
        {
            var id = await service.CreateAsync(request);
            return Results.Created($"/api/products/{id}", ApiResponse<int>.Ok(id, "Product created successfully."));
        }).RequirePermission("Products:Create");

        group.MapPut("/{id:int}", async (int id, UpdateProductRequest request, ProductService service) =>
        {
            await service.UpdateAsync(id, request);
            return Results.Ok(ApiResponse.Ok("Product updated successfully."));
        }).RequirePermission("Products:Edit");

        group.MapDelete("/{id:int}", async (int id, ProductService service) =>
        {
            await service.DeleteAsync(id);
            return Results.Ok(ApiResponse.Ok("Product deleted successfully."));
        }).RequirePermission("Products:Delete");
    }
}
