namespace FreelanceManager.Core.Models;

public class InvoiceLineItem
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }

    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    public decimal LineTotal => Quantity * UnitPrice;
}
