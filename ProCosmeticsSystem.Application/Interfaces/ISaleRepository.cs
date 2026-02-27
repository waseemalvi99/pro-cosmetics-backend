using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.DTOs.Sales;
using ProCosmeticsSystem.Domain.Entities;

namespace ProCosmeticsSystem.Application.Interfaces;

public interface ISaleRepository
{
    Task<PagedResult<SaleDto>> GetAllAsync(int page, int pageSize, int? customerId, int? salesmanId);
    Task<SaleDto?> GetByIdAsync(int id);
    Task<int> CreateAsync(Sale sale);
    Task AddItemsAsync(int saleId, IEnumerable<SaleItem> items);
    Task<List<SaleItemDto>> GetItemsAsync(int saleId);
    Task UpdateStatusAsync(int id, int status);
    Task UpdateDueDateAsync(int id, DateTime dueDate);
    Task UpdateItemReturnedQuantityAsync(int saleId, int productId, int quantityReturned);
    Task UpdateReturnedAmountAsync(int id, decimal returnedAmount);
}
