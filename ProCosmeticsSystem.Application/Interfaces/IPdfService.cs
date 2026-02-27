using ProCosmeticsSystem.Application.DTOs.Accounts;
using ProCosmeticsSystem.Application.DTOs.CreditDebitNotes;
using ProCosmeticsSystem.Application.DTOs.Payments;
using ProCosmeticsSystem.Application.DTOs.Products;

namespace ProCosmeticsSystem.Application.Interfaces;

public interface IPdfService
{
    byte[] GeneratePaymentReceipt(PaymentDto payment);
    byte[] GenerateCreditDebitNote(CreditDebitNoteDto note);
    byte[] GenerateAccountStatement(AccountStatementDto statement);
    byte[] GenerateBarcodeLabelsPdf(List<BarcodeLabelItem> items, IBarcodeService barcodeService);
}
