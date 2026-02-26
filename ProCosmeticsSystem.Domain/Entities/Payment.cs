using ProCosmeticsSystem.Domain.Common;
using ProCosmeticsSystem.Domain.Enums;

namespace ProCosmeticsSystem.Domain.Entities;

public class Payment : AuditableEntity
{
    public string ReceiptNumber { get; set; } = string.Empty;
    public PaymentType PaymentType { get; set; }
    public int? CustomerId { get; set; }
    public int? SupplierId { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethodLedger PaymentMethod { get; set; }
    public string? ChequeNumber { get; set; }
    public string? BankName { get; set; }
    public DateTime? ChequeDate { get; set; }
    public string? BankAccountReference { get; set; }
    public string? Notes { get; set; }
}
