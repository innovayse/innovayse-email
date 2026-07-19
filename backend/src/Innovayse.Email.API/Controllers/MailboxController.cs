namespace Innovayse.Email.API.Controllers;

using Innovayse.Email.API.Filters;
using Innovayse.Email.Application.Session;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/mailboxes")]
[ServiceFilter(typeof(RequireActiveMailboxFilter))]
public sealed class MailboxController(MailboxSessionHolder session) : ControllerBase
{
    [HttpGet("active")]
    public IActionResult GetActiveMailbox()
    {
        var mailbox = session.ActiveMailbox!; // filter guarantees non-null
        return Ok(new
        {
            email = mailbox.Email,
            displayName = mailbox.DisplayName,
            quotaMb = mailbox.QuotaMb,
            imapHost = mailbox.ImapHost,
            smtpHost = mailbox.SmtpHost,
        });
    }
}
