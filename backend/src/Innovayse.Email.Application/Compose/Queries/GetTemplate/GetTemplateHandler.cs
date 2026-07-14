namespace Innovayse.Email.Application.Compose.Queries.GetTemplate;

using Innovayse.Email.Application.Session;
using Innovayse.Email.Domain.Interfaces;
using Innovayse.Email.Domain.Models;

public sealed class GetTemplateHandler(IImapService imap, MailboxSessionHolder session)
{
    public async Task<EmailMessageDetail?> HandleAsync(GetTemplateQuery query, CancellationToken ct)
    {
        var creds = session.ActiveMailbox ?? throw new InvalidOperationException("No mailbox selected");
        return await imap.GetMessageAsync(creds, EmailFolder.Templates, query.Uid, ct);
    }
}
