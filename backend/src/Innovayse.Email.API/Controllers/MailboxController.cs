namespace Innovayse.Email.API.Controllers;

using Innovayse.Email.Application.Mailboxes.Queries.ListMailboxes;
using Innovayse.Email.Application.Session;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/mailboxes")]
[Authorize]
public sealed class MailboxController(
    ListMailboxesHandler listMailboxes,
    MailboxSessionHolder session) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMailboxes(CancellationToken ct)
    {
        var token = session.AccessToken;
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var mailboxes = await listMailboxes.HandleAsync(new ListMailboxesQuery(token), ct);
        var result = mailboxes.Select(m => new
        {
            email = m.Email,
            displayName = m.DisplayName,
            quotaMb = m.QuotaMb,
            imapHost = m.ImapHost,
            smtpHost = m.SmtpHost,
        });
        return Ok(result);
    }

    [HttpPost("select")]
    public IActionResult SelectMailbox([FromBody] SelectMailboxRequest request)
    {
        // The active mailbox is resolved per-request via MailboxSessionMiddleware using the X-Mailbox-Email header.
        // This endpoint exists for the frontend to "confirm" selection (e.g., persist to cookie).
        // The actual credential lookup happens in the middleware on each request.
        Response.Cookies.Append("X-Mailbox-Email", request.Email, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Secure = Request.IsHttps,
            MaxAge = TimeSpan.FromDays(7),
        });
        return Ok(new { email = request.Email });
    }

    [HttpGet("active")]
    public IActionResult GetActiveMailbox()
    {
        var mailbox = session.ActiveMailbox;
        if (mailbox is null)
            return NotFound(new { error = "No active mailbox selected" });

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

public sealed record SelectMailboxRequest(string Email);
