namespace Innovayse.Email.Application.Session;

using Innovayse.Email.Domain.Models;

public sealed class MailboxSessionHolder
{
    public MailboxCredentials? ActiveMailbox { get; set; }
}
