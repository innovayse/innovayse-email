namespace Innovayse.Email.Application.Messages.Commands.DeleteMessage;

using Innovayse.Email.Application.Session;
using Innovayse.Email.Domain.Interfaces;

public sealed class DeleteMessageHandler(IImapService imap, MailboxSessionHolder session)
{
    public async Task HandleAsync(DeleteMessageCommand cmd, CancellationToken ct)
    {
        var creds = session.ActiveMailbox ?? throw new InvalidOperationException("No mailbox selected");
        await imap.DeleteAsync(creds, cmd.Folder, cmd.Uid, ct);
    }
}
