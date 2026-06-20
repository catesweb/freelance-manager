using System.Threading.Tasks;
using FreelanceManager.Core.Models;

namespace FreelanceManager.App.Services;

public interface IEmailSender
{
    /// <summary>True when the profile has the minimum SMTP config to attempt a send.</summary>
    bool IsConfigured(BusinessProfile profile);

    /// <summary>Sends an email with a single file attachment using the profile's SMTP settings.</summary>
    Task SendAsync(BusinessProfile profile, string toEmail, string? toName,
                   string subject, string body, string attachmentPath);
}
