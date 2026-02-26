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
    private readonly ILedgerRepository _ledgerRepo;
    private readonly ICurrentUserService _currentUser;

    public PurchaseOrderService(
        IPurchaseOrderRepository repo,
        ISupplierRepository supplierRepo,
        IInventoryRepository inventoryRepo,
        ILedgerRepository ledgerRepo,
        ICurrentUserService currentUser)
    {
        _repo = repo;
        _supplierRepo = supplierRepo;
        _inventoryRepo = inventoryRepo;
        _ledgerRepo = ledgerRepo;
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

        var supplier = await _supplierRepo.GetByIdAsync(request.SupplierId)
            ?? throw new NotFoundException("Supplier", request.SupplierId);

        var orderNumber = $"PO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
        var totalAmount = request.Items.Sum(i => i.Quantity * i.UnitPrice);

        // Use request PaymentTermDays if provided, otherwise use supplier default
        var paymentTermDays = request.PaymentTermDays ?? supplier.PaymentTermDays;

        var order = new PurchaseOrder
        {
            SupplierId = request.SupplierId,
            OrderNumber = orderNumber,
            OrderDate = DateTime.UtcNow,
            ExpectedDeliveryDate = request.ExpectedDeliveryDate,
            Status = PurchaseOrderStatus.Draft,
            TotalAmount = totalAmount,
            Notes = request.Notes,
            PaymentTermDays = paymentTermDays,
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

        var orderItems = await _repo.GetItemsAsync(id);

        foreach (var received in request.Items)
        {
            if (received.QuantityReceived <= 0) continue;

            var orderItem = orderItems.FirstOrDefault(i => i.ProductId == received.ProductId)
                ?? throw new AppException($"Product {received.ProductId} is not part of this purchase order.");

            var newTotalReceived = orderItem.QuantityReceived + received.QuantityReceived;
            if (newTotalReceived > orderItem.Quantity)
                throw new AppException($"Cannot receive more than ordered for product {orderItem.ProductName}. Ordered: {orderItem.Quantity}, Already received: {orderItem.QuantityReceived}, Attempting: {received.QuantityReceived}.");

            await _repo.UpdateItemReceivedQuantityAsync(id, received.ProductId, newTotalReceived);
            await _inventoryRepo.UpdateQuantityAsync(received.ProductId, received.QuantityReceived);
            await _inventoryRepo.AddTransactionAsync(new InventoryTransaction
            {
                ProductId = received.ProductId,
                TransactionType = InventoryTransactionType.Purchase,
                Quantity = received.QuantityReceived,
                ReferenceType = "PurchaseOrder",
                ReferenceId = id,
                Notes = $"Received from PO {order.OrderNumber}"
            });

            orderItem.QuantityReceived = newTotalReceived;
        }

        var allFullyReceived = orderItems.All(i => i.QuantityReceived >= i.Quantity);
        var newStatus = allFullyReceived ? PurchaseOrderStatus.Received : PurchaseOrderStatus.PartiallyReceived;
        await _repo.UpdateStatusAsync(id, (int)newStatus);

        // When fully received, set DueDate and create payable ledger entry
        if (allFullyReceived)
        {
            var dueDate = order.PaymentTermDays > 0
                ? DateTime.UtcNow.AddDays(order.PaymentTermDays)
                : DateTime.UtcNow.AddDays(30);

            await _repo.UpdateDueDateAsync(id, dueDate);

            var ledgerEntry = new LedgerEntry
            {
                EntryDate = DateTime.UtcNow,
                AccountType = LedgerAccountType.SupplierPayable,
                SupplierId = order.SupplierId,
                ReferenceType = "PurchaseOrder",
                ReferenceId = id,
                Description = $"Purchase order {order.OrderNumber} received",
                DebitAmount = 0,
                CreditAmount = order.TotalAmount,
                CreatedBy = _currentUser.UserId
            };
            await _ledgerRepo.CreateAsync(ledgerEntry);
        }
    }

    public async Task CancelAsync(int id)
    {
        var order = await _repo.GetByIdAsync(id) ?? throw new NotFoundException("PurchaseOrder", id);
        if (order.Status == nameof(PurchaseOrderStatus.Received) || order.Status == nameof(PurchaseOrderStatus.Cancelled))
            throw new AppException("Cannot cancel a received or already cancelled order.");

        await _repo.UpdateStatusAsync(id, (int)PurchaseOrderStatus.Cancelled);
    }
}
