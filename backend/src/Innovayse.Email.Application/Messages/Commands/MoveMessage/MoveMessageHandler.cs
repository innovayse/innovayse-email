namespace Innovayse.Email.Application.Messages.Commands.MoveMessage;

using Innovayse.Email.Application.Session;
using Innovayse.Email.Domain.Interfaces;

public sealed class MoveMessageHandler(IImapService imap, MailboxSessionHolder session)
{
    public async Task HandleAsync(MoveMessageCommand cmd, CancellationToken ct)
    {
        var creds = session.ActiveMailbox ?? throw new InvalidOperationException("No mailbox selected");
        await imap.MoveAsync(creds, cmd.SourceFolder, cmd.Uid, cmd.TargetFolder, ct);
    }
}
