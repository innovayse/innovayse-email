namespace Innovayse.Email.Tests.Middleware;

using Innovayse.Email.API.Middleware;
using Innovayse.Email.Application.Session;
using Innovayse.Email.Domain.Interfaces;
using Innovayse.Email.Domain.Models;
using Microsoft.AspNetCore.Http;
using Xunit;

public class MailboxSessionMiddlewareTests
{
    private sealed class FakeCodec(MailboxCredentials? result, bool throwOnDecode = false) : ISessionCookieCodec
    {
        public string Encode(MailboxCredentials credentials) => "irrelevant";
        public MailboxCredentials? Decode(string cookieValue)
            => throwOnDecode ? throw new FormatException("bad") : result;
    }

    private static MailboxCredentials SampleCredentials() =>
        new("user@example.com", "pw", "user@example.com", 0, "h", 993, "h", 587);

    [Fact]
    public async Task InvokeAsync_ValidCookie_SetsActiveMailbox()
    {
        var session = new MailboxSessionHolder();
        var middleware = new MailboxSessionMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();
        context.Request.Headers.Append("Cookie", "mail_session=anything");

        await middleware.InvokeAsync(context, session, new FakeCodec(SampleCredentials()));

        Assert.Equal("user@example.com", session.ActiveMailbox?.Email);
    }

    [Fact]
    public async Task InvokeAsync_NoCookie_LeavesActiveMailboxNull()
    {
        var session = new MailboxSessionHolder();
        var middleware = new MailboxSessionMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context, session, new FakeCodec(SampleCredentials()));

        Assert.Null(session.ActiveMailbox);
    }

    [Fact]
    public async Task InvokeAsync_CorruptCookie_LeavesActiveMailboxNull()
    {
        var session = new MailboxSessionHolder();
        var middleware = new MailboxSessionMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();
        context.Request.Headers.Append("Cookie", "mail_session=tampered");

        await middleware.InvokeAsync(context, session, new FakeCodec(null, throwOnDecode: true));

        Assert.Null(session.ActiveMailbox);
    }
}
