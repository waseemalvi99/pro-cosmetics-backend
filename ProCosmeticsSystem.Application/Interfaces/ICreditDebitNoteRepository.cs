using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.DTOs.CreditDebitNotes;
using ProCosmeticsSystem.Domain.Entities;

namespace ProCosmeticsSystem.Application.Interfaces;

public interface ICreditDebitNoteRepository
{
    Task<PagedResult<CreditDebitNoteDto>> GetAllAsync(int page, int pageSize, int? customerId, int? supplierId);
    Task<CreditDebitNoteDto?> GetByIdAsync(int id);
    Task<int> CreateAsync(CreditDebitNote note);
    Task SoftDeleteAsync(int id);
}
