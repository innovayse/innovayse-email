namespace Innovayse.Email.Application.Messages.Queries.ListMessages;

using Innovayse.Email.Application.Session;
using Innovayse.Email.Domain.Interfaces;
using Innovayse.Email.Domain.Models;

public sealed class ListMessagesHandler(IImapService imap, MailboxSessionHolder session)
{
    public async Task<List<EmailMessageSummary>> HandleAsync(ListMessagesQuery query, CancellationToken ct)
    {
        var creds = session.ActiveMailbox ?? throw new InvalidOperationException("No mailbox selected");
        return await imap.ListMessagesAsync(creds, query.Folder, query.Page, query.PageSize, ct);
    }
}
