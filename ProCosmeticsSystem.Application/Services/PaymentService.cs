using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.DTOs.Payments;
using ProCosmeticsSystem.Application.Exceptions;
using ProCosmeticsSystem.Application.Interfaces;
using ProCosmeticsSystem.Domain.Entities;
using ProCosmeticsSystem.Domain.Enums;

namespace ProCosmeticsSystem.Application.Services;

public class PaymentService
{
    private readonly IPaymentRepository _repo;
    private readonly ILedgerRepository _ledgerRepo;
    private readonly ICustomerRepository _customerRepo;
    private readonly ISupplierRepository _supplierRepo;
    private readonly ICurrentUserService _currentUser;

    public PaymentService(
        IPaymentRepository repo,
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

    public Task<PagedResult<PaymentDto>> GetAllAsync(int page, int pageSize, int? customerId, int? supplierId)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize > 100 ? 100 : pageSize;
        return _repo.GetAllAsync(page, pageSize, customerId, supplierId);
    }

    public async Task<PaymentDto> GetByIdAsync(int id)
    {
        return await _repo.GetByIdAsync(id) ?? throw new NotFoundException("Payment", id);
    }

    public async Task<int> CreateCustomerPaymentAsync(CreateCustomerPaymentRequest request)
    {
        if (request.Amount <= 0)
            throw new ValidationException("Amount", "Amount must be greater than zero.");

        _ = await _customerRepo.GetByIdAsync(request.CustomerId)
            ?? throw new NotFoundException("Customer", request.CustomerId);

        var paymentMethod = (PaymentMethodLedger)request.PaymentMethod;
        ValidatePaymentMethodDetails(paymentMethod, request.ChequeNumber, request.BankName, request.ChequeDate, request.BankAccountReference);

        var receiptNumber = $"RCP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";

        var payment = new Payment
        {
            ReceiptNumber = receiptNumber,
            PaymentType = PaymentType.CustomerReceipt,
            CustomerId = request.CustomerId,
            PaymentDate = DateTime.UtcNow,
            Amount = request.Amount,
            PaymentMethod = paymentMethod,
            ChequeNumber = request.ChequeNumber,
            BankName = request.BankName,
            ChequeDate = request.ChequeDate,
            BankAccountReference = request.BankAccountReference,
            Notes = request.Notes,
            CreatedBy = _currentUser.UserId
        };

        var paymentId = await _repo.CreateAsync(payment);

        // Create ledger entry: Customer payment reduces receivable (credit)
        var ledgerEntry = new LedgerEntry
        {
            EntryDate = DateTime.UtcNow,
            AccountType = LedgerAccountType.CustomerReceivable,
            CustomerId = request.CustomerId,
            ReferenceType = "Payment",
            ReferenceId = paymentId,
            Description = $"Payment received {receiptNumber}",
            DebitAmount = 0,
            CreditAmount = request.Amount,
            CreatedBy = _currentUser.UserId
        };
        await _ledgerRepo.CreateAsync(ledgerEntry);

        return paymentId;
    }

    public async Task<int> CreateSupplierPaymentAsync(CreateSupplierPaymentRequest request)
    {
        if (request.Amount <= 0)
            throw new ValidationException("Amount", "Amount must be greater than zero.");

        _ = await _supplierRepo.GetByIdAsync(request.SupplierId)
            ?? throw new NotFoundException("Supplier", request.SupplierId);

        var paymentMethod = (PaymentMethodLedger)request.PaymentMethod;
        ValidatePaymentMethodDetails(paymentMethod, request.ChequeNumber, request.BankName, request.ChequeDate, request.BankAccountReference);

        var receiptNumber = $"PAY-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";

        var payment = new Payment
        {
            ReceiptNumber = receiptNumber,
            PaymentType = PaymentType.SupplierPayment,
            SupplierId = request.SupplierId,
            PaymentDate = DateTime.UtcNow,
            Amount = request.Amount,
            PaymentMethod = paymentMethod,
            ChequeNumber = request.ChequeNumber,
            BankName = request.BankName,
            ChequeDate = request.ChequeDate,
            BankAccountReference = request.BankAccountReference,
            Notes = request.Notes,
            CreatedBy = _currentUser.UserId
        };

        var paymentId = await _repo.CreateAsync(payment);

        // Create ledger entry: Supplier payment reduces payable (debit)
        var ledgerEntry = new LedgerEntry
        {
            EntryDate = DateTime.UtcNow,
            AccountType = LedgerAccountType.SupplierPayable,
            SupplierId = request.SupplierId,
            ReferenceType = "Payment",
            ReferenceId = paymentId,
            Description = $"Payment made {receiptNumber}",
            DebitAmount = request.Amount,
            CreditAmount = 0,
            CreatedBy = _currentUser.UserId
        };
        await _ledgerRepo.CreateAsync(ledgerEntry);

        return paymentId;
    }

    public async Task VoidAsync(int id)
    {
        var payment = await _repo.GetByIdAsync(id) ?? throw new NotFoundException("Payment", id);

        // Reverse ledger entries for this payment
        var entries = await _ledgerRepo.GetByReferenceAsync("Payment", id);
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
                ReferenceType = "Payment",
                ReferenceId = id,
                Description = $"Voided payment {payment.ReceiptNumber}",
                DebitAmount = entry.CreditAmount,
                CreditAmount = entry.DebitAmount,
                CreatedBy = _currentUser.UserId
            };

            var reversalId = await _ledgerRepo.CreateAsync(reversal);
            await _ledgerRepo.MarkReversedAsync(entry.Id, reversalId);
        }

        await _repo.SoftDeleteAsync(id);
    }

    private static void ValidatePaymentMethodDetails(PaymentMethodLedger method, string? chequeNumber, string? bankName, DateTime? chequeDate, string? bankRef)
    {
        if (method == PaymentMethodLedger.Cheque)
        {
            if (string.IsNullOrWhiteSpace(chequeNumber))
                throw new ValidationException("ChequeNumber", "Cheque number is required for cheque payments.");
            if (string.IsNullOrWhiteSpace(bankName))
                throw new ValidationException("BankName", "Bank name is required for cheque payments.");
            if (!chequeDate.HasValue)
                throw new ValidationException("ChequeDate", "Cheque date is required for cheque payments.");
        }
        else if (method == PaymentMethodLedger.BankTransfer)
        {
            if (string.IsNullOrWhiteSpace(bankRef))
                throw new ValidationException("BankAccountReference", "Bank account reference is required for bank transfers.");
        }
    }
}
