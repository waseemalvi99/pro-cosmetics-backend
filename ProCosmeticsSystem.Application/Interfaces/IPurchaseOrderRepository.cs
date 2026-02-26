using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.DTOs.Purchases;
using ProCosmeticsSystem.Domain.Entities;

namespace ProCosmeticsSystem.Application.Interfaces;

public interface IPurchaseOrderRepository
{
    Task<PagedResult<PurchaseOrderDto>> GetAllAsync(int page, int pageSize, int? supplierId);
    Task<PurchaseOrderDto?> GetByIdAsync(int id);
    Task<int> CreateAsync(PurchaseOrder order);
    Task AddItemsAsync(int orderId, IEnumerable<PurchaseOrderItem> items);
    Task<List<PurchaseOrderItemDto>> GetItemsAsync(int orderId);
    Task UpdateStatusAsync(int id, int status);
    Task UpdateTotalAsync(int id, decimal total);
    Task UpdateItemReceivedQuantityAsync(int orderId, int productId, int quantityReceived);
}
