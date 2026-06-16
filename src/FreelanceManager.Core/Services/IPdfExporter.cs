using FreelanceManager.Core.Models;

namespace FreelanceManager.Core.Services;

public interface IPdfExporter
{
    /// <summary>Renders the invoice to a PDF at the given path using the business profile for branding.</summary>
    void ExportInvoice(Invoice invoice, BusinessProfile profile, string outputPath);
}
