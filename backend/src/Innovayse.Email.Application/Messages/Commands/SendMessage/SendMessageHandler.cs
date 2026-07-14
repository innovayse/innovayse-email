namespace Innovayse.Email.Application.Messages.Commands.SendMessage;

using Innovayse.Email.Application.Session;
using Innovayse.Email.Domain.Interfaces;

public sealed class SendMessageHandler(ISmtpService smtp, MailboxSessionHolder session)
{
    public async Task HandleAsync(SendMessageCommand cmd, CancellationToken ct)
    {
        var creds = session.ActiveMailbox ?? throw new InvalidOperationException("No mailbox selected");

        List<(string, string, Stream)>? attachments = null;
        if (cmd.Attachments is { Count: > 0 })
        {
            attachments = cmd.Attachments.Select(f =>
                (f.FileName, f.ContentType, (Stream)f.OpenReadStream())).ToList();
        }

        await smtp.SendAsync(creds, cmd.To, cmd.Cc, cmd.Subject, cmd.BodyHtml, attachments, ct);
    }
}
