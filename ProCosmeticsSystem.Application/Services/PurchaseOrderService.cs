using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.DTOs.Purchases;
using ProCosmeticsSystem.Application.Exceptions;
using ProCosmeticsSystem.Application.Interfaces;
using ProCosmeticsSystem.Domain.Entities;
using ProCosmeticsSystem.Domain.Enums;

namespace ProCosmeticsSystem.Application.Services;

public class PurchaseOrderService
{
    private readonly IPurchaseOrderRepository _repo;
    private readonly ISupplierRepository _supplierRepo;
    private readonly IInventoryRepository _inventoryRepo;
    private readonly ICurrentUserService _currentUser;

    public PurchaseOrderService(
        IPurchaseOrderRepository repo,
        ISupplierRepository supplierRepo,
        IInventoryRepository inventoryRepo,
        ICurrentUserService currentUser)
    {
        _repo = repo;
        _supplierRepo = supplierRepo;
        _inventoryRepo = inventoryRepo;
        _currentUser = currentUser;
    }

    public Task<PagedResult<PurchaseOrderDto>> GetAllAsync(int page, int pageSize, int? supplierId)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize > 100 ? 100 : pageSize;
        return _repo.GetAllAsync(page, pageSize, supplierId);
    }

    public async Task<PurchaseOrderDto> GetByIdAsync(int id)
    {
        var order = await _repo.GetByIdAsync(id) ?? throw new NotFoundException("PurchaseOrder", id);
        order.Items = await _repo.GetItemsAsync(id);
        return order;
    }

    public async Task<int> CreateAsync(CreatePurchaseOrderRequest request)
    {
        if (request.Items.Count == 0)
            throw new ValidationException("Items", "At least one item is required.");

        _ = await _supplierRepo.GetByIdAsync(request.SupplierId)
            ?? throw new NotFoundException("Supplier", request.SupplierId);

        var orderNumber = $"PO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
        var totalAmount = request.Items.Sum(i => i.Quantity * i.UnitPrice);

        var order = new PurchaseOrder
        {
            SupplierId = request.SupplierId,
            OrderNumber = orderNumber,
            OrderDate = DateTime.UtcNow,
            ExpectedDeliveryDate = request.ExpectedDeliveryDate,
            Status = PurchaseOrderStatus.Draft,
            TotalAmount = totalAmount,
            Notes = request.Notes,
            CreatedBy = _currentUser.UserId
        };

        var orderId = await _repo.CreateAsync(order);

        var items = request.Items.Select(i => new PurchaseOrderItem
        {
            PurchaseOrderId = orderId,
            ProductId = i.ProductId,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            TotalPrice = i.Quantity * i.UnitPrice
        });

        await _repo.AddItemsAsync(orderId, items);
        return orderId;
    }

    public async Task SubmitAsync(int id)
    {
        var order = await _repo.GetByIdAsync(id) ?? throw new NotFoundException("PurchaseOrder", id);
        if (order.Status != nameof(PurchaseOrderStatus.Draft))
            throw new AppException("Only draft orders can be submitted.");

        await _repo.UpdateStatusAsync(id, (int)PurchaseOrderStatus.Submitted);
    }

    public async Task ReceiveAsync(int id, ReceivePurchaseOrderRequest request)
    {
        var order = await _repo.GetByIdAsync(id) ?? throw new NotFoundException("PurchaseOrder", id);
        if (order.Status != nameof(PurchaseOrderStatus.Submitted) && order.Status != nameof(PurchaseOrderStatus.PartiallyReceived))
            throw new AppException("Only submitted or partially received orders can be received.");

        foreach (var item in request.Items)
        {
            if (item.QuantityReceived <= 0) continue;

            await _inventoryRepo.UpdateQuantityAsync(item.ProductId, item.QuantityReceived);
            await _inventoryRepo.AddTransactionAsync(new InventoryTransaction
            {
                ProductId = item.ProductId,
                TransactionType = InventoryTransactionType.Purchase,
                Quantity = item.QuantityReceived,
                ReferenceType = "PurchaseOrder",
                ReferenceId = id,
                Notes = $"Received from PO {order.OrderNumber}"
            });
        }

        await _repo.UpdateStatusAsync(id, (int)PurchaseOrderStatus.Received);
    }

    public async Task CancelAsync(int id)
    {
        var order = await _repo.GetByIdAsync(id) ?? throw new NotFoundException("PurchaseOrder", id);
        if (order.Status == nameof(PurchaseOrderStatus.Received) || order.Status == nameof(PurchaseOrderStatus.Cancelled))
            throw new AppException("Cannot cancel a received or already cancelled order.");

        await _repo.UpdateStatusAsync(id, (int)PurchaseOrderStatus.Cancelled);
    }
}
