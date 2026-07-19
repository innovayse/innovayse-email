namespace Innovayse.Email.Domain.Interfaces;

using Innovayse.Email.Domain.Models;

public interface ISessionCookieCodec
{
    string Encode(MailboxCredentials credentials);

    /// <summary>Returns null if the value is missing, corrupt, or tampered with.</summary>
    MailboxCredentials? Decode(string cookieValue);
}
