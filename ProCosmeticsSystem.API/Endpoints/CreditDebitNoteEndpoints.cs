using ProCosmeticsSystem.API.Middlewares;
using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.DTOs.CreditDebitNotes;
using ProCosmeticsSystem.Application.Interfaces;
using ProCosmeticsSystem.Application.Services;

namespace ProCosmeticsSystem.API.Endpoints;

public static class CreditDebitNoteEndpoints
{
    public static void MapCreditDebitNoteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/credit-debit-notes").WithTags("CreditDebitNotes").RequireAuthorization();

        group.MapGet("/", async (int? page, int? pageSize, int? customerId, int? supplierId, CreditDebitNoteService service) =>
        {
            var result = await service.GetAllAsync(page ?? 1, pageSize ?? 20, customerId, supplierId);
            return Results.Ok(ApiResponse<PagedResult<CreditDebitNoteDto>>.Ok(result));
        }).RequirePermission("CreditNotes:View");

        group.MapGet("/{id:int}", async (int id, CreditDebitNoteService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return Results.Ok(ApiResponse<CreditDebitNoteDto>.Ok(result));
        }).RequirePermission("CreditNotes:View");

        group.MapPost("/", async (CreateCreditDebitNoteRequest request, CreditDebitNoteService service) =>
        {
            var id = await service.CreateAsync(request);
            return Results.Created($"/api/credit-debit-notes/{id}", ApiResponse<int>.Ok(id, "Note created."));
        }).RequirePermission("CreditNotes:Create");

        group.MapDelete("/{id:int}", async (int id, CreditDebitNoteService service) =>
        {
            await service.VoidAsync(id);
            return Results.Ok(ApiResponse.Ok("Note voided."));
        }).RequirePermission("CreditNotes:Delete");

        group.MapGet("/{id:int}/pdf", async (int id, CreditDebitNoteService service, IPdfService pdfService) =>
        {
            var note = await service.GetByIdAsync(id);
            var pdf = pdfService.GenerateCreditDebitNote(note);
            return Results.File(pdf, "application/pdf", $"note-{note.NoteNumber}.pdf");
        }).RequirePermission("CreditNotes:View");
    }
}
