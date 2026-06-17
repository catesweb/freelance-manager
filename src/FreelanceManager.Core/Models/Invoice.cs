namespace FreelanceManager.Core.Models;

public class Invoice
{
    public int Id { get; set; }
    public string Number { get; set; } = string.Empty;

    public int ClientId { get; set; }
    public Client? Client { get; set; }

    public int? ProjectId { get; set; }   // nullable: standalone invoices allowed
    public Project? Project { get; set; }

    public DateTime IssueDate { get; set; } = DateTime.Today;
    public DateTime DueDate { get; set; } = DateTime.Today.AddDays(14);

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    public string Currency { get; set; } = "USD";
    public decimal TaxRate { get; set; }   // e.g. 0.20 for 20%
    public string? Notes { get; set; }

    public List<InvoiceLineItem> LineItems { get; set; } = new();
}
