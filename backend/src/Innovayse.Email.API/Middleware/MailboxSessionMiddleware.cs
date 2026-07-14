namespace Innovayse.Email.API.Middleware;

using Innovayse.Email.Application.Session;
using Innovayse.Email.Domain.Interfaces;

public sealed class MailboxSessionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        MailboxSessionHolder session,
        IMailboxCredentialProvider credentialProvider)
    {
        // Extract bearer token from Authorization header
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        var accessToken = authHeader?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true
            ? authHeader["Bearer ".Length..].Trim()
            : null;

        session.AccessToken = accessToken;

        // Read desired mailbox from header
        var mailboxEmail = context.Request.Headers["X-Mailbox-Email"].FirstOrDefault();

        if (!string.IsNullOrEmpty(mailboxEmail) && !string.IsNullOrEmpty(accessToken))
        {
            try
            {
                var mailboxes = await credentialProvider.GetMailboxesAsync(accessToken, context.RequestAborted);
                session.ActiveMailbox = mailboxes.FirstOrDefault(
                    m => m.Email.Equals(mailboxEmail, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                // If we can't fetch credentials, proceed without setting active mailbox.
                // Controllers will return 400/401 when they need it.
            }
        }

        await next(context);
    }
}
