namespace FreelanceManager.Core.Models;

public class Payment
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }

    public decimal Amount { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;
    public string? Method { get; set; }   // e.g. "Bank transfer", "PayPal"
    public string? Note { get; set; }
}
