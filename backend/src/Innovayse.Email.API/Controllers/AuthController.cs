namespace Innovayse.Email.API.Controllers;

using Innovayse.Email.Application.Auth.Commands.Login;
using Innovayse.Email.Domain.Exceptions;
using Innovayse.Email.Domain.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    LoginHandler login,
    ISessionCookieCodec cookieCodec,
    IWebHostEnvironment env) : ControllerBase
{
    private const string CookieName = "mail_session";

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { error = "Email and password are required" });

        try
        {
            var creds = await login.HandleAsync(new LoginCommand(request.Email, request.Password), ct);
            if (creds is null)
                return Unauthorized(new { error = "Invalid email or password" });

            var cookieValue = cookieCodec.Encode(creds);
            Response.Cookies.Append(CookieName, cookieValue, new CookieOptions
            {
                HttpOnly = true,
                Secure = !env.IsDevelopment(),
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromHours(12),
                Path = "/",
            });

            return Ok(new { email = creds.Email, displayName = creds.DisplayName });
        }
        catch (MailServerUnavailableException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Mail server is unavailable" });
        }
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(CookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = !env.IsDevelopment(),
            SameSite = SameSiteMode.Lax,
            Path = "/",
        });
        return Ok(new { success = true });
    }
}

public sealed record LoginRequest(string Email, string Password);
