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
    public ThemeMode Theme { get; set; } = ThemeMode.System;

    // SMTP (for emailing invoices). Password is stored DPAPI-encrypted (base64), never plaintext.
    public string? SmtpHost { get; set; }
    public int SmtpPort { get; set; } = 587;
    public string? SmtpUsername { get; set; }
    public string? SmtpPasswordEncrypted { get; set; }
    public bool SmtpUseSsl { get; set; } = true;
    public string? SmtpFromEmail { get; set; }   // falls back to Email when blank
    public string? SmtpFromName { get; set; }     // falls back to Name when blank
}
