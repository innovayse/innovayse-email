namespace Innovayse.Email.Application.Messages.Queries.GetFolderCounts;

using Innovayse.Email.Application.Session;
using Innovayse.Email.Domain.Interfaces;
using Innovayse.Email.Domain.Models;

public sealed class GetFolderCountsHandler(IImapService imap, MailboxSessionHolder session)
{
    public async Task<List<FolderCount>> HandleAsync(GetFolderCountsQuery query, CancellationToken ct)
    {
        var creds = session.ActiveMailbox ?? throw new InvalidOperationException("No mailbox selected");
        return await imap.GetFolderCountsAsync(creds, ct);
    }
}
