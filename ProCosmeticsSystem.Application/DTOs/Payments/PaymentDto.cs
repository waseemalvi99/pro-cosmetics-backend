namespace ProCosmeticsSystem.Application.DTOs.Payments;

public class PaymentDto
{
    public int Id { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public string PaymentType { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? ChequeNumber { get; set; }
    public string? BankName { get; set; }
    public DateTime? ChequeDate { get; set; }
    public string? BankAccountReference { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateCustomerPaymentRequest
{
    public int CustomerId { get; set; }
    public decimal Amount { get; set; }
    public int PaymentMethod { get; set; }
    public string? ChequeNumber { get; set; }
    public string? BankName { get; set; }
    public DateTime? ChequeDate { get; set; }
    public string? BankAccountReference { get; set; }
    public string? Notes { get; set; }
}

public class CreateSupplierPaymentRequest
{
    public int SupplierId { get; set; }
    public decimal Amount { get; set; }
    public int PaymentMethod { get; set; }
    public string? ChequeNumber { get; set; }
    public string? BankName { get; set; }
    public DateTime? ChequeDate { get; set; }
    public string? BankAccountReference { get; set; }
    public string? Notes { get; set; }
}
