namespace Innovayse.Email.Domain.Interfaces;

using Innovayse.Email.Domain.Models;

public interface IMailboxCredentialProvider
{
    Task<List<MailboxCredentials>> GetMailboxesAsync(string accessToken, CancellationToken ct);
}
