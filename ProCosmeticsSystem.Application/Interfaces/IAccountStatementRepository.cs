using ProCosmeticsSystem.Application.DTOs.Accounts;

namespace ProCosmeticsSystem.Application.Interfaces;

public interface IAccountStatementRepository
{
    Task<AccountStatementDto> GetCustomerStatementAsync(int customerId, DateTime fromDate, DateTime toDate);
    Task<AccountStatementDto> GetSupplierStatementAsync(int supplierId, DateTime fromDate, DateTime toDate);
    Task<AgingReportDto> GetReceivablesAgingAsync();
    Task<AgingReportDto> GetPayablesAgingAsync();
}
