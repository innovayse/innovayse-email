namespace Innovayse.Email.Domain.Interfaces;

using Innovayse.Email.Domain.Models;

public interface ISmtpService
{
    Task SendAsync(
        MailboxCredentials creds,
        List<string> to,
        List<string>? cc,
        string subject,
        string bodyHtml,
        List<(string Filename, string ContentType, Stream Content)>? attachments,
        CancellationToken ct);
}
