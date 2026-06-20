using System.Threading.Tasks;
using FreelanceManager.Core.Models;

namespace FreelanceManager.App.Services;

public interface IEmailSender
{
    /// <summary>True when the profile has the minimum SMTP config to attempt a send.</summary>
    bool IsConfigured(BusinessProfile profile);

    /// <summary>
    /// Connects (and authenticates, if a username is set) to verify the SMTP settings, then disconnects.
    /// Throws on failure. <paramref name="plainPassword"/> overrides the stored one when non-empty,
    /// so unsaved settings can be tested.
    /// </summary>
    Task TestConnectionAsync(BusinessProfile profile, string? plainPassword);

    /// <summary>Sends an email with a single file attachment using the profile's SMTP settings.</summary>
    Task SendAsync(BusinessProfile profile, string toEmail, string? toName,
                   string subject, string body, string attachmentPath);
}
