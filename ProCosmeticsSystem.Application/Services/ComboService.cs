using ProCosmeticsSystem.Application.DTOs.Combos;
using ProCosmeticsSystem.Application.Interfaces;

namespace ProCosmeticsSystem.Application.Services;

public class ComboService
{
    private readonly IComboRepository _repo;

    public ComboService(IComboRepository repo)
    {
        _repo = repo;
    }

    public Task<List<CustomerComboDto>> SearchCustomersAsync(string? search, int limit = 20)
        => _repo.SearchCustomersAsync(search, ClampLimit(limit));

    public Task<List<SupplierComboDto>> SearchSuppliersAsync(string? search, int limit = 20)
        => _repo.SearchSuppliersAsync(search, ClampLimit(limit));

    public Task<List<ProductComboDto>> SearchProductsAsync(string? search, int limit = 20)
        => _repo.SearchProductsAsync(search, ClampLimit(limit));

    public Task<List<ComboItemDto>> SearchSalesmenAsync(string? search, int limit = 20)
        => _repo.SearchSalesmenAsync(search, ClampLimit(limit));

    public Task<List<ComboItemDto>> SearchCategoriesAsync(string? search, int limit = 20)
        => _repo.SearchCategoriesAsync(search, ClampLimit(limit));

    public Task<List<DeliveryManComboDto>> SearchDeliveryMenAsync(string? search, int limit = 20)
        => _repo.SearchDeliveryMenAsync(search, ClampLimit(limit));

    public Task<List<ComboItemDto>> SearchUsersAsync(string? search, int limit = 20)
        => _repo.SearchUsersAsync(search, ClampLimit(limit));

    public Task<List<ComboItemDto>> SearchRolesAsync(string? search, int limit = 20)
        => _repo.SearchRolesAsync(search, ClampLimit(limit));

    public Task<List<SaleComboDto>> SearchSalesAsync(int customerId, string? search, int limit = 20)
        => _repo.SearchSalesAsync(customerId, search, ClampLimit(limit));

    public Task<List<PurchaseOrderComboDto>> SearchPurchaseOrdersAsync(int supplierId, string? search, int limit = 20)
        => _repo.SearchPurchaseOrdersAsync(supplierId, search, ClampLimit(limit));

    private static int ClampLimit(int limit) => limit < 1 ? 20 : limit > 50 ? 50 : limit;
}
