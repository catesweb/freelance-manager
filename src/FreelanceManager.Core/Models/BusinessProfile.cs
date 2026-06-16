namespace FreelanceManager.Core.Models;

public class BusinessProfile
{
    public int Id { get; set; }   // always 1 (singleton row)
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? LogoPath { get; set; }

    public string DefaultCurrency { get; set; } = "USD";
    public decimal DefaultTaxRate { get; set; }
    public string InvoiceNumberFormat { get; set; } = "INV-{YYYY}-{0000}";
}
