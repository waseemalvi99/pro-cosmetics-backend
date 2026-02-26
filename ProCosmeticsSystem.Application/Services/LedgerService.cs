using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.DTOs.Ledger;
using ProCosmeticsSystem.Application.Exceptions;
using ProCosmeticsSystem.Application.Interfaces;
using ProCosmeticsSystem.Domain.Entities;
using ProCosmeticsSystem.Domain.Enums;

namespace ProCosmeticsSystem.Application.Services;

public class LedgerService
{
    private readonly ILedgerRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public LedgerService(ILedgerRepository repo, ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public Task<PagedResult<LedgerEntryDto>> GetAllAsync(int page, int pageSize, int? customerId, int? supplierId)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize > 100 ? 100 : pageSize;
        return _repo.GetAllAsync(page, pageSize, customerId, supplierId);
    }

    public async Task<LedgerEntryDto> GetByIdAsync(int id)
    {
        return await _repo.GetByIdAsync(id) ?? throw new NotFoundException("LedgerEntry", id);
    }

    public async Task<int> CreateManualEntryAsync(CreateManualLedgerEntryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Description))
            throw new ValidationException("Description", "Description is required.");
        if (request.DebitAmount < 0 || request.CreditAmount < 0)
            throw new ValidationException("Amount", "Amounts cannot be negative.");
        if (request.DebitAmount == 0 && request.CreditAmount == 0)
            throw new ValidationException("Amount", "Either debit or credit amount must be provided.");

        var accountType = (LedgerAccountType)request.AccountType;
        if (accountType == LedgerAccountType.CustomerReceivable && !request.CustomerId.HasValue)
            throw new ValidationException("CustomerId", "Customer is required for receivable entries.");
        if (accountType == LedgerAccountType.SupplierPayable && !request.SupplierId.HasValue)
            throw new ValidationException("SupplierId", "Supplier is required for payable entries.");

        var entry = new LedgerEntry
        {
            EntryDate = DateTime.UtcNow,
            AccountType = accountType,
            CustomerId = request.CustomerId,
            SupplierId = request.SupplierId,
            ReferenceType = "Manual",
            ReferenceId = 0,
            Description = request.Description,
            DebitAmount = request.DebitAmount,
            CreditAmount = request.CreditAmount,
            CreatedBy = _currentUser.UserId
        };

        return await _repo.CreateAsync(entry);
    }

    public async Task<int> CreateSaleEntryAsync(int saleId, string saleNumber, int customerId, decimal amount, int? userId)
    {
        var entry = new LedgerEntry
        {
            EntryDate = DateTime.UtcNow,
            AccountType = LedgerAccountType.CustomerReceivable,
            CustomerId = customerId,
            ReferenceType = "Sale",
            ReferenceId = saleId,
            Description = $"Credit sale {saleNumber}",
            DebitAmount = amount,
            CreditAmount = 0,
            CreatedBy = userId
        };

        return await _repo.CreateAsync(entry);
    }

    public async Task ReverseSaleEntriesAsync(int saleId, string saleNumber, int? userId)
    {
        var entries = await _repo.GetByReferenceAsync("Sale", saleId);
        foreach (var entry in entries)
        {
            if (entry.IsReversed) continue;

            var reversal = new LedgerEntry
            {
                EntryDate = DateTime.UtcNow,
                AccountType = entry.AccountType == "CustomerReceivable"
                    ? LedgerAccountType.CustomerReceivable
                    : LedgerAccountType.SupplierPayable,
                CustomerId = entry.CustomerId,
                SupplierId = entry.SupplierId,
                ReferenceType = "Sale",
                ReferenceId = saleId,
                Description = $"Reversal - cancelled sale {saleNumber}",
                DebitAmount = entry.CreditAmount,
                CreditAmount = entry.DebitAmount,
                CreatedBy = userId
            };

            var reversalId = await _repo.CreateAsync(reversal);
            await _repo.MarkReversedAsync(entry.Id, reversalId);
        }
    }

    public async Task<int> CreatePurchaseOrderEntryAsync(int poId, string orderNumber, int supplierId, decimal amount, int? userId)
    {
        var entry = new LedgerEntry
        {
            EntryDate = DateTime.UtcNow,
            AccountType = LedgerAccountType.SupplierPayable,
            SupplierId = supplierId,
            ReferenceType = "PurchaseOrder",
            ReferenceId = poId,
            Description = $"Purchase order {orderNumber} received",
            DebitAmount = 0,
            CreditAmount = amount,
            CreatedBy = userId
        };

        return await _repo.CreateAsync(entry);
    }
}
