namespace Innovayse.Email.Domain.Models;

public sealed record MailboxCredentials(
    string Email,
    string Password,
    string DisplayName,
    int QuotaMb,
    string ImapHost,
    int ImapPort,
    string SmtpHost,
    int SmtpPort);
