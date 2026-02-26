namespace ProCosmeticsSystem.Application.DTOs.CreditDebitNotes;

public class CreditDebitNoteDto
{
    public int Id { get; set; }
    public string NoteNumber { get; set; } = string.Empty;
    public string NoteType { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public DateTime NoteDate { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int? SaleId { get; set; }
    public string? SaleNumber { get; set; }
    public int? PurchaseOrderId { get; set; }
    public string? PurchaseOrderNumber { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateCreditDebitNoteRequest
{
    public int NoteType { get; set; }
    public int AccountType { get; set; }
    public int? CustomerId { get; set; }
    public int? SupplierId { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int? SaleId { get; set; }
    public int? PurchaseOrderId { get; set; }
}
