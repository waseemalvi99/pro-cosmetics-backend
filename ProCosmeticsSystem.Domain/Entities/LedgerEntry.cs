using ProCosmeticsSystem.Domain.Common;
using ProCosmeticsSystem.Domain.Enums;

namespace ProCosmeticsSystem.Domain.Entities;

public class LedgerEntry : AuditableEntity
{
    public DateTime EntryDate { get; set; }
    public LedgerAccountType AccountType { get; set; }
    public int? CustomerId { get; set; }
    public int? SupplierId { get; set; }
    public string ReferenceType { get; set; } = string.Empty;
    public int ReferenceId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public bool IsReversed { get; set; }
    public int? ReversedByEntryId { get; set; }
}
