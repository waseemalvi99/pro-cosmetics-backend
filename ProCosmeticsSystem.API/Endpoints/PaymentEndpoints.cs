using ProCosmeticsSystem.API.Middlewares;
using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.DTOs.Payments;
using ProCosmeticsSystem.Application.Interfaces;
using ProCosmeticsSystem.Application.Services;

namespace ProCosmeticsSystem.API.Endpoints;

public static class PaymentEndpoints
{
    public static void MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/payments").WithTags("Payments").RequireAuthorization();

        group.MapGet("/", async (int? page, int? pageSize, int? customerId, int? supplierId, PaymentService service) =>
        {
            var result = await service.GetAllAsync(page ?? 1, pageSize ?? 20, customerId, supplierId);
            return Results.Ok(ApiResponse<PagedResult<PaymentDto>>.Ok(result));
        }).RequirePermission("Payments:View");

        group.MapGet("/{id:int}", async (int id, PaymentService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return Results.Ok(ApiResponse<PaymentDto>.Ok(result));
        }).RequirePermission("Payments:View");

        group.MapPost("/customer", async (CreateCustomerPaymentRequest request, PaymentService service) =>
        {
            var id = await service.CreateCustomerPaymentAsync(request);
            return Results.Created($"/api/payments/{id}", ApiResponse<int>.Ok(id, "Customer payment recorded."));
        }).RequirePermission("Payments:Create");

        group.MapPost("/supplier", async (CreateSupplierPaymentRequest request, PaymentService service) =>
        {
            var id = await service.CreateSupplierPaymentAsync(request);
            return Results.Created($"/api/payments/{id}", ApiResponse<int>.Ok(id, "Supplier payment recorded."));
        }).RequirePermission("Payments:Create");

        group.MapDelete("/{id:int}", async (int id, PaymentService service) =>
        {
            await service.VoidAsync(id);
            return Results.Ok(ApiResponse.Ok("Payment voided."));
        }).RequirePermission("Payments:Delete");

        group.MapGet("/{id:int}/pdf", async (int id, PaymentService service, IPdfService pdfService) =>
        {
            var payment = await service.GetByIdAsync(id);
            var pdf = pdfService.GeneratePaymentReceipt(payment);
            return Results.File(pdf, "application/pdf", $"receipt-{payment.ReceiptNumber}.pdf");
        }).RequirePermission("Payments:View");
    }
}
