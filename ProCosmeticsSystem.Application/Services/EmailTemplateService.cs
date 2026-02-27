namespace ProCosmeticsSystem.Application.Services;

public class EmailTemplateService
{
    private static string Wrap(string title, string content) => $@"
<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8""/>
<style>
  body {{ font-family: 'Segoe UI', Arial, sans-serif; margin: 0; padding: 0; background: #f4f4f7; }}
  .container {{ max-width: 600px; margin: 20px auto; background: #fff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.08); }}
  .header {{ background: linear-gradient(135deg, #1a1a2e, #16213e); padding: 24px; text-align: center; }}
  .header h1 {{ color: #e94560; margin: 0; font-size: 22px; letter-spacing: 1px; }}
  .header p {{ color: #a0a0b0; margin: 4px 0 0; font-size: 12px; }}
  .body {{ padding: 24px 32px; color: #333; line-height: 1.6; }}
  .body h2 {{ color: #1a1a2e; margin-top: 0; font-size: 18px; }}
  .detail {{ background: #f8f9fa; border-left: 4px solid #e94560; padding: 12px 16px; margin: 16px 0; border-radius: 4px; }}
  .detail p {{ margin: 4px 0; font-size: 14px; }}
  .label {{ font-weight: 600; color: #555; }}
  .footer {{ background: #f4f4f7; padding: 16px; text-align: center; font-size: 12px; color: #888; }}
</style>
</head>
<body>
<div class=""container"">
  <div class=""header"">
    <h1>Pro Cosmetics</h1>
    <p>{title}</p>
  </div>
  <div class=""body"">
    {content}
  </div>
  <div class=""footer"">
    <p>This is an automated notification from Pro Cosmetics ERP System.</p>
    <p>&copy; {DateTime.UtcNow.Year} Pro Cosmetics. All rights reserved.</p>
  </div>
</div>
</body>
</html>";

    // ── Purchase Orders ─────────────────────────────────────────────────

    public string PurchaseOrderCreated(string orderNumber, string supplier, decimal total, int itemCount)
        => Wrap("Purchase Order Created", $@"
<h2>New Purchase Order Created</h2>
<div class=""detail"">
  <p><span class=""label"">Order #:</span> {orderNumber}</p>
  <p><span class=""label"">Supplier:</span> {supplier}</p>
  <p><span class=""label"">Total Amount:</span> {total:N2}</p>
  <p><span class=""label"">Items:</span> {itemCount}</p>
</div>
<p>A new purchase order has been created and is currently in <strong>Draft</strong> status.</p>");

    public string PurchaseOrderSubmitted(string orderNumber, string supplier, decimal total)
        => Wrap("Purchase Order Submitted", $@"
<h2>Purchase Order Submitted</h2>
<div class=""detail"">
  <p><span class=""label"">Order #:</span> {orderNumber}</p>
  <p><span class=""label"">Supplier:</span> {supplier}</p>
  <p><span class=""label"">Total Amount:</span> {total:N2}</p>
</div>
<p>This purchase order has been submitted and is awaiting delivery.</p>");

    public string PurchaseOrderReceived(string orderNumber, string supplier, decimal receivedAmount, bool isPartial)
        => Wrap("Purchase Order Received", $@"
<h2>Purchase Order {(isPartial ? "Partially " : "")}Received</h2>
<div class=""detail"">
  <p><span class=""label"">Order #:</span> {orderNumber}</p>
  <p><span class=""label"">Supplier:</span> {supplier}</p>
  <p><span class=""label"">Received Amount:</span> {receivedAmount:N2}</p>
  <p><span class=""label"">Status:</span> {(isPartial ? "Partially Received" : "Fully Received")}</p>
</div>
<p>Goods have been received and inventory has been updated accordingly.</p>");

    public string PurchaseOrderClosed(string orderNumber, string supplier, string? reason)
        => Wrap("Purchase Order Closed", $@"
<h2>Purchase Order Closed</h2>
<div class=""detail"">
  <p><span class=""label"">Order #:</span> {orderNumber}</p>
  <p><span class=""label"">Supplier:</span> {supplier}</p>
  {(string.IsNullOrEmpty(reason) ? "" : $"<p><span class=\"label\">Reason:</span> {reason}</p>")}
</div>
<p>This partially received purchase order has been closed.</p>");

    public string PurchaseOrderCancelled(string orderNumber, string supplier, string? reason)
        => Wrap("Purchase Order Cancelled", $@"
<h2>Purchase Order Cancelled</h2>
<div class=""detail"">
  <p><span class=""label"">Order #:</span> {orderNumber}</p>
  <p><span class=""label"">Supplier:</span> {supplier}</p>
  {(string.IsNullOrEmpty(reason) ? "" : $"<p><span class=\"label\">Reason:</span> {reason}</p>")}
</div>
<p>This purchase order has been cancelled.</p>");

    // ── Sales ────────────────────────────────────────────────────────────

    public string SaleCreated(string saleNumber, string? customer, decimal total, string paymentMethod)
        => Wrap("Sale Created", $@"
<h2>New Sale Created</h2>
<div class=""detail"">
  <p><span class=""label"">Sale #:</span> {saleNumber}</p>
  {(customer != null ? $"<p><span class=\"label\">Customer:</span> {customer}</p>" : "")}
  <p><span class=""label"">Total Amount:</span> {total:N2}</p>
  <p><span class=""label"">Payment Method:</span> {paymentMethod}</p>
</div>
<p>A new sale has been recorded.</p>");

    public string SaleReturned(string saleNumber, string? customer, decimal returnAmount, int itemCount)
        => Wrap("Sale Return Processed", $@"
<h2>Sale Return Processed</h2>
<div class=""detail"">
  <p><span class=""label"">Sale #:</span> {saleNumber}</p>
  {(customer != null ? $"<p><span class=\"label\">Customer:</span> {customer}</p>" : "")}
  <p><span class=""label"">Return Amount:</span> {returnAmount:N2}</p>
  <p><span class=""label"">Items Returned:</span> {itemCount}</p>
</div>
<p>A sale return has been processed and inventory has been restored.</p>");

    public string SaleCancelled(string saleNumber, string? customer, decimal total)
        => Wrap("Sale Cancelled", $@"
<h2>Sale Cancelled</h2>
<div class=""detail"">
  <p><span class=""label"">Sale #:</span> {saleNumber}</p>
  {(customer != null ? $"<p><span class=\"label\">Customer:</span> {customer}</p>" : "")}
  <p><span class=""label"">Amount:</span> {total:N2}</p>
</div>
<p>This sale has been cancelled and inventory has been restored.</p>");

    // ── Deliveries ───────────────────────────────────────────────────────

    public string DeliveryAssigned(int deliveryId, string? saleNumber, string? deliveryMan)
        => Wrap("Delivery Assigned", $@"
<h2>Delivery Assigned</h2>
<div class=""detail"">
  <p><span class=""label"">Delivery #:</span> {deliveryId}</p>
  {(saleNumber != null ? $"<p><span class=\"label\">Sale #:</span> {saleNumber}</p>" : "")}
  {(deliveryMan != null ? $"<p><span class=\"label\">Delivery Man:</span> {deliveryMan}</p>" : "")}
</div>
<p>A delivery has been assigned and is ready for pickup.</p>");

    public string DeliveryPickedUp(int deliveryId, string? saleNumber)
        => Wrap("Delivery Picked Up", $@"
<h2>Delivery Picked Up</h2>
<div class=""detail"">
  <p><span class=""label"">Delivery #:</span> {deliveryId}</p>
  {(saleNumber != null ? $"<p><span class=\"label\">Sale #:</span> {saleNumber}</p>" : "")}
</div>
<p>Goods have been picked up and are on the way.</p>");

    public string DeliveryCompleted(int deliveryId, string? saleNumber, DateTime deliveredAt)
        => Wrap("Delivery Completed", $@"
<h2>Delivery Completed</h2>
<div class=""detail"">
  <p><span class=""label"">Delivery #:</span> {deliveryId}</p>
  {(saleNumber != null ? $"<p><span class=\"label\">Sale #:</span> {saleNumber}</p>" : "")}
  <p><span class=""label"">Delivered At:</span> {deliveredAt:yyyy-MM-dd HH:mm} UTC</p>
</div>
<p>The delivery has been completed successfully.</p>");

    // ── Payments ─────────────────────────────────────────────────────────

    public string PaymentReceived(string receiptNumber, string customer, decimal amount, string method)
        => Wrap("Payment Received", $@"
<h2>Customer Payment Received</h2>
<div class=""detail"">
  <p><span class=""label"">Receipt #:</span> {receiptNumber}</p>
  <p><span class=""label"">Customer:</span> {customer}</p>
  <p><span class=""label"">Amount:</span> {amount:N2}</p>
  <p><span class=""label"">Method:</span> {method}</p>
</div>
<p>A customer payment has been recorded.</p>");

    public string SupplierPaymentMade(string receiptNumber, string supplier, decimal amount, string method)
        => Wrap("Supplier Payment Made", $@"
<h2>Supplier Payment Made</h2>
<div class=""detail"">
  <p><span class=""label"">Receipt #:</span> {receiptNumber}</p>
  <p><span class=""label"">Supplier:</span> {supplier}</p>
  <p><span class=""label"">Amount:</span> {amount:N2}</p>
  <p><span class=""label"">Method:</span> {method}</p>
</div>
<p>A supplier payment has been issued.</p>");

    public string PaymentVoided(string receiptNumber, string entityName, decimal amount)
        => Wrap("Payment Voided", $@"
<h2>Payment Voided</h2>
<div class=""detail"">
  <p><span class=""label"">Receipt #:</span> {receiptNumber}</p>
  <p><span class=""label"">Entity:</span> {entityName}</p>
  <p><span class=""label"">Amount:</span> {amount:N2}</p>
</div>
<p>This payment has been voided and ledger entries have been reversed.</p>");

    // ── Credit/Debit Notes ───────────────────────────────────────────────

    public string CreditNoteCreated(string noteNumber, string entityName, decimal amount, string reason)
        => Wrap("Credit Note Created", $@"
<h2>Credit Note Created</h2>
<div class=""detail"">
  <p><span class=""label"">Note #:</span> {noteNumber}</p>
  <p><span class=""label"">Entity:</span> {entityName}</p>
  <p><span class=""label"">Amount:</span> {amount:N2}</p>
  <p><span class=""label"">Reason:</span> {reason}</p>
</div>
<p>A credit note has been issued.</p>");

    public string DebitNoteCreated(string noteNumber, string entityName, decimal amount, string reason)
        => Wrap("Debit Note Created", $@"
<h2>Debit Note Created</h2>
<div class=""detail"">
  <p><span class=""label"">Note #:</span> {noteNumber}</p>
  <p><span class=""label"">Entity:</span> {entityName}</p>
  <p><span class=""label"">Amount:</span> {amount:N2}</p>
  <p><span class=""label"">Reason:</span> {reason}</p>
</div>
<p>A debit note has been issued.</p>");

    // ── User Notifications ────────────────────────────────────────────────

    public string WelcomeUser(string fullName, string email, string password, string loginUrl)
        => Wrap("Welcome to Pro Cosmetics", $@"
<h2>Welcome, {fullName}!</h2>
<p>Your account has been created on the Pro Cosmetics ERP system. Below are your login credentials:</p>
<div class=""detail"">
  <p><span class=""label"">Email:</span> {email}</p>
  <p><span class=""label"">Password:</span> {password}</p>
  <p><span class=""label"">Login URL:</span> <a href=""{loginUrl}"" style=""color:#e94560;"">{loginUrl}</a></p>
</div>
<p style=""color:#e94560; font-weight:600;"">Please change your password after your first login for security purposes.</p>");

    public string PasswordResetCode(string fullName, string resetToken)
        => Wrap("Password Reset Request", $@"
<h2>Password Reset</h2>
<p>Hi {fullName}, we received a request to reset your password. Use the code below to reset it:</p>
<div class=""detail"" style=""text-align:center;"">
  <p style=""font-size:28px; font-weight:700; letter-spacing:4px; color:#e94560; margin:12px 0;"">{resetToken}</p>
</div>
<p>This code will expire shortly. If you did not request a password reset, please ignore this email.</p>");

    // ── Custom / Ad-hoc ──────────────────────────────────────────────────

    public string CustomEmail(string subject, string body)
        => Wrap(subject, $@"<h2>{subject}</h2><div style=""line-height:1.7"">{body}</div>");
}
