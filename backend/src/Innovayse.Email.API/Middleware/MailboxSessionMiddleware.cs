namespace Innovayse.Email.API.Middleware;

using Innovayse.Email.Application.Session;
using Innovayse.Email.Domain.Interfaces;

public sealed class MailboxSessionMiddleware(RequestDelegate next)
{
    private const string CookieName = "mail_session";

    public async Task InvokeAsync(
        HttpContext context,
        MailboxSessionHolder session,
        ISessionCookieCodec cookieCodec)
    {
        if (context.Request.Cookies.TryGetValue(CookieName, out var cookieValue) && !string.IsNullOrEmpty(cookieValue))
        {
            try
            {
                session.ActiveMailbox = cookieCodec.Decode(cookieValue);
            }
            catch
            {
                // Corrupt/tampered cookie — proceed unauthenticated; RequireActiveMailboxFilter gates access.
                session.ActiveMailbox = null;
            }
        }

        await next(context);
    }
}
