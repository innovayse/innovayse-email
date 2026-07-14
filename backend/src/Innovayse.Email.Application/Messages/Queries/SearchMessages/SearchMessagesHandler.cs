namespace Innovayse.Email.Application.Messages.Queries.SearchMessages;

using Innovayse.Email.Application.Session;
using Innovayse.Email.Domain.Interfaces;
using Innovayse.Email.Domain.Models;

public sealed class SearchMessagesHandler(IImapService imap, MailboxSessionHolder session)
{
    public async Task<List<EmailMessageSummary>> HandleAsync(SearchMessagesQuery query, CancellationToken ct)
    {
        var creds = session.ActiveMailbox ?? throw new InvalidOperationException("No mailbox selected");
        return await imap.SearchAsync(creds, query.Query, ct);
    }
}
