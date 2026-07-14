namespace Innovayse.Email.Infrastructure.Smtp;

using Innovayse.Email.Domain.Interfaces;
using Innovayse.Email.Domain.Models;
using MailKit.Net.Smtp;
using MimeKit;

public sealed class SmtpMailService : ISmtpService
{
    public async Task SendAsync(
        MailboxCredentials creds,
        List<string> to,
        List<string>? cc,
        string subject,
        string bodyHtml,
        List<(string Filename, string ContentType, Stream Content)>? attachments,
        CancellationToken ct)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(creds.DisplayName, creds.Email));
        foreach (var addr in to)
            message.To.Add(MailboxAddress.Parse(addr));
        if (cc is not null)
            foreach (var addr in cc)
                message.Cc.Add(MailboxAddress.Parse(addr));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = bodyHtml };
        if (attachments is not null)
        {
            foreach (var (filename, contentType, content) in attachments)
            {
                var ct2 = ContentType.Parse(contentType);
                await bodyBuilder.Attachments.AddAsync(filename, content, ct2, ct);
            }
        }
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        client.ServerCertificateValidationCallback = (s, c, h, e) => true;
        await client.ConnectAsync(creds.SmtpHost, creds.SmtpPort, MailKit.Security.SecureSocketOptions.StartTls, ct);
        await client.AuthenticateAsync(creds.Email, creds.Password, ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);

        // Copy to Sent folder via IMAP
        using var imap = new MailKit.Net.Imap.ImapClient();
        imap.ServerCertificateValidationCallback = (s, c, h, e) => true;
        await imap.ConnectAsync(creds.ImapHost, creds.ImapPort, MailKit.Security.SecureSocketOptions.SslOnConnect, ct);
        await imap.AuthenticateAsync(creds.Email, creds.Password, ct);
        var sent = imap.GetFolder(MailKit.SpecialFolder.Sent);
        if (sent is not null)
        {
            await sent.OpenAsync(MailKit.FolderAccess.ReadWrite, ct);
            var sentRequest = new MailKit.AppendRequest(message, MailKit.MessageFlags.Seen);
            await sent.AppendAsync(sentRequest, ct);
        }
        await imap.DisconnectAsync(true, ct);
    }
}
