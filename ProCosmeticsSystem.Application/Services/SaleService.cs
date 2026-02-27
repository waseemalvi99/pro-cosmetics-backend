using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.DTOs.Sales;
using ProCosmeticsSystem.Application.Exceptions;
using ProCosmeticsSystem.Application.Interfaces;
using ProCosmeticsSystem.Domain.Entities;
using ProCosmeticsSystem.Domain.Enums;

namespace ProCosmeticsSystem.Application.Services;

public class SaleService
{
    private readonly ISaleRepository _repo;
    private readonly IInventoryRepository _inventoryRepo;
    private readonly ICustomerRepository _customerRepo;
    private readonly ILedgerRepository _ledgerRepo;
    private readonly ICreditDebitNoteRepository _creditDebitNoteRepo;
    private readonly ICurrentUserService _currentUser;

    public SaleService(
        ISaleRepository repo,
        IInventoryRepository inventoryRepo,
        ICustomerRepository customerRepo,
        ILedgerRepository ledgerRepo,
        ICreditDebitNoteRepository creditDebitNoteRepo,
        ICurrentUserService currentUser)
    {
        _repo = repo;
        _inventoryRepo = inventoryRepo;
        _customerRepo = customerRepo;
        _ledgerRepo = ledgerRepo;
        _creditDebitNoteRepo = creditDebitNoteRepo;
        _currentUser = currentUser;
    }

    public Task<PagedResult<SaleDto>> GetAllAsync(int page, int pageSize, int? customerId, int? salesmanId)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize > 100 ? 100 : pageSize;
        return _repo.GetAllAsync(page, pageSize, customerId, salesmanId);
    }

    public async Task<SaleDto> GetByIdAsync(int id)
    {
        var sale = await _repo.GetByIdAsync(id) ?? throw new NotFoundException("Sale", id);
        sale.Items = await _repo.GetItemsAsync(id);
        return sale;
    }

    public async Task<int> CreateAsync(CreateSaleRequest request)
    {
        if (request.Items.Count == 0)
            throw new ValidationException("Items", "At least one item is required.");

        // Validate quantities
        foreach (var item in request.Items)
        {
            if (item.Quantity <= 0)
                throw new ValidationException("Quantity", "Quantity must be greater than zero.");
            if (item.UnitPrice < 0)
                throw new ValidationException("UnitPrice", "Unit price cannot be negative.");
            if (item.Discount < 0)
                throw new ValidationException("Discount", "Discount cannot be negative.");
        }

        // Merge duplicate products (same ProductId) by summing quantities and discounts
        request.Items = request.Items
            .GroupBy(i => i.ProductId)
            .Select(g => new CreateSaleItemRequest
            {
                ProductId = g.Key,
                Quantity = g.Sum(i => i.Quantity),
                UnitPrice = g.First().UnitPrice,
                Discount = g.Sum(i => i.Discount)
            })
            .ToList();

        // Check inventory availability
        foreach (var item in request.Items)
        {
            var inv = await _inventoryRepo.GetByProductIdAsync(item.ProductId)
                ?? throw new NotFoundException("Product inventory", item.ProductId);

            if (inv.QuantityOnHand < item.Quantity)
                throw new ValidationException("Quantity", $"Insufficient stock for product '{inv.ProductName}'. Available: {inv.QuantityOnHand}");
        }

        var subTotal = request.Items.Sum(i => (i.UnitPrice * i.Quantity) - i.Discount);
        var totalAmount = subTotal - request.Discount + request.Tax;
        var saleNumber = $"SL-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";

        // Check credit limit if credit sale with customer
        var paymentMethod = (PaymentMethod)request.PaymentMethod;
        DateTime? dueDate = null;

        if (paymentMethod == PaymentMethod.Credit && request.CustomerId.HasValue)
        {
            var customer = await _customerRepo.GetByIdAsync(request.CustomerId.Value)
                ?? throw new NotFoundException("Customer", request.CustomerId.Value);

            if (customer.CreditLimit > 0)
            {
                var currentBalance = await _ledgerRepo.GetBalanceAsync(request.CustomerId.Value, null);
                if (currentBalance + totalAmount > customer.CreditLimit)
                    throw new ValidationException("CreditLimit",
                        $"Credit limit exceeded. Limit: {customer.CreditLimit:N2}, Current balance: {currentBalance:N2}, Sale amount: {totalAmount:N2}");
            }

            dueDate = customer.CreditDays > 0
                ? DateTime.UtcNow.AddDays(customer.CreditDays)
                : DateTime.UtcNow.AddDays(30);
        }

        var sale = new Sale
        {
            SaleNumber = saleNumber,
            CustomerId = request.CustomerId,
            SalesmanId = request.SalesmanId,
            SaleDate = DateTime.UtcNow,
            SubTotal = subTotal,
            Discount = request.Discount,
            Tax = request.Tax,
            TotalAmount = totalAmount,
            PaymentMethod = paymentMethod,
            Status = SaleStatus.Completed,
            Notes = request.Notes,
            DueDate = dueDate,
            CreatedBy = _currentUser.UserId
        };

        var saleId = await _repo.CreateAsync(sale);

        var items = request.Items.Select(i => new SaleItem
        {
            SaleId = saleId,
            ProductId = i.ProductId,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            Discount = i.Discount,
            TotalPrice = (i.UnitPrice * i.Quantity) - i.Discount
        });

        await _repo.AddItemsAsync(saleId, items);

        // Deduct inventory
        foreach (var item in request.Items)
        {
            await _inventoryRepo.UpdateQuantityAsync(item.ProductId, -item.Quantity);
            await _inventoryRepo.AddTransactionAsync(new InventoryTransaction
            {
                ProductId = item.ProductId,
                TransactionType = InventoryTransactionType.Sale,
                Quantity = -item.Quantity,
                ReferenceType = "Sale",
                ReferenceId = saleId,
                Notes = $"Sale {saleNumber}"
            });
        }

        // Create ledger entry for credit sales
        if (paymentMethod == PaymentMethod.Credit && request.CustomerId.HasValue)
        {
            var ledgerEntry = new LedgerEntry
            {
                EntryDate = DateTime.UtcNow,
                AccountType = LedgerAccountType.CustomerReceivable,
                CustomerId = request.CustomerId.Value,
                ReferenceType = "Sale",
                ReferenceId = saleId,
                Description = $"Credit sale {saleNumber}",
                DebitAmount = totalAmount,
                CreditAmount = 0,
                CreatedBy = _currentUser.UserId
            };
            await _ledgerRepo.CreateAsync(ledgerEntry);
        }

        return saleId;
    }

    public async Task ReturnAsync(int id, ReturnSaleRequest request)
    {
        var sale = await _repo.GetByIdAsync(id) ?? throw new NotFoundException("Sale", id);
        if (sale.Status != nameof(SaleStatus.Completed))
            throw new AppException("Only completed sales can have returns.");

        if (request.Items.Count == 0)
            throw new ValidationException("Items", "At least one return item is required.");

        var saleItems = await _repo.GetItemsAsync(id);
        var returnAmount = 0m;

        foreach (var returnItem in request.Items)
        {
            var saleItem = saleItems.FirstOrDefault(si => si.ProductId == returnItem.ProductId)
                ?? throw new NotFoundException("Sale item for product", returnItem.ProductId);

            if (returnItem.QuantityReturned <= 0)
                throw new ValidationException("QuantityReturned", "Return quantity must be greater than zero.");

            if (saleItem.QuantityReturned + returnItem.QuantityReturned > saleItem.Quantity)
                throw new ValidationException("QuantityReturned",
                    $"Cannot return {returnItem.QuantityReturned} of '{saleItem.ProductName}'. Already returned: {saleItem.QuantityReturned}, sold: {saleItem.Quantity}.");

            var newReturnedQty = saleItem.QuantityReturned + returnItem.QuantityReturned;
            await _repo.UpdateItemReturnedQuantityAsync(id, returnItem.ProductId, newReturnedQty);

            // Restore inventory
            await _inventoryRepo.UpdateQuantityAsync(returnItem.ProductId, returnItem.QuantityReturned);
            await _inventoryRepo.AddTransactionAsync(new InventoryTransaction
            {
                ProductId = returnItem.ProductId,
                TransactionType = InventoryTransactionType.Return,
                Quantity = returnItem.QuantityReturned,
                ReferenceType = "SaleReturn",
                ReferenceId = id,
                Notes = $"Return from sale {sale.SaleNumber}"
            });

            // Calculate item return value (proportional discount)
            var itemReturnValue = (saleItem.UnitPrice * returnItem.QuantityReturned)
                - (saleItem.Discount * returnItem.QuantityReturned / saleItem.Quantity);
            returnAmount += itemReturnValue;

            // Update local copy for full-return check
            saleItem.QuantityReturned = newReturnedQty;
        }

        var newTotalReturned = sale.ReturnedAmount + returnAmount;
        await _repo.UpdateReturnedAmountAsync(id, newTotalReturned);

        // Reverse ledger for credit sales
        if (sale.PaymentMethod == nameof(PaymentMethod.Credit) && sale.CustomerId.HasValue)
        {
            await _ledgerRepo.CreateAsync(new LedgerEntry
            {
                EntryDate = DateTime.UtcNow,
                AccountType = LedgerAccountType.CustomerReceivable,
                CustomerId = sale.CustomerId.Value,
                ReferenceType = "SaleReturn",
                ReferenceId = id,
                Description = $"Return from sale {sale.SaleNumber} ({returnAmount:N2} returned)",
                DebitAmount = 0,
                CreditAmount = returnAmount,
                CreatedBy = _currentUser.UserId
            });
        }

        // Auto credit note for sales with a customer
        if (sale.CustomerId.HasValue)
        {
            var noteNumber = $"CN-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
            await _creditDebitNoteRepo.CreateAsync(new CreditDebitNote
            {
                NoteNumber = noteNumber,
                NoteType = NoteType.CreditNote,
                AccountType = NoteAccountType.Customer,
                CustomerId = sale.CustomerId.Value,
                NoteDate = DateTime.UtcNow,
                Amount = returnAmount,
                Reason = request.Reason ?? "Sales return",
                SaleId = id,
                CreatedBy = _currentUser.UserId
            });
        }

        // Check if fully returned
        var allFullyReturned = saleItems.All(si => si.QuantityReturned >= si.Quantity);
        if (allFullyReturned)
            await _repo.UpdateStatusAsync(id, (int)SaleStatus.Refunded);
    }

    public async Task CancelAsync(int id)
    {
        var sale = await _repo.GetByIdAsync(id) ?? throw new NotFoundException("Sale", id);
        if (sale.Status != nameof(SaleStatus.Completed) && sale.Status != nameof(SaleStatus.Pending))
            throw new AppException("Only completed or pending sales can be cancelled.");

        if (sale.ReturnedAmount > 0)
            throw new AppException("Cannot cancel a sale with returned items.");

        var items = await _repo.GetItemsAsync(id);

        // Restore inventory
        foreach (var item in items)
        {
            await _inventoryRepo.UpdateQuantityAsync(item.ProductId, item.Quantity);
            await _inventoryRepo.AddTransactionAsync(new InventoryTransaction
            {
                ProductId = item.ProductId,
                TransactionType = InventoryTransactionType.Return,
                Quantity = item.Quantity,
                ReferenceType = "SaleCancellation",
                ReferenceId = id,
                Notes = $"Cancelled sale {sale.SaleNumber}"
            });
        }

        // Reverse ledger entries if this was a credit sale
        var ledgerEntries = await _ledgerRepo.GetByReferenceAsync("Sale", id);
        foreach (var entry in ledgerEntries)
        {
            if (entry.IsReversed) continue;

            var reversal = new LedgerEntry
            {
                EntryDate = DateTime.UtcNow,
                AccountType = LedgerAccountType.CustomerReceivable,
                CustomerId = entry.CustomerId,
                ReferenceType = "Sale",
                ReferenceId = id,
                Description = $"Reversal - cancelled sale {sale.SaleNumber}",
                DebitAmount = entry.CreditAmount,
                CreditAmount = entry.DebitAmount,
                CreatedBy = _currentUser.UserId
            };

            var reversalId = await _ledgerRepo.CreateAsync(reversal);
            await _ledgerRepo.MarkReversedAsync(entry.Id, reversalId);
        }

        await _repo.UpdateStatusAsync(id, (int)SaleStatus.Cancelled);
    }
}
