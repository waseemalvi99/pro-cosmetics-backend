using ProCosmeticsSystem.Application.DTOs.Combos;

namespace ProCosmeticsSystem.Application.Interfaces;

public interface IComboRepository
{
    Task<List<CustomerComboDto>> SearchCustomersAsync(string? search, int limit);
    Task<List<SupplierComboDto>> SearchSuppliersAsync(string? search, int limit);
    Task<List<ProductComboDto>> SearchProductsAsync(string? search, int limit);
    Task<List<ComboItemDto>> SearchSalesmenAsync(string? search, int limit);
    Task<List<ComboItemDto>> SearchCategoriesAsync(string? search, int limit);
    Task<List<DeliveryManComboDto>> SearchDeliveryMenAsync(string? search, int limit);
    Task<List<ComboItemDto>> SearchUsersAsync(string? search, int limit);
    Task<List<ComboItemDto>> SearchRolesAsync(string? search, int limit);
    Task<List<SaleComboDto>> SearchSalesAsync(int customerId, string? search, int limit);
    Task<List<PurchaseOrderComboDto>> SearchPurchaseOrdersAsync(int supplierId, string? search, int limit);
}
