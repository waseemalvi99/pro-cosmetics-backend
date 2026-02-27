using Microsoft.Extensions.Logging;
using ProCosmeticsSystem.Application.Interfaces;

namespace ProCosmeticsSystem.Application.Services;

public class EmailNotificationService : IEmailNotificationService
{
    private readonly IEmailService _emailService;
    private readonly IUserRepository _userRepo;
    private readonly EmailTemplateService _templateService;
    private readonly ILogger<EmailNotificationService> _logger;

    public EmailNotificationService(
        IEmailService emailService,
        IUserRepository userRepo,
        EmailTemplateService templateService,
        ILogger<EmailNotificationService> logger)
    {
        _emailService = emailService;
        _userRepo = userRepo;
        _templateService = templateService;
        _logger = logger;
    }

    // ── Purchase Orders ─────────────────────────────────────────────────

    public void NotifyPurchaseOrderCreated(string orderNumber, string supplierName, string? supplierEmail, decimal total, int itemCount)
    {
        var html = _templateService.PurchaseOrderCreated(orderNumber, supplierName, total, itemCount);
        FireAndForget($"Purchase Order {orderNumber} Created", html);
    }

    public void NotifyPurchaseOrderSubmitted(string orderNumber, string supplierName, string? supplierEmail, decimal total)
    {
        var html = _templateService.PurchaseOrderSubmitted(orderNumber, supplierName, total);
        FireAndForget($"Purchase Order {orderNumber} Submitted", html, supplierEmail: supplierEmail);
    }

    public void NotifyPurchaseOrderReceived(string orderNumber, string supplierName, decimal receivedAmount, bool isPartial)
    {
        var html = _templateService.PurchaseOrderReceived(orderNumber, supplierName, receivedAmount, isPartial);
        FireAndForget($"Purchase Order {orderNumber} {(isPartial ? "Partially " : "")}Received", html);
    }

    public void NotifyPurchaseOrderClosed(string orderNumber, string supplierName, string? supplierEmail, string? reason)
    {
        var html = _templateService.PurchaseOrderClosed(orderNumber, supplierName, reason);
        FireAndForget($"Purchase Order {orderNumber} Closed", html, supplierEmail: supplierEmail);
    }

    public void NotifyPurchaseOrderCancelled(string orderNumber, string supplierName, string? supplierEmail, string? reason)
    {
        var html = _templateService.PurchaseOrderCancelled(orderNumber, supplierName, reason);
        FireAndForget($"Purchase Order {orderNumber} Cancelled", html, supplierEmail: supplierEmail);
    }

    // ── Sales ────────────────────────────────────────────────────────────

    public void NotifySaleCreated(string saleNumber, string? customerName, string? customerEmail, decimal total, string paymentMethod)
    {
        var html = _templateService.SaleCreated(saleNumber, customerName, total, paymentMethod);
        FireAndForget($"Sale {saleNumber} Created", html, customerEmail: customerEmail);
    }

    public void NotifySaleReturned(string saleNumber, string? customerName, string? customerEmail, decimal returnAmount, int itemCount)
    {
        var html = _templateService.SaleReturned(saleNumber, customerName, returnAmount, itemCount);
        FireAndForget($"Sale {saleNumber} Return Processed", html, customerEmail: customerEmail);
    }

    public void NotifySaleCancelled(string saleNumber, string? customerName, decimal total)
    {
        var html = _templateService.SaleCancelled(saleNumber, customerName, total);
        FireAndForget($"Sale {saleNumber} Cancelled", html);
    }

    // ── Deliveries ───────────────────────────────────────────────────────

    public void NotifyDeliveryAssigned(int deliveryId, string? saleNumber, string? deliveryManName)
    {
        var html = _templateService.DeliveryAssigned(deliveryId, saleNumber, deliveryManName);
        FireAndForget($"Delivery #{deliveryId} Assigned", html);
    }

    public void NotifyDeliveryPickedUp(int deliveryId, string? saleNumber)
    {
        var html = _templateService.DeliveryPickedUp(deliveryId, saleNumber);
        FireAndForget($"Delivery #{deliveryId} Picked Up", html);
    }

    public void NotifyDeliveryCompleted(int deliveryId, string? saleNumber, string? customerEmail, DateTime deliveredAt)
    {
        var html = _templateService.DeliveryCompleted(deliveryId, saleNumber, deliveredAt);
        FireAndForget($"Delivery #{deliveryId} Completed", html, customerEmail: customerEmail);
    }

    // ── Payments ─────────────────────────────────────────────────────────

    public void NotifyPaymentReceived(string receiptNumber, string customerName, string? customerEmail, decimal amount, string method)
    {
        var html = _templateService.PaymentReceived(receiptNumber, customerName, amount, method);
        FireAndForget($"Payment {receiptNumber} Received", html, customerEmail: customerEmail);
    }

    public void NotifySupplierPaymentMade(string receiptNumber, string supplierName, string? supplierEmail, decimal amount, string method)
    {
        var html = _templateService.SupplierPaymentMade(receiptNumber, supplierName, amount, method);
        FireAndForget($"Payment {receiptNumber} Made", html, supplierEmail: supplierEmail);
    }

    public void NotifyPaymentVoided(string receiptNumber, string entityName, decimal amount)
    {
        var html = _templateService.PaymentVoided(receiptNumber, entityName, amount);
        FireAndForget($"Payment {receiptNumber} Voided", html);
    }

    // ── Credit/Debit Notes ───────────────────────────────────────────────

    public void NotifyCreditNoteCreated(string noteNumber, string entityName, string? entityEmail, decimal amount, string reason)
    {
        var html = _templateService.CreditNoteCreated(noteNumber, entityName, amount, reason);
        FireAndForget($"Credit Note {noteNumber} Created", html, customerEmail: entityEmail);
    }

    public void NotifyDebitNoteCreated(string noteNumber, string entityName, string? entityEmail, decimal amount, string reason)
    {
        var html = _templateService.DebitNoteCreated(noteNumber, entityName, amount, reason);
        FireAndForget($"Debit Note {noteNumber} Created", html, customerEmail: entityEmail);
    }

    // ── Fire-and-forget helper ───────────────────────────────────────────

    private void FireAndForget(string subject, string html, string? customerEmail = null, string? supplierEmail = null)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var recipients = new List<string>();

                // Always include admin emails
                var adminEmails = await _userRepo.GetAdminEmailsAsync();
                recipients.AddRange(adminEmails);

                // Add customer/supplier email if provided
                if (!string.IsNullOrWhiteSpace(customerEmail))
                    recipients.Add(customerEmail);
                if (!string.IsNullOrWhiteSpace(supplierEmail))
                    recipients.Add(supplierEmail);

                recipients = recipients.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

                if (recipients.Count > 0)
                    await _emailService.SendAsync(recipients, subject, html);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification email: {Subject}", subject);
            }
        });
    }
}
