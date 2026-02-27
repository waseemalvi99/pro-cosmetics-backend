namespace ProCosmeticsSystem.Application.Interfaces;

public interface IEmailNotificationService
{
    // Purchase Orders
    void NotifyPurchaseOrderCreated(string orderNumber, string supplierName, string? supplierEmail, decimal total, int itemCount);
    void NotifyPurchaseOrderSubmitted(string orderNumber, string supplierName, string? supplierEmail, decimal total);
    void NotifyPurchaseOrderReceived(string orderNumber, string supplierName, decimal receivedAmount, bool isPartial);
    void NotifyPurchaseOrderClosed(string orderNumber, string supplierName, string? supplierEmail, string? reason);
    void NotifyPurchaseOrderCancelled(string orderNumber, string supplierName, string? supplierEmail, string? reason);

    // Sales
    void NotifySaleCreated(string saleNumber, string? customerName, string? customerEmail, decimal total, string paymentMethod);
    void NotifySaleReturned(string saleNumber, string? customerName, string? customerEmail, decimal returnAmount, int itemCount);
    void NotifySaleCancelled(string saleNumber, string? customerName, decimal total);

    // Deliveries
    void NotifyDeliveryAssigned(int deliveryId, string? saleNumber, string? deliveryManName);
    void NotifyDeliveryPickedUp(int deliveryId, string? saleNumber);
    void NotifyDeliveryCompleted(int deliveryId, string? saleNumber, string? customerEmail, DateTime deliveredAt);

    // Payments
    void NotifyPaymentReceived(string receiptNumber, string customerName, string? customerEmail, decimal amount, string method);
    void NotifySupplierPaymentMade(string receiptNumber, string supplierName, string? supplierEmail, decimal amount, string method);
    void NotifyPaymentVoided(string receiptNumber, string entityName, decimal amount);

    // Credit/Debit Notes
    void NotifyCreditNoteCreated(string noteNumber, string entityName, string? entityEmail, decimal amount, string reason);
    void NotifyDebitNoteCreated(string noteNumber, string entityName, string? entityEmail, decimal amount, string reason);

    // User Notifications
    void NotifyUserCreated(string fullName, string email, string password);
    void NotifyPasswordReset(string fullName, string email, string resetToken);
}
