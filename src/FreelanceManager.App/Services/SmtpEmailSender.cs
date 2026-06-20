using System;
using System.Threading.Tasks;
using FreelanceManager.Core.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace FreelanceManager.App.Services;

public class SmtpEmailSender : IEmailSender
{
    public bool IsConfigured(BusinessProfile profile) =>
        !string.IsNullOrWhiteSpace(profile.SmtpHost) &&
        !string.IsNullOrWhiteSpace(FromEmail(profile));

    public async Task SendAsync(BusinessProfile profile, string toEmail, string? toName,
                                string subject, string body, string attachmentPath)
    {
        if (!IsConfigured(profile))
            throw new InvalidOperationException("SMTP is not configured. Add host and a from-address in Settings.");
        if (string.IsNullOrWhiteSpace(toEmail))
            throw new InvalidOperationException("The client has no email address.");

        var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress(FromName(profile), FromEmail(profile)!));   // non-null: IsConfigured checked
        msg.To.Add(new MailboxAddress(toName ?? toEmail, toEmail));
        msg.Subject = subject;

        var builder = new BodyBuilder { TextBody = body };
        builder.Attachments.Add(attachmentPath);
        msg.Body = builder.ToMessageBody();

        using var client = await ConnectAsync(profile, null);
        await client.SendAsync(msg);
        await client.DisconnectAsync(true);
    }

    public async Task TestConnectionAsync(BusinessProfile profile, string? plainPassword)
    {
        if (!IsConfigured(profile))
            throw new InvalidOperationException("Enter at least an SMTP host and a from-address.");
        using var client = await ConnectAsync(profile, plainPassword);
        await client.DisconnectAsync(true);
    }

    private static async Task<SmtpClient> ConnectAsync(BusinessProfile profile, string? plainPassword)
    {
        var client = new SmtpClient();
        var security = profile.SmtpUseSsl ? SecureSocketOptions.Auto : SecureSocketOptions.None;
        await client.ConnectAsync(profile.SmtpHost!, profile.SmtpPort, security);

        if (!string.IsNullOrWhiteSpace(profile.SmtpUsername))
        {
            var password = !string.IsNullOrEmpty(plainPassword)
                ? plainPassword
                : Dpapi.Decrypt(profile.SmtpPasswordEncrypted) ?? string.Empty;
            await client.AuthenticateAsync(profile.SmtpUsername, password);
        }
        return client;
    }

    private static string? FromEmail(BusinessProfile p) =>
        string.IsNullOrWhiteSpace(p.SmtpFromEmail) ? p.Email : p.SmtpFromEmail;

    private static string FromName(BusinessProfile p) =>
        string.IsNullOrWhiteSpace(p.SmtpFromName) ? (p.Name ?? "") : p.SmtpFromName;
}
