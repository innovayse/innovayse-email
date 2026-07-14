namespace Innovayse.Email.Application.Messages.Commands.SaveDraft;

using Innovayse.Email.Application.Session;
using Innovayse.Email.Domain.Interfaces;

public sealed class SaveDraftHandler(IImapService imap, MailboxSessionHolder session)
{
    public async Task<uint> HandleAsync(SaveDraftCommand cmd, CancellationToken ct)
    {
        var creds = session.ActiveMailbox ?? throw new InvalidOperationException("No mailbox selected");
        return await imap.SaveDraftAsync(creds, cmd.To, cmd.Subject, cmd.Body, cmd.ExistingDraftUid, ct);
    }
}
