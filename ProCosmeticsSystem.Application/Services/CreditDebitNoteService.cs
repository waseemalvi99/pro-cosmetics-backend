using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.DTOs.CreditDebitNotes;
using ProCosmeticsSystem.Application.Exceptions;
using ProCosmeticsSystem.Application.Interfaces;
using ProCosmeticsSystem.Domain.Entities;
using ProCosmeticsSystem.Domain.Enums;

namespace ProCosmeticsSystem.Application.Services;

public class CreditDebitNoteService
{
    private readonly ICreditDebitNoteRepository _repo;
    private readonly ILedgerRepository _ledgerRepo;
    private readonly ICustomerRepository _customerRepo;
    private readonly ISupplierRepository _supplierRepo;
    private readonly ICurrentUserService _currentUser;

    public CreditDebitNoteService(
        ICreditDebitNoteRepository repo,
        ILedgerRepository ledgerRepo,
        ICustomerRepository customerRepo,
        ISupplierRepository supplierRepo,
        ICurrentUserService currentUser)
    {
        _repo = repo;
        _ledgerRepo = ledgerRepo;
        _customerRepo = customerRepo;
        _supplierRepo = supplierRepo;
        _currentUser = currentUser;
    }

    public Task<PagedResult<CreditDebitNoteDto>> GetAllAsync(int page, int pageSize, int? customerId, int? supplierId)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize > 100 ? 100 : pageSize;
        return _repo.GetAllAsync(page, pageSize, customerId, supplierId);
    }

    public async Task<CreditDebitNoteDto> GetByIdAsync(int id)
    {
        return await _repo.GetByIdAsync(id) ?? throw new NotFoundException("CreditDebitNote", id);
    }

    public async Task<int> CreateAsync(CreateCreditDebitNoteRequest request)
    {
        if (request.Amount <= 0)
            throw new ValidationException("Amount", "Amount must be greater than zero.");
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ValidationException("Reason", "Reason is required.");

        var noteType = (NoteType)request.NoteType;
        var accountType = (NoteAccountType)request.AccountType;

        if (accountType == NoteAccountType.Customer)
        {
            if (!request.CustomerId.HasValue)
                throw new ValidationException("CustomerId", "Customer is required.");
            _ = await _customerRepo.GetByIdAsync(request.CustomerId.Value)
                ?? throw new NotFoundException("Customer", request.CustomerId.Value);
        }
        else
        {
            if (!request.SupplierId.HasValue)
                throw new ValidationException("SupplierId", "Supplier is required.");
            _ = await _supplierRepo.GetByIdAsync(request.SupplierId.Value)
                ?? throw new NotFoundException("Supplier", request.SupplierId.Value);
        }

        var prefix = noteType == NoteType.CreditNote ? "CN" : "DN";
        var noteNumber = $"{prefix}-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";

        var note = new CreditDebitNote
        {
            NoteNumber = noteNumber,
            NoteType = noteType,
            AccountType = accountType,
            CustomerId = request.CustomerId,
            SupplierId = request.SupplierId,
            NoteDate = DateTime.UtcNow,
            Amount = request.Amount,
            Reason = request.Reason,
            SaleId = request.SaleId,
            PurchaseOrderId = request.PurchaseOrderId,
            CreatedBy = _currentUser.UserId
        };

        var noteId = await _repo.CreateAsync(note);

        // Create ledger entry based on note type + account type combination
        var referenceType = noteType == NoteType.CreditNote ? "CreditNote" : "DebitNote";
        var ledgerEntry = CreateLedgerEntry(noteType, accountType, request, noteId, noteNumber, referenceType);
        await _ledgerRepo.CreateAsync(ledgerEntry);

        return noteId;
    }

    public async Task VoidAsync(int id)
    {
        var note = await _repo.GetByIdAsync(id) ?? throw new NotFoundException("CreditDebitNote", id);

        var referenceType = note.NoteType == "CreditNote" ? "CreditNote" : "DebitNote";
        var entries = await _ledgerRepo.GetByReferenceAsync(referenceType, id);

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
                ReferenceType = referenceType,
                ReferenceId = id,
                Description = $"Voided {note.NoteType} {note.NoteNumber}",
                DebitAmount = entry.CreditAmount,
                CreditAmount = entry.DebitAmount,
                CreatedBy = _currentUser.UserId
            };

            var reversalId = await _ledgerRepo.CreateAsync(reversal);
            await _ledgerRepo.MarkReversedAsync(entry.Id, reversalId);
        }

        await _repo.SoftDeleteAsync(id);
    }

    private LedgerEntry CreateLedgerEntry(
        NoteType noteType, NoteAccountType accountType,
        CreateCreditDebitNoteRequest request, int noteId, string noteNumber, string referenceType)
    {
        // Credit Note + Customer → Credit receivable (reduces balance)
        // Credit Note + Supplier → Debit payable (reduces what we owe)
        // Debit Note + Customer → Debit receivable (increases balance)
        // Debit Note + Supplier → Credit payable (increases what we owe)

        decimal debit = 0, credit = 0;
        LedgerAccountType ledgerAccountType;

        if (accountType == NoteAccountType.Customer)
        {
            ledgerAccountType = LedgerAccountType.CustomerReceivable;
            if (noteType == NoteType.CreditNote)
                credit = request.Amount;
            else
                debit = request.Amount;
        }
        else
        {
            ledgerAccountType = LedgerAccountType.SupplierPayable;
            if (noteType == NoteType.CreditNote)
                debit = request.Amount;
            else
                credit = request.Amount;
        }

        return new LedgerEntry
        {
            EntryDate = DateTime.UtcNow,
            AccountType = ledgerAccountType,
            CustomerId = request.CustomerId,
            SupplierId = request.SupplierId,
            ReferenceType = referenceType,
            ReferenceId = noteId,
            Description = $"{noteType} {noteNumber} - {request.Reason}",
            DebitAmount = debit,
            CreditAmount = credit,
            CreatedBy = _currentUser.UserId
        };
    }
}
