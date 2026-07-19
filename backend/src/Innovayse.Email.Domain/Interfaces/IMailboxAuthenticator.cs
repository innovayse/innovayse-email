namespace Innovayse.Email.Domain.Interfaces;

using Innovayse.Email.Domain.Models;

public interface IMailboxAuthenticator
{
    /// <summary>Attempts a live login against the mail server. Returns null on bad credentials.</summary>
    /// <exception cref="Innovayse.Email.Domain.Exceptions.MailServerUnavailableException">
    /// Thrown when the mail server itself cannot be reached.
    /// </exception>
    Task<MailboxCredentials?> AuthenticateAsync(string email, string password, CancellationToken ct);
}
