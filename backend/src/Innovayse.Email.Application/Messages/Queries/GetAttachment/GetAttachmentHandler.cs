namespace Innovayse.Email.Application.Messages.Queries.GetAttachment;

using Innovayse.Email.Application.Session;
using Innovayse.Email.Domain.Interfaces;

public sealed class GetAttachmentHandler(IImapService imap, MailboxSessionHolder session)
{
    public async Task<Stream> HandleAsync(GetAttachmentQuery query, CancellationToken ct)
    {
        var creds = session.ActiveMailbox ?? throw new InvalidOperationException("No mailbox selected");
        return await imap.GetAttachmentAsync(creds, query.Folder, query.Uid, query.AttachmentIndex, ct);
    }
}
