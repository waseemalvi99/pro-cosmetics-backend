using ProCosmeticsSystem.Domain.Common;
using ProCosmeticsSystem.Domain.Enums;

namespace ProCosmeticsSystem.Domain.Entities;

public class CreditDebitNote : AuditableEntity
{
    public string NoteNumber { get; set; } = string.Empty;
    public NoteType NoteType { get; set; }
    public NoteAccountType AccountType { get; set; }
    public int? CustomerId { get; set; }
    public int? SupplierId { get; set; }
    public DateTime NoteDate { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int? SaleId { get; set; }
    public int? PurchaseOrderId { get; set; }
}
