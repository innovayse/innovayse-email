namespace Innovayse.Email.Tests.Controllers;

using Innovayse.Email.API.Controllers;
using Innovayse.Email.Application.Auth.Commands.Login;
using Innovayse.Email.Domain.Exceptions;
using Innovayse.Email.Domain.Interfaces;
using Innovayse.Email.Domain.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

public class AuthControllerTests
{
    private sealed class FakeAuthenticator(MailboxCredentials? result, Exception? toThrow = null) : IMailboxAuthenticator
    {
        public Task<MailboxCredentials?> AuthenticateAsync(string email, string password, CancellationToken ct)
            => toThrow is not null ? throw toThrow : Task.FromResult(result);
    }

    private sealed class FakeCodec : ISessionCookieCodec
    {
        public string Encode(MailboxCredentials credentials) => "encoded:" + credentials.Email;
        public MailboxCredentials? Decode(string cookieValue) => null;
    }

    private sealed class FakeWebHostEnvironment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Production";
        public string ApplicationName { get; set; } = "Innovayse.Email.Tests";
        public string WebRootPath { get; set; } = "";
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; } = "";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    private static AuthController BuildController(IMailboxAuthenticator authenticator)
    {
        var handler = new LoginHandler(authenticator);
        var controller = new AuthController(handler, new FakeCodec(), new FakeWebHostEnvironment())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
        return controller;
    }

    [Fact]
    public async Task Login_ValidCredentials_SetsCookieAndReturnsOk()
    {
        var creds = new MailboxCredentials("user@example.com", "pw", "user@example.com", 0, "h", 993, "h", 587);
        var controller = BuildController(new FakeAuthenticator(creds));

        var result = await controller.Login(new LoginRequest("user@example.com", "pw"), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.True(controller.HttpContext.Response.Headers.ContainsKey("Set-Cookie"));
        Assert.Contains("mail_session=", controller.HttpContext.Response.Headers["Set-Cookie"].ToString());
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        var controller = BuildController(new FakeAuthenticator(null));

        var result = await controller.Login(new LoginRequest("user@example.com", "wrong"), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_ServerUnreachable_Returns503()
    {
        var controller = BuildController(new FakeAuthenticator(null, new MailServerUnavailableException("down")));

        var result = await controller.Login(new LoginRequest("user@example.com", "pw"), CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(503, status.StatusCode);
    }

    [Fact]
    public async Task Login_MissingFields_ReturnsBadRequest()
    {
        var controller = BuildController(new FakeAuthenticator(null));

        var result = await controller.Login(new LoginRequest("", ""), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void Logout_ClearsCookie()
    {
        var controller = BuildController(new FakeAuthenticator(null));

        var result = controller.Logout();

        Assert.IsType<OkObjectResult>(result);
    }
}
