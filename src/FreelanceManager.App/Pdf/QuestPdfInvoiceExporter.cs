using FreelanceManager.Core.Models;
using FreelanceManager.Core.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FreelanceManager.App.Pdf;

public class QuestPdfInvoiceExporter : IPdfExporter
{
    public void ExportInvoice(Invoice invoice, BusinessProfile profile, string outputPath)
    {
        decimal subtotal = InvoiceCalculator.Subtotal(invoice);
        decimal tax = InvoiceCalculator.Tax(invoice);
        decimal total = InvoiceCalculator.Total(invoice);
        string cur = invoice.Currency;

        Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Margin(40);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(t => t.FontSize(10));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(profile.Name).FontSize(16).Bold();
                        if (!string.IsNullOrWhiteSpace(profile.Address)) col.Item().Text(profile.Address);
                        if (!string.IsNullOrWhiteSpace(profile.Email)) col.Item().Text(profile.Email);
                    });
                    row.ConstantItem(180).Column(col =>
                    {
                        col.Item().AlignRight().Text("INVOICE").FontSize(20).Bold();
                        col.Item().AlignRight().Text(invoice.Number);
                        col.Item().AlignRight().Text($"Issued: {invoice.IssueDate:yyyy-MM-dd}");
                        col.Item().AlignRight().Text($"Due: {invoice.DueDate:yyyy-MM-dd}");
                    });
                });

                page.Content().PaddingVertical(15).Column(col =>
                {
                    col.Item().Text($"Bill to: {invoice.Client?.Name}").Bold();
                    col.Item().PaddingBottom(10);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(4);
                            c.RelativeColumn(1);
                            c.RelativeColumn(1);
                            c.RelativeColumn(1);
                        });

                        table.Header(h =>
                        {
                            h.Cell().Text("Description").Bold();
                            h.Cell().AlignRight().Text("Qty").Bold();
                            h.Cell().AlignRight().Text("Unit").Bold();
                            h.Cell().AlignRight().Text("Amount").Bold();
                        });

                        foreach (var li in invoice.LineItems)
                        {
                            table.Cell().Text(li.Description);
                            table.Cell().AlignRight().Text(li.Quantity.ToString("0.##"));
                            table.Cell().AlignRight().Text($"{cur} {li.UnitPrice:0.00}");
                            table.Cell().AlignRight().Text($"{cur} {li.LineTotal:0.00}");
                        }
                    });

                    col.Item().PaddingTop(10).AlignRight().Text($"Subtotal: {cur} {subtotal:0.00}");
                    col.Item().AlignRight().Text($"Tax ({invoice.TaxRate:P0}): {cur} {tax:0.00}");
                    col.Item().AlignRight().Text($"Total: {cur} {total:0.00}").FontSize(13).Bold();

                    if (!string.IsNullOrWhiteSpace(invoice.Notes))
                        col.Item().PaddingTop(20).Text(invoice.Notes!);
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Thank you for your business.");
                });
            });
        }).GeneratePdf(outputPath);
    }
}
