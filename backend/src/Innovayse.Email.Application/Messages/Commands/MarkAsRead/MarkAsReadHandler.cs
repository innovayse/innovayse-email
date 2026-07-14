namespace Innovayse.Email.Application.Messages.Commands.MarkAsRead;

using Innovayse.Email.Application.Session;
using Innovayse.Email.Domain.Interfaces;

public sealed class MarkAsReadHandler(IImapService imap, MailboxSessionHolder session)
{
    public async Task HandleReadAsync(MarkAsReadCommand cmd, CancellationToken ct)
    {
        var creds = session.ActiveMailbox ?? throw new InvalidOperationException("No mailbox selected");
        await imap.MarkAsReadAsync(creds, cmd.Folder, cmd.Uid, ct);
    }

    public async Task HandleUnreadAsync(MarkAsUnreadCommand cmd, CancellationToken ct)
    {
        var creds = session.ActiveMailbox ?? throw new InvalidOperationException("No mailbox selected");
        await imap.MarkAsUnreadAsync(creds, cmd.Folder, cmd.Uid, ct);
    }
}
