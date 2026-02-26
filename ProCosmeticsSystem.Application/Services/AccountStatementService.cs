using ProCosmeticsSystem.Application.DTOs.Accounts;
using ProCosmeticsSystem.Application.Exceptions;
using ProCosmeticsSystem.Application.Interfaces;

namespace ProCosmeticsSystem.Application.Services;

public class AccountStatementService
{
    private readonly IAccountStatementRepository _repo;
    private readonly ICustomerRepository _customerRepo;
    private readonly ISupplierRepository _supplierRepo;
    private readonly IPdfService _pdfService;

    public AccountStatementService(
        IAccountStatementRepository repo,
        ICustomerRepository customerRepo,
        ISupplierRepository supplierRepo,
        IPdfService pdfService)
    {
        _repo = repo;
        _customerRepo = customerRepo;
        _supplierRepo = supplierRepo;
        _pdfService = pdfService;
    }

    public async Task<AccountStatementDto> GetCustomerStatementAsync(int customerId, DateTime? fromDate, DateTime? toDate)
    {
        _ = await _customerRepo.GetByIdAsync(customerId)
            ?? throw new NotFoundException("Customer", customerId);

        var from = fromDate ?? DateTime.UtcNow.AddMonths(-3);
        var to = toDate ?? DateTime.UtcNow;

        return await _repo.GetCustomerStatementAsync(customerId, from, to);
    }

    public async Task<AccountStatementDto> GetSupplierStatementAsync(int supplierId, DateTime? fromDate, DateTime? toDate)
    {
        _ = await _supplierRepo.GetByIdAsync(supplierId)
            ?? throw new NotFoundException("Supplier", supplierId);

        var from = fromDate ?? DateTime.UtcNow.AddMonths(-3);
        var to = toDate ?? DateTime.UtcNow;

        return await _repo.GetSupplierStatementAsync(supplierId, from, to);
    }

    public async Task<byte[]> GetCustomerStatementPdfAsync(int customerId, DateTime? fromDate, DateTime? toDate)
    {
        var statement = await GetCustomerStatementAsync(customerId, fromDate, toDate);
        return _pdfService.GenerateAccountStatement(statement);
    }

    public async Task<byte[]> GetSupplierStatementPdfAsync(int supplierId, DateTime? fromDate, DateTime? toDate)
    {
        var statement = await GetSupplierStatementAsync(supplierId, fromDate, toDate);
        return _pdfService.GenerateAccountStatement(statement);
    }

    public Task<AgingReportDto> GetReceivablesAgingAsync()
    {
        return _repo.GetReceivablesAgingAsync();
    }

    public Task<AgingReportDto> GetPayablesAgingAsync()
    {
        return _repo.GetPayablesAgingAsync();
    }
}
