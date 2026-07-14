namespace Innovayse.Email.Application.Messages.Queries.GetMessage;

using Innovayse.Email.Application.Session;
using Innovayse.Email.Domain.Interfaces;
using Innovayse.Email.Domain.Models;

public sealed class GetMessageHandler(IImapService imap, MailboxSessionHolder session)
{
    public async Task<EmailMessageDetail?> HandleAsync(GetMessageQuery query, CancellationToken ct)
    {
        var creds = session.ActiveMailbox ?? throw new InvalidOperationException("No mailbox selected");
        return await imap.GetMessageAsync(creds, query.Folder, query.Uid, ct);
    }
}
