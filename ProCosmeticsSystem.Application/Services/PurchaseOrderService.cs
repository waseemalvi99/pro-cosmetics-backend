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
    private readonly IEmailNotificationService _emailNotification;

    public PurchaseOrderService(
        IPurchaseOrderRepository repo,
        ISupplierRepository supplierRepo,
        IInventoryRepository inventoryRepo,
        ILedgerRepository ledgerRepo,
        ICurrentUserService currentUser,
        IEmailNotificationService emailNotification)
    {
        _repo = repo;
        _supplierRepo = supplierRepo;
        _inventoryRepo = inventoryRepo;
        _ledgerRepo = ledgerRepo;
        _currentUser = currentUser;
        _emailNotification = emailNotification;
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

        // Validate quantities
        foreach (var item in request.Items)
        {
            if (item.Quantity <= 0)
                throw new ValidationException("Quantity", "Quantity must be greater than zero.");
            if (item.UnitPrice < 0)
                throw new ValidationException("UnitPrice", "Unit price cannot be negative.");
        }

        // Merge duplicate products (same ProductId) by summing quantities
        request.Items = request.Items
            .GroupBy(i => i.ProductId)
            .Select(g => new CreatePurchaseOrderItemRequest
            {
                ProductId = g.Key,
                Quantity = g.Sum(i => i.Quantity),
                UnitPrice = g.First().UnitPrice
            })
            .ToList();

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

        _emailNotification.NotifyPurchaseOrderCreated(orderNumber, supplier.Name, supplier.Email, totalAmount, request.Items.Count);

        return orderId;
    }

    public async Task SubmitAsync(int id)
    {
        var order = await _repo.GetByIdAsync(id) ?? throw new NotFoundException("PurchaseOrder", id);
        if (order.Status != nameof(PurchaseOrderStatus.Draft))
            throw new AppException("Only draft orders can be submitted.");

        await _repo.UpdateStatusAsync(id, (int)PurchaseOrderStatus.Submitted);

        var supplier = await _supplierRepo.GetByIdAsync(order.SupplierId);
        _emailNotification.NotifyPurchaseOrderSubmitted(order.OrderNumber, order.SupplierName, supplier?.Email, order.TotalAmount);
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

        // Recalculate and persist ReceivedAmount
        var receivedAmount = orderItems.Sum(i => i.QuantityReceived * i.UnitPrice);
        await _repo.UpdateReceivedAmountAsync(id, receivedAmount);

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

        _emailNotification.NotifyPurchaseOrderReceived(order.OrderNumber, order.SupplierName, receivedAmount, !allFullyReceived);
    }

    public async Task CancelAsync(int id)
    {
        var order = await _repo.GetByIdAsync(id) ?? throw new NotFoundException("PurchaseOrder", id);
        if (order.Status == nameof(PurchaseOrderStatus.PartiallyReceived))
            throw new AppException("Cannot cancel a partially received order. Use Close instead to finalize it.");
        if (order.Status == nameof(PurchaseOrderStatus.Received) || order.Status == nameof(PurchaseOrderStatus.Cancelled) || order.Status == nameof(PurchaseOrderStatus.Closed))
            throw new AppException("Cannot cancel a received, closed, or already cancelled order.");

        await _repo.UpdateStatusAsync(id, (int)PurchaseOrderStatus.Cancelled);

        var supplier = await _supplierRepo.GetByIdAsync(order.SupplierId);
        _emailNotification.NotifyPurchaseOrderCancelled(order.OrderNumber, order.SupplierName, supplier?.Email, null);
    }

    public async Task CloseAsync(int id, ClosePurchaseOrderRequest request)
    {
        var order = await _repo.GetByIdAsync(id) ?? throw new NotFoundException("PurchaseOrder", id);
        if (order.Status != nameof(PurchaseOrderStatus.PartiallyReceived))
            throw new AppException("Only partially received orders can be closed.");

        var orderItems = await _repo.GetItemsAsync(id);
        var receivedAmount = orderItems.Sum(i => i.QuantityReceived * i.UnitPrice);

        if (receivedAmount <= 0)
            throw new AppException("Cannot close a purchase order with no received items.");

        await _repo.UpdateReceivedAmountAsync(id, receivedAmount);

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
            Description = $"Purchase order {order.OrderNumber} closed (partial: {receivedAmount:N2} of {order.TotalAmount:N2})",
            DebitAmount = 0,
            CreditAmount = receivedAmount,
            CreatedBy = _currentUser.UserId
        };
        await _ledgerRepo.CreateAsync(ledgerEntry);

        await _repo.CloseAsync(id, request.Reason);

        var supplier = await _supplierRepo.GetByIdAsync(order.SupplierId);
        _emailNotification.NotifyPurchaseOrderClosed(order.OrderNumber, order.SupplierName, supplier?.Email, request.Reason);
    }
}
