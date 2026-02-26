using ProCosmeticsSystem.Application.DTOs.Accounts;
using ProCosmeticsSystem.Application.DTOs.CreditDebitNotes;
using ProCosmeticsSystem.Application.DTOs.Payments;
using ProCosmeticsSystem.Application.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ProCosmeticsSystem.Infrastructure.Services;

public class PdfService : IPdfService
{
    public byte[] GeneratePaymentReceipt(PaymentDto payment)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text("Pro Cosmetics").Bold().FontSize(18);
                    col.Item().Text("Payment Receipt").FontSize(14).SemiBold();
                    col.Item().LineHorizontal(1);
                    col.Item().Height(10);
                });

                page.Content().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"Receipt #: {payment.ReceiptNumber}").SemiBold();
                            c.Item().Text($"Date: {payment.PaymentDate:dd/MM/yyyy}");
                            c.Item().Text($"Type: {payment.PaymentType}");
                        });
                        row.RelativeItem().Column(c =>
                        {
                            if (payment.CustomerName != null)
                                c.Item().Text($"Customer: {payment.CustomerName}");
                            if (payment.SupplierName != null)
                                c.Item().Text($"Supplier: {payment.SupplierName}");
                        });
                    });

                    col.Item().Height(20);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(3);
                        });

                        table.Cell().Border(1).Padding(5).Text("Amount").SemiBold();
                        table.Cell().Border(1).Padding(5).Text($"{payment.Amount:N2}");

                        table.Cell().Border(1).Padding(5).Text("Payment Method").SemiBold();
                        table.Cell().Border(1).Padding(5).Text(payment.PaymentMethod);

                        if (!string.IsNullOrEmpty(payment.ChequeNumber))
                        {
                            table.Cell().Border(1).Padding(5).Text("Cheque Number").SemiBold();
                            table.Cell().Border(1).Padding(5).Text(payment.ChequeNumber);
                        }

                        if (!string.IsNullOrEmpty(payment.BankName))
                        {
                            table.Cell().Border(1).Padding(5).Text("Bank Name").SemiBold();
                            table.Cell().Border(1).Padding(5).Text(payment.BankName);
                        }

                        if (payment.ChequeDate.HasValue)
                        {
                            table.Cell().Border(1).Padding(5).Text("Cheque Date").SemiBold();
                            table.Cell().Border(1).Padding(5).Text($"{payment.ChequeDate:dd/MM/yyyy}");
                        }

                        if (!string.IsNullOrEmpty(payment.BankAccountReference))
                        {
                            table.Cell().Border(1).Padding(5).Text("Bank Reference").SemiBold();
                            table.Cell().Border(1).Padding(5).Text(payment.BankAccountReference);
                        }

                        if (!string.IsNullOrEmpty(payment.Notes))
                        {
                            table.Cell().Border(1).Padding(5).Text("Notes").SemiBold();
                            table.Cell().Border(1).Padding(5).Text(payment.Notes);
                        }
                    });
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Generated on ");
                    t.Span($"{DateTime.UtcNow:dd/MM/yyyy HH:mm}");
                });
            });
        });

        return document.GeneratePdf();
    }

    public byte[] GenerateCreditDebitNote(CreditDebitNoteDto note)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text("Pro Cosmetics").Bold().FontSize(18);
                    col.Item().Text($"{note.NoteType}").FontSize(14).SemiBold();
                    col.Item().LineHorizontal(1);
                    col.Item().Height(10);
                });

                page.Content().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"Note #: {note.NoteNumber}").SemiBold();
                            c.Item().Text($"Date: {note.NoteDate:dd/MM/yyyy}");
                            c.Item().Text($"Account Type: {note.AccountType}");
                        });
                        row.RelativeItem().Column(c =>
                        {
                            if (note.CustomerName != null)
                                c.Item().Text($"Customer: {note.CustomerName}");
                            if (note.SupplierName != null)
                                c.Item().Text($"Supplier: {note.SupplierName}");
                        });
                    });

                    col.Item().Height(20);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(3);
                        });

                        table.Cell().Border(1).Padding(5).Text("Amount").SemiBold();
                        table.Cell().Border(1).Padding(5).Text($"{note.Amount:N2}");

                        table.Cell().Border(1).Padding(5).Text("Reason").SemiBold();
                        table.Cell().Border(1).Padding(5).Text(note.Reason);

                        if (note.SaleNumber != null)
                        {
                            table.Cell().Border(1).Padding(5).Text("Related Sale").SemiBold();
                            table.Cell().Border(1).Padding(5).Text(note.SaleNumber);
                        }

                        if (note.PurchaseOrderNumber != null)
                        {
                            table.Cell().Border(1).Padding(5).Text("Related PO").SemiBold();
                            table.Cell().Border(1).Padding(5).Text(note.PurchaseOrderNumber);
                        }
                    });
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Generated on ");
                    t.Span($"{DateTime.UtcNow:dd/MM/yyyy HH:mm}");
                });
            });
        });

        return document.GeneratePdf();
    }

    public byte[] GenerateAccountStatement(AccountStatementDto statement)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(col =>
                {
                    col.Item().Text("Pro Cosmetics").Bold().FontSize(18);
                    col.Item().Text($"Account Statement - {statement.AccountName}").FontSize(14).SemiBold();
                    col.Item().Text($"Period: {statement.FromDate:dd/MM/yyyy} to {statement.ToDate:dd/MM/yyyy}");
                    col.Item().LineHorizontal(1);
                    col.Item().Height(5);
                });

                page.Content().Column(col =>
                {
                    col.Item().Text($"Opening Balance: {statement.OpeningBalance:N2}").SemiBold();
                    col.Item().Height(10);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(80);  // Date
                            columns.RelativeColumn(1);    // Reference
                            columns.RelativeColumn(3);    // Description
                            columns.ConstantColumn(90);   // Debit
                            columns.ConstantColumn(90);   // Credit
                            columns.ConstantColumn(100);  // Balance
                        });

                        // Header
                        table.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(4).Text("Date").SemiBold();
                        table.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(4).Text("Reference").SemiBold();
                        table.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(4).Text("Description").SemiBold();
                        table.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Debit").SemiBold();
                        table.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Credit").SemiBold();
                        table.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Balance").SemiBold();

                        foreach (var line in statement.Lines)
                        {
                            table.Cell().Border(1).Padding(4).Text($"{line.EntryDate:dd/MM/yyyy}");
                            table.Cell().Border(1).Padding(4).Text($"{line.ReferenceType} #{line.ReferenceId}");
                            table.Cell().Border(1).Padding(4).Text(line.Description);
                            table.Cell().Border(1).Padding(4).AlignRight().Text(line.DebitAmount > 0 ? $"{line.DebitAmount:N2}" : "");
                            table.Cell().Border(1).Padding(4).AlignRight().Text(line.CreditAmount > 0 ? $"{line.CreditAmount:N2}" : "");
                            table.Cell().Border(1).Padding(4).AlignRight().Text($"{line.RunningBalance:N2}");
                        }
                    });

                    col.Item().Height(10);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem();
                        row.ConstantItem(300).Column(c =>
                        {
                            c.Item().Text($"Total Debits: {statement.TotalDebits:N2}");
                            c.Item().Text($"Total Credits: {statement.TotalCredits:N2}");
                            c.Item().Text($"Closing Balance: {statement.ClosingBalance:N2}").Bold();
                        });
                    });
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Generated on ");
                    t.Span($"{DateTime.UtcNow:dd/MM/yyyy HH:mm}");
                    t.Span(" | Page ");
                    t.CurrentPageNumber();
                    t.Span(" of ");
                    t.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }
}
