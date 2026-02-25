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
    private readonly ICurrentUserService _currentUser;

    public SaleService(ISaleRepository repo, IInventoryRepository inventoryRepo, ICurrentUserService currentUser)
    {
        _repo = repo;
        _inventoryRepo = inventoryRepo;
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
            PaymentMethod = (PaymentMethod)request.PaymentMethod,
            Status = SaleStatus.Completed,
            Notes = request.Notes,
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

        return saleId;
    }

    public async Task CancelAsync(int id)
    {
        var sale = await _repo.GetByIdAsync(id) ?? throw new NotFoundException("Sale", id);
        if (sale.Status != nameof(SaleStatus.Completed) && sale.Status != nameof(SaleStatus.Pending))
            throw new AppException("Only completed or pending sales can be cancelled.");

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

        await _repo.UpdateStatusAsync(id, (int)SaleStatus.Cancelled);
    }
}
