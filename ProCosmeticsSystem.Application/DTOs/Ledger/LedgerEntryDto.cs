namespace ProCosmeticsSystem.Application.DTOs.Ledger;

public class LedgerEntryDto
{
    public int Id { get; set; }
    public DateTime EntryDate { get; set; }
    public string AccountType { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string ReferenceType { get; set; } = string.Empty;
    public int ReferenceId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public bool IsReversed { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateManualLedgerEntryRequest
{
    public int AccountType { get; set; }
    public int? CustomerId { get; set; }
    public int? SupplierId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
}
