using Microsoft.Extensions.DependencyInjection;
using ProCosmeticsSystem.Application.Interfaces;
using ProCosmeticsSystem.Application.Services;

namespace ProCosmeticsSystem.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Auth
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IBarcodeService, BarcodeService>();

        // Business services
        services.AddScoped<CustomerService>();
        services.AddScoped<ProductService>();
        services.AddScoped<ProductImageService>();
        services.AddScoped<CategoryService>();
        services.AddScoped<InventoryService>();
        services.AddScoped<SupplierService>();
        services.AddScoped<PurchaseOrderService>();
        services.AddScoped<SalesmanService>();
        services.AddScoped<SaleService>();
        services.AddScoped<DeliveryService>();
        services.AddScoped<ReportService>();
        services.AddScoped<LedgerService>();
        services.AddScoped<PaymentService>();
        services.AddScoped<CreditDebitNoteService>();
        services.AddScoped<AccountStatementService>();
        services.AddScoped<ComboService>();

        // Notification
        services.AddScoped<INotificationService, NotificationService>();

        // Email
        services.AddScoped<EmailTemplateService>();
        services.AddScoped<IEmailNotificationService, EmailNotificationService>();

        return services;
    }
}
