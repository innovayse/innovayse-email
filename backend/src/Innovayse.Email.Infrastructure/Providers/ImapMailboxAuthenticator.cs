namespace Innovayse.Email.Infrastructure.Providers;

using Innovayse.Email.Domain.Exceptions;
using Innovayse.Email.Domain.Interfaces;
using Innovayse.Email.Domain.Models;
using Innovayse.Email.Infrastructure.Settings;
using MailKit.Net.Imap;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public sealed class ImapMailboxAuthenticator(
    IOptions<ImapSettings> imapSettings,
    IOptions<SmtpSettings> smtpSettings,
    ILogger<ImapMailboxAuthenticator> logger) : IMailboxAuthenticator
{
    public async Task<MailboxCredentials?> AuthenticateAsync(string email, string password, CancellationToken ct)
    {
        var imap = imapSettings.Value;
        using var client = new ImapClient
        {
            ServerCertificateValidationCallback = (s, c, h, e) => true,
        };

        try
        {
            await client.ConnectAsync(imap.Host, imap.Port, SecureSocketOptions.SslOnConnect, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "IMAP server {Host}:{Port} unreachable during login", imap.Host, imap.Port);
            throw new MailServerUnavailableException($"Could not reach mail server {imap.Host}:{imap.Port}", ex);
        }

        try
        {
            await client.AuthenticateAsync(email, password, ct);
        }
        catch (AuthenticationException)
        {
            return null;
        }
        finally
        {
            if (client.IsConnected)
                await client.DisconnectAsync(true, ct);
        }

        var smtp = smtpSettings.Value;
        return new MailboxCredentials(
            Email: email,
            Password: password,
            DisplayName: email,
            QuotaMb: 0,
            ImapHost: imap.Host,
            ImapPort: imap.Port,
            SmtpHost: smtp.Host,
            SmtpPort: smtp.Port);
    }
}
