using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.DTOs.Ledger;
using ProCosmeticsSystem.Domain.Entities;

namespace ProCosmeticsSystem.Application.Interfaces;

public interface ILedgerRepository
{
    Task<PagedResult<LedgerEntryDto>> GetAllAsync(int page, int pageSize, int? customerId, int? supplierId);
    Task<LedgerEntryDto?> GetByIdAsync(int id);
    Task<int> CreateAsync(LedgerEntry entry);
    Task<decimal> GetBalanceAsync(int? customerId, int? supplierId);
    Task<List<LedgerEntryDto>> GetByReferenceAsync(string referenceType, int referenceId);
    Task MarkReversedAsync(int entryId, int reversedByEntryId);
}
