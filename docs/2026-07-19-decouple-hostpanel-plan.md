# Decouple innovayse-email from Hostpanel & SSO — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove innovayse-email's runtime dependency on Hostpanel and Innovayse SSO by replacing SSO-token-based mailbox lookup with a direct email+password login validated live against Mailcow's IMAP server, session state carried in a self-encrypted cookie.

**Architecture:** A new `AuthController.Login` performs a real MailKit IMAP authenticate against the configured mail server; on success it AES-encrypts the resulting `MailboxCredentials` into an httpOnly cookie (`mail_session`) using a key the service owns outright. `MailboxSessionMiddleware` decrypts that cookie on every request instead of calling Hostpanel. A new `RequireActiveMailboxFilter` replaces `[Authorize]`/JWT-bearer auth on all controllers. The frontend drops its OIDC/PKCE flow for a plain login form and stops injecting `Authorization`/`X-Mailbox-Email` headers (the cookie now carries everything, and the existing proxy already forwards cookies as-is).

**Tech Stack:** .NET 9 / ASP.NET Core (Clean Architecture, CQRS-lite handlers), MailKit, xUnit, Nuxt 4 / Vue 3.

## Global Constraints
- No dependency on Hostpanel's API in any form (config, HTTP client, docs referencing it as a runtime dependency).
- No dependency on an SSO/OIDC authority in any form (JWT bearer auth, PKCE flow, SSO config sections).
- Multi-mailbox picker UX is intentionally removed — one login = one mailbox (per approved design spec `docs/2026-07-19-decouple-hostpanel-design.md`).
- `EncryptionKey` becomes innovayse-email's own secret, independent of any other service's key.
- Login failures return `401` with a generic message (no user enumeration); mail-server-unreachable returns `503`.
- `mail.vue` / `MailSidebar.vue` must NOT need code changes — they only consume `useMailbox().activeMailbox`, which keeps the same shape (`email`, `displayName`, `quotaMb`).

---

### Task 1: Backend test project + Domain layer (interfaces & exception)

**Files:**
- Create: `backend/tests/Innovayse.Email.Tests/Innovayse.Email.Tests.csproj` (via `dotnet new xunit`)
- Create: `backend/src/Innovayse.Email.Domain/Interfaces/IMailboxAuthenticator.cs`
- Create: `backend/src/Innovayse.Email.Domain/Interfaces/ISessionCookieCodec.cs`
- Create: `backend/src/Innovayse.Email.Domain/Exceptions/MailServerUnavailableException.cs`
- Delete: `backend/src/Innovayse.Email.Domain/Interfaces/IMailboxCredentialProvider.cs`
- Test: `backend/tests/Innovayse.Email.Tests/Domain/MailServerUnavailableExceptionTests.cs`

**Interfaces:**
- Produces: `IMailboxAuthenticator.AuthenticateAsync(string email, string password, CancellationToken ct) : Task<MailboxCredentials?>` — returns `null` on bad credentials, throws `MailServerUnavailableException` if the mail server can't be reached.
- Produces: `ISessionCookieCodec.Encode(MailboxCredentials) : string` and `ISessionCookieCodec.Decode(string) : MailboxCredentials?`.
- Produces: `MailServerUnavailableException(string message, Exception? innerException = null)`.
- Consumes: `MailboxCredentials` record (`backend/src/Innovayse.Email.Domain/Models/MailboxCredentials.cs`, unchanged: `Email, Password, DisplayName, QuotaMb, ImapHost, ImapPort, SmtpHost, SmtpPort`).

- [ ] **Step 1: Create the test project**

```bash
cd backend/tests
dotnet new xunit -n Innovayse.Email.Tests -o Innovayse.Email.Tests
cd Innovayse.Email.Tests
dotnet add reference ../../src/Innovayse.Email.Domain/Innovayse.Email.Domain.csproj
dotnet add reference ../../src/Innovayse.Email.Application/Innovayse.Email.Application.csproj
dotnet add reference ../../src/Innovayse.Email.Infrastructure/Innovayse.Email.Infrastructure.csproj
dotnet add reference ../../src/Innovayse.Email.API/Innovayse.Email.API.csproj
cd ../../..
dotnet sln backend/Innovayse.Email.sln add backend/tests/Innovayse.Email.Tests/Innovayse.Email.Tests.csproj
```

- [ ] **Step 2: Write the failing test for the new exception**

```csharp
// backend/tests/Innovayse.Email.Tests/Domain/MailServerUnavailableExceptionTests.cs
namespace Innovayse.Email.Tests.Domain;

using Innovayse.Email.Domain.Exceptions;
using Xunit;

public class MailServerUnavailableExceptionTests
{
    [Fact]
    public void Constructor_SetsMessageAndInnerException()
    {
        var inner = new InvalidOperationException("connect refused");

        var ex = new MailServerUnavailableException("Could not reach mail server", inner);

        Assert.Equal("Could not reach mail server", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }
}
```

- [ ] **Step 3: Run test to verify it fails to compile (type doesn't exist yet)**

Run: `dotnet test backend/tests/Innovayse.Email.Tests/Innovayse.Email.Tests.csproj`
Expected: build error, `MailServerUnavailableException` not found.

- [ ] **Step 4: Create the Domain interfaces and exception**

```csharp
// backend/src/Innovayse.Email.Domain/Exceptions/MailServerUnavailableException.cs
namespace Innovayse.Email.Domain.Exceptions;

public sealed class MailServerUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);
```

```csharp
// backend/src/Innovayse.Email.Domain/Interfaces/IMailboxAuthenticator.cs
namespace Innovayse.Email.Domain.Interfaces;

using Innovayse.Email.Domain.Models;

public interface IMailboxAuthenticator
{
    /// <summary>Attempts a live login against the mail server. Returns null on bad credentials.</summary>
    /// <exception cref="Innovayse.Email.Domain.Exceptions.MailServerUnavailableException">
    /// Thrown when the mail server itself cannot be reached.
    /// </exception>
    Task<MailboxCredentials?> AuthenticateAsync(string email, string password, CancellationToken ct);
}
```

```csharp
// backend/src/Innovayse.Email.Domain/Interfaces/ISessionCookieCodec.cs
namespace Innovayse.Email.Domain.Interfaces;

using Innovayse.Email.Domain.Models;

public interface ISessionCookieCodec
{
    string Encode(MailboxCredentials credentials);

    /// <summary>Returns null if the value is missing, corrupt, or tampered with.</summary>
    MailboxCredentials? Decode(string cookieValue);
}
```

- [ ] **Step 5: Delete the old Hostpanel-shaped interface**

```bash
rm backend/src/Innovayse.Email.Domain/Interfaces/IMailboxCredentialProvider.cs
```

- [ ] **Step 6: Run test to verify it passes**

Run: `dotnet test backend/tests/Innovayse.Email.Tests/Innovayse.Email.Tests.csproj`
Expected: PASS (1 test) — the rest of the solution will not compile yet because `HostpanelCredentialProvider` still implements the now-deleted interface; that's fixed in Task 6. For this step, run only the Domain project's build to confirm the new types compile:

```bash
dotnet build backend/src/Innovayse.Email.Domain/Innovayse.Email.Domain.csproj
```

Expected: Build succeeded.

- [ ] **Step 7: Commit**

```bash
git add backend/tests backend/src/Innovayse.Email.Domain backend/Innovayse.Email.sln
git commit -m "test(email): add test project; add IMailboxAuthenticator/ISessionCookieCodec"
```

---

### Task 2: `SessionCookieCodec` — AES session encryption

**Files:**
- Create: `backend/src/Innovayse.Email.Infrastructure/Security/SessionCookieCodec.cs`
- Test: `backend/tests/Innovayse.Email.Tests/Infrastructure/SessionCookieCodecTests.cs`

**Interfaces:**
- Consumes: `ISessionCookieCodec` (Task 1), `MailboxCredentials` record.
- Produces: `SessionCookieCodec(string base64Key)` implementing `ISessionCookieCodec`, used by Task 4 (AuthController) and Task 5 (middleware).

- [ ] **Step 1: Write the failing tests**

```csharp
// backend/tests/Innovayse.Email.Tests/Infrastructure/SessionCookieCodecTests.cs
namespace Innovayse.Email.Tests.Infrastructure;

using Innovayse.Email.Domain.Models;
using Innovayse.Email.Infrastructure.Security;
using Xunit;

public class SessionCookieCodecTests
{
    private const string Key = "Uf2LNuJEOIDuLW1GBXu75paqpCYnEXyCtPoCxOlelK0="; // 32 bytes base64

    private static MailboxCredentials SampleCredentials() => new(
        Email: "user@example.com",
        Password: "s3cret!",
        DisplayName: "user@example.com",
        QuotaMb: 0,
        ImapHost: "mail.example.com",
        ImapPort: 993,
        SmtpHost: "mail.example.com",
        SmtpPort: 587);

    [Fact]
    public void EncodeThenDecode_RoundTripsExactCredentials()
    {
        var codec = new SessionCookieCodec(Key);
        var creds = SampleCredentials();

        var cookieValue = codec.Encode(creds);
        var decoded = codec.Decode(cookieValue);

        Assert.Equal(creds, decoded);
    }

    [Fact]
    public void Encode_ProducesDifferentCiphertextEachCall()
    {
        var codec = new SessionCookieCodec(Key);
        var creds = SampleCredentials();

        var first = codec.Encode(creds);
        var second = codec.Encode(creds);

        Assert.NotEqual(first, second); // random IV per call
    }

    [Fact]
    public void Decode_ReturnsNull_WhenValueIsTampered()
    {
        var codec = new SessionCookieCodec(Key);
        var cookieValue = codec.Encode(SampleCredentials());
        var tampered = cookieValue[..^4] + "abcd";

        var decoded = codec.Decode(tampered);

        Assert.Null(decoded);
    }

    [Fact]
    public void Decode_ReturnsNull_WhenValueIsNotBase64()
    {
        var codec = new SessionCookieCodec(Key);

        var decoded = codec.Decode("not-valid-base64!!");

        Assert.Null(decoded);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test backend/tests/Innovayse.Email.Tests/Innovayse.Email.Tests.csproj --filter SessionCookieCodecTests`
Expected: FAIL to compile — `SessionCookieCodec` doesn't exist.

- [ ] **Step 3: Implement `SessionCookieCodec`**

```csharp
// backend/src/Innovayse.Email.Infrastructure/Security/SessionCookieCodec.cs
namespace Innovayse.Email.Infrastructure.Security;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Innovayse.Email.Domain.Interfaces;
using Innovayse.Email.Domain.Models;

public sealed class SessionCookieCodec(string base64Key) : ISessionCookieCodec
{
    public string Encode(MailboxCredentials credentials)
    {
        var key = Convert.FromBase64String(base64Key);
        var plainBytes = JsonSerializer.SerializeToUtf8Bytes(credentials);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        var combined = new byte[aes.IV.Length + cipherBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, combined, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherBytes, 0, combined, aes.IV.Length, cipherBytes.Length);

        return Convert.ToBase64String(combined);
    }

    public MailboxCredentials? Decode(string cookieValue)
    {
        try
        {
            var key = Convert.FromBase64String(base64Key);
            var combined = Convert.FromBase64String(cookieValue);
            if (combined.Length < 17) return null;

            using var aes = Aes.Create();
            aes.Key = key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.IV = combined[..16];

            using var decryptor = aes.CreateDecryptor();
            var plainBytes = decryptor.TransformFinalBlock(combined, 16, combined.Length - 16);

            return JsonSerializer.Deserialize<MailboxCredentials>(plainBytes);
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException or JsonException)
        {
            return null;
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test backend/tests/Innovayse.Email.Tests/Innovayse.Email.Tests.csproj --filter SessionCookieCodecTests`
Expected: PASS (4 tests)

- [ ] **Step 5: Commit**

```bash
git add backend/src/Innovayse.Email.Infrastructure/Security backend/tests/Innovayse.Email.Tests/Infrastructure/SessionCookieCodecTests.cs
git commit -m "feat(email): add SessionCookieCodec for self-owned session encryption"
```

---

### Task 3: `ImapMailboxAuthenticator` — live IMAP login

**Files:**
- Create: `backend/src/Innovayse.Email.Infrastructure/Providers/ImapMailboxAuthenticator.cs`
- Delete: `backend/src/Innovayse.Email.Infrastructure/Providers/HostpanelCredentialProvider.cs`
- Delete: `backend/src/Innovayse.Email.Infrastructure/Settings/HostpanelSettings.cs`

**Interfaces:**
- Consumes: `IMailboxAuthenticator`, `MailServerUnavailableException` (Task 1); `ImapSettings`, `SmtpSettings` (existing, `backend/src/Innovayse.Email.Infrastructure/Settings/`).
- Produces: `ImapMailboxAuthenticator` registered as `IMailboxAuthenticator` in Task 6's DI wiring.

No unit test for this file: it makes a real network connection to an IMAP server, which is integration-level behavior (MailKit's `ImapClient` is not practically mockable without a running server or a protocol-level fake, which is out of scope here per YAGNI). It is covered by the manual verification in Task 9. This mirrors the existing `ImapMailService`/`SmtpMailService`, which also have no unit tests today.

- [ ] **Step 1: Implement `ImapMailboxAuthenticator`**

```csharp
// backend/src/Innovayse.Email.Infrastructure/Providers/ImapMailboxAuthenticator.cs
namespace Innovayse.Email.Infrastructure.Providers;

using Innovayse.Email.Domain.Exceptions;
using Innovayse.Email.Domain.Interfaces;
using Innovayse.Email.Domain.Models;
using Innovayse.Email.Infrastructure.Settings;
using MailKit.Net.Imap;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public sealed class ImapMailboxAuthenticator(
    IOptions<ImapSettings> imapSettings,
    IOptions<SmtpSettings> smtpSettings,
    ILogger<ImapMailboxAuthenticator> logger) : IMailboxAuthenticator
{
    public async Task<MailboxCredentials?> AuthenticateAsync(string email, string password, CancellationToken ct)
    {
        var imap = imapSettings.Value;
        using var client = new ImapClient
        {
            ServerCertificateValidationCallback = (s, c, h, e) => true,
        };

        try
        {
            await client.ConnectAsync(imap.Host, imap.Port, SecureSocketOptions.SslOnConnect, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "IMAP server {Host}:{Port} unreachable during login", imap.Host, imap.Port);
            throw new MailServerUnavailableException($"Could not reach mail server {imap.Host}:{imap.Port}", ex);
        }

        try
        {
            await client.AuthenticateAsync(email, password, ct);
        }
        catch (AuthenticationException)
        {
            return null;
        }
        finally
        {
            if (client.IsConnected)
                await client.DisconnectAsync(true, ct);
        }

        var smtp = smtpSettings.Value;
        return new MailboxCredentials(
            Email: email,
            Password: password,
            DisplayName: email,
            QuotaMb: 0,
            ImapHost: imap.Host,
            ImapPort: imap.Port,
            SmtpHost: smtp.Host,
            SmtpPort: smtp.Port);
    }
}
```

- [ ] **Step 2: Delete the Hostpanel provider and settings**

```bash
rm backend/src/Innovayse.Email.Infrastructure/Providers/HostpanelCredentialProvider.cs
rm backend/src/Innovayse.Email.Infrastructure/Settings/HostpanelSettings.cs
```

- [ ] **Step 3: Build to confirm the new file compiles**

Run: `dotnet build backend/src/Innovayse.Email.Infrastructure/Innovayse.Email.Infrastructure.csproj`
Expected: build errors from `DependencyInjection.cs` still referencing `HostpanelSettings`/`HostpanelCredentialProvider` — expected at this point, fixed in Task 6. Confirm the *new* file itself has no errors by checking the error list only mentions `DependencyInjection.cs`.

- [ ] **Step 4: Commit**

```bash
git add backend/src/Innovayse.Email.Infrastructure/Providers/ImapMailboxAuthenticator.cs
git rm backend/src/Innovayse.Email.Infrastructure/Providers/HostpanelCredentialProvider.cs backend/src/Innovayse.Email.Infrastructure/Settings/HostpanelSettings.cs
git commit -m "feat(email): replace HostpanelCredentialProvider with live IMAP login"
```

---

### Task 4: `AuthController` — login/logout endpoints

**Files:**
- Create: `backend/src/Innovayse.Email.Application/Auth/Commands/Login/LoginCommand.cs`
- Create: `backend/src/Innovayse.Email.Application/Auth/Commands/Login/LoginHandler.cs`
- Create: `backend/src/Innovayse.Email.API/Controllers/AuthController.cs`
- Delete: `backend/src/Innovayse.Email.Application/Mailboxes/Queries/ListMailboxes/ListMailboxesQuery.cs`
- Delete: `backend/src/Innovayse.Email.Application/Mailboxes/Queries/ListMailboxes/ListMailboxesHandler.cs`
- Test: `backend/tests/Innovayse.Email.Tests/Controllers/AuthControllerTests.cs`

**Interfaces:**
- Consumes: `IMailboxAuthenticator` (Task 1/3), `ISessionCookieCodec` (Task 1/2), `MailServerUnavailableException` (Task 1).
- Produces: `LoginCommand(string Email, string Password)`, `LoginHandler(IMailboxAuthenticator).HandleAsync(LoginCommand, CancellationToken) : Task<MailboxCredentials?>` (throws `MailServerUnavailableException` same as the authenticator). `AuthController` exposes `POST /api/auth/login` and `POST /api/auth/logout`, setting/clearing the `mail_session` cookie — consumed by Task 5's middleware.

- [ ] **Step 1: Write the failing controller tests using a hand-written fake**

```csharp
// backend/tests/Innovayse.Email.Tests/Controllers/AuthControllerTests.cs
namespace Innovayse.Email.Tests.Controllers;

using Innovayse.Email.API.Controllers;
using Innovayse.Email.Application.Auth.Commands.Login;
using Innovayse.Email.Domain.Exceptions;
using Innovayse.Email.Domain.Interfaces;
using Innovayse.Email.Domain.Models;
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

    private static AuthController BuildController(IMailboxAuthenticator authenticator)
    {
        var handler = new LoginHandler(authenticator);
        var controller = new AuthController(handler, new FakeCodec())
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
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test backend/tests/Innovayse.Email.Tests/Innovayse.Email.Tests.csproj --filter AuthControllerTests`
Expected: FAIL to compile — `LoginCommand`, `LoginHandler`, `AuthController`, `LoginRequest` don't exist yet.

- [ ] **Step 3: Implement the Application-layer command/handler**

```csharp
// backend/src/Innovayse.Email.Application/Auth/Commands/Login/LoginCommand.cs
namespace Innovayse.Email.Application.Auth.Commands.Login;

public sealed record LoginCommand(string Email, string Password);
```

```csharp
// backend/src/Innovayse.Email.Application/Auth/Commands/Login/LoginHandler.cs
namespace Innovayse.Email.Application.Auth.Commands.Login;

using Innovayse.Email.Domain.Interfaces;
using Innovayse.Email.Domain.Models;

public sealed class LoginHandler(IMailboxAuthenticator authenticator)
{
    public Task<MailboxCredentials?> HandleAsync(LoginCommand command, CancellationToken ct)
        => authenticator.AuthenticateAsync(command.Email, command.Password, ct);
}
```

- [ ] **Step 4: Implement `AuthController`**

```csharp
// backend/src/Innovayse.Email.API/Controllers/AuthController.cs
namespace Innovayse.Email.API.Controllers;

using Innovayse.Email.Application.Auth.Commands.Login;
using Innovayse.Email.Domain.Exceptions;
using Innovayse.Email.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    LoginHandler login,
    ISessionCookieCodec cookieCodec) : ControllerBase
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
                Secure = Request.IsHttps,
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
        Response.Cookies.Delete(CookieName, new CookieOptions { Path = "/" });
        return Ok(new { success = true });
    }
}

public sealed record LoginRequest(string Email, string Password);
```

- [ ] **Step 5: Delete the old `ListMailboxes` query/handler**

```bash
rm -rf backend/src/Innovayse.Email.Application/Mailboxes/Queries/ListMailboxes
```

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test backend/tests/Innovayse.Email.Tests/Innovayse.Email.Tests.csproj --filter AuthControllerTests`
Expected: PASS (5 tests). (The solution as a whole still won't build — `Application/DependencyInjection.cs` still references the deleted `ListMailboxesHandler`; fixed in Task 6.)

- [ ] **Step 7: Commit**

```bash
git add backend/src/Innovayse.Email.Application/Auth backend/src/Innovayse.Email.API/Controllers/AuthController.cs backend/tests/Innovayse.Email.Tests/Controllers/AuthControllerTests.cs
git rm -r backend/src/Innovayse.Email.Application/Mailboxes/Queries/ListMailboxes
git commit -m "feat(email): add AuthController with live-IMAP login/logout"
```

---

### Task 5: Session middleware, action filter, and `MailboxSessionHolder` cleanup

**Files:**
- Modify: `backend/src/Innovayse.Email.API/Middleware/MailboxSessionMiddleware.cs`
- Create: `backend/src/Innovayse.Email.API/Filters/RequireActiveMailboxFilter.cs`
- Modify: `backend/src/Innovayse.Email.Application/Session/MailboxSessionHolder.cs`
- Test: `backend/tests/Innovayse.Email.Tests/Middleware/MailboxSessionMiddlewareTests.cs`
- Test: `backend/tests/Innovayse.Email.Tests/Filters/RequireActiveMailboxFilterTests.cs`

**Interfaces:**
- Consumes: `ISessionCookieCodec` (Task 1/2), `MailboxSessionHolder` (existing, trimmed).
- Produces: `RequireActiveMailboxFilter` registered as a scoped service in Task 6, applied via `[ServiceFilter(typeof(RequireActiveMailboxFilter))]` on controllers in Task 6.

- [ ] **Step 1: Write the failing middleware tests**

```csharp
// backend/tests/Innovayse.Email.Tests/Middleware/MailboxSessionMiddlewareTests.cs
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
```

- [ ] **Step 2: Write the failing filter tests**

```csharp
// backend/tests/Innovayse.Email.Tests/Filters/RequireActiveMailboxFilterTests.cs
namespace Innovayse.Email.Tests.Filters;

using Innovayse.Email.API.Filters;
using Innovayse.Email.Application.Session;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Xunit;

public class RequireActiveMailboxFilterTests
{
    private static ActionExecutingContext BuildContext(MailboxSessionHolder session)
    {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor());
        return new ActionExecutingContext(
            actionContext, new List<IFilterMetadata>(), new Dictionary<string, object?>(), controller: new object());
    }

    [Fact]
    public async Task OnActionExecutionAsync_NoActiveMailbox_ShortCircuitsWith401()
    {
        var session = new MailboxSessionHolder();
        var filter = new RequireActiveMailboxFilter(session);
        var context = BuildContext(session);
        var nextCalled = false;

        await filter.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(context, context.Filters, context.Controller));
        });

        Assert.False(nextCalled);
        var result = Assert.IsType<UnauthorizedObjectResult>(context.Result);
        Assert.Equal(401, result.StatusCode);
    }

    [Fact]
    public async Task OnActionExecutionAsync_HasActiveMailbox_CallsNext()
    {
        var session = new MailboxSessionHolder
        {
            ActiveMailbox = new("user@example.com", "pw", "user@example.com", 0, "h", 993, "h", 587),
        };
        var filter = new RequireActiveMailboxFilter(session);
        var context = BuildContext(session);
        var nextCalled = false;

        await filter.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(context, context.Filters, context.Controller));
        });

        Assert.True(nextCalled);
        Assert.Null(context.Result);
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test backend/tests/Innovayse.Email.Tests/Innovayse.Email.Tests.csproj --filter "MailboxSessionMiddlewareTests|RequireActiveMailboxFilterTests"`
Expected: FAIL to compile — middleware signature and `RequireActiveMailboxFilter` not updated/created yet.

- [ ] **Step 4: Rewrite `MailboxSessionMiddleware`**

```csharp
// backend/src/Innovayse.Email.API/Middleware/MailboxSessionMiddleware.cs
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
```

- [ ] **Step 5: Create `RequireActiveMailboxFilter`**

```csharp
// backend/src/Innovayse.Email.API/Filters/RequireActiveMailboxFilter.cs
namespace Innovayse.Email.API.Filters;

using Innovayse.Email.Application.Session;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public sealed class RequireActiveMailboxFilter(MailboxSessionHolder session) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (session.ActiveMailbox is null)
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Not authenticated" });
            return;
        }

        await next();
    }
}
```

- [ ] **Step 6: Trim `MailboxSessionHolder`**

```csharp
// backend/src/Innovayse.Email.Application/Session/MailboxSessionHolder.cs
namespace Innovayse.Email.Application.Session;

using Innovayse.Email.Domain.Models;

public sealed class MailboxSessionHolder
{
    public MailboxCredentials? ActiveMailbox { get; set; }
}
```

- [ ] **Step 7: Run tests to verify they pass**

Run: `dotnet test backend/tests/Innovayse.Email.Tests/Innovayse.Email.Tests.csproj --filter "MailboxSessionMiddlewareTests|RequireActiveMailboxFilterTests"`
Expected: PASS (5 tests)

- [ ] **Step 8: Commit**

```bash
git add backend/src/Innovayse.Email.API/Middleware backend/src/Innovayse.Email.API/Filters backend/src/Innovayse.Email.Application/Session backend/tests/Innovayse.Email.Tests/Middleware backend/tests/Innovayse.Email.Tests/Filters
git commit -m "feat(email): cookie-based session middleware + RequireActiveMailboxFilter"
```

---

### Task 6: Wire it all together — DI, `Program.cs`, controllers, config

**Files:**
- Modify: `backend/src/Innovayse.Email.Infrastructure/DependencyInjection.cs`
- Modify: `backend/src/Innovayse.Email.Application/DependencyInjection.cs`
- Modify: `backend/src/Innovayse.Email.API/Program.cs`
- Modify: `backend/src/Innovayse.Email.API/Controllers/MailboxController.cs`
- Modify: `backend/src/Innovayse.Email.API/Controllers/MessageController.cs:15-17`
- Modify: `backend/src/Innovayse.Email.API/Controllers/ComposeController.cs:9-11`
- Modify: `backend/src/Innovayse.Email.API/Controllers/AttachmentController.cs:9-11`
- Modify: `backend/src/Innovayse.Email.API/Controllers/QuotaController.cs`
- Modify: `backend/src/Innovayse.Email.API/Innovayse.Email.API.csproj`
- Modify: `backend/src/Innovayse.Email.API/appsettings.json`

**Interfaces:**
- Consumes: everything produced in Tasks 1–5 (`IMailboxAuthenticator`, `ISessionCookieCodec`, `RequireActiveMailboxFilter`, `AuthController`).
- Produces: a fully-buildable, fully-testable solution — this task is the integration point; after it, `dotnet build`/`dotnet test` on the whole solution must succeed.

- [ ] **Step 1: Update `Infrastructure/DependencyInjection.cs`**

```csharp
// backend/src/Innovayse.Email.Infrastructure/DependencyInjection.cs
namespace Innovayse.Email.Infrastructure;

using Innovayse.Email.Domain.Interfaces;
using Innovayse.Email.Infrastructure.Imap;
using Innovayse.Email.Infrastructure.Providers;
using Innovayse.Email.Infrastructure.Security;
using Innovayse.Email.Infrastructure.Settings;
using Innovayse.Email.Infrastructure.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration config)
    {
        services.Configure<ImapSettings>(config.GetSection("Imap"));
        services.Configure<SmtpSettings>(config.GetSection("Smtp"));
        services.Configure<MailcowSettings>(config.GetSection("Mailcow"));

        services.AddScoped<IImapService, ImapMailService>();
        services.AddScoped<ISmtpService, SmtpMailService>();
        services.AddScoped<IMailboxAuthenticator, ImapMailboxAuthenticator>();
        services.AddSingleton<ISessionCookieCodec>(_ => new SessionCookieCodec(config["EncryptionKey"] ?? ""));

        services.AddHttpClient<IMailboxQuotaProvider, MailcowQuotaProvider>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        });

        return services;
    }
}
```

- [ ] **Step 2: Update `Application/DependencyInjection.cs`**

```csharp
// backend/src/Innovayse.Email.Application/DependencyInjection.cs
namespace Innovayse.Email.Application;

using Innovayse.Email.Application.Auth.Commands.Login;
using Innovayse.Email.Application.Compose.Queries.GetTemplate;
using Innovayse.Email.Application.Messages.Commands.DeleteMessage;
using Innovayse.Email.Application.Messages.Commands.MarkAsRead;
using Innovayse.Email.Application.Messages.Commands.MoveMessage;
using Innovayse.Email.Application.Messages.Commands.SaveDraft;
using Innovayse.Email.Application.Messages.Commands.SendMessage;
using Innovayse.Email.Application.Messages.Queries.GetAttachment;
using Innovayse.Email.Application.Messages.Queries.GetFolderCounts;
using Innovayse.Email.Application.Messages.Queries.GetMessage;
using Innovayse.Email.Application.Messages.Queries.ListMessages;
using Innovayse.Email.Application.Messages.Queries.SearchMessages;
using Innovayse.Email.Application.Quota.Queries.GetQuota;
using Innovayse.Email.Application.Session;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<MailboxSessionHolder>();
        services.AddScoped<LoginHandler>();
        services.AddScoped<ListMessagesHandler>();
        services.AddScoped<GetMessageHandler>();
        services.AddScoped<SearchMessagesHandler>();
        services.AddScoped<GetAttachmentHandler>();
        services.AddScoped<GetFolderCountsHandler>();
        services.AddScoped<SendMessageHandler>();
        services.AddScoped<SaveDraftHandler>();
        services.AddScoped<DeleteMessageHandler>();
        services.AddScoped<MoveMessageHandler>();
        services.AddScoped<MarkAsReadHandler>();
        services.AddScoped<GetTemplateHandler>();
        services.AddScoped<GetQuotaHandler>();
        return services;
    }
}
```

- [ ] **Step 3: Rewrite `Program.cs`**

```csharp
// backend/src/Innovayse.Email.API/Program.cs
using Innovayse.Email.API.Filters;
using Innovayse.Email.API.Middleware;
using Innovayse.Email.Application;
using Innovayse.Email.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));

// CORS
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(opts =>
    opts.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()));

// Health checks
builder.Services.AddHealthChecks();

// Controllers + auth gate
builder.Services.AddControllers();
builder.Services.AddScoped<RequireActiveMailboxFilter>();

// Application + Infrastructure DI
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseCors();

// Mailbox session middleware — decrypts the mail_session cookie into MailboxSessionHolder
app.UseMiddleware<MailboxSessionMiddleware>();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
```

- [ ] **Step 4: Simplify `MailboxController`**

```csharp
// backend/src/Innovayse.Email.API/Controllers/MailboxController.cs
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
```

- [ ] **Step 5: Swap `[Authorize]` for `[ServiceFilter(typeof(RequireActiveMailboxFilter))]` on the remaining controllers**

In `backend/src/Innovayse.Email.API/Controllers/MessageController.cs`, replace:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/messages")]
[Authorize]
public sealed class MessageController(
```

with:

```csharp
using Innovayse.Email.API.Filters;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/messages")]
[ServiceFilter(typeof(RequireActiveMailboxFilter))]
public sealed class MessageController(
```

Apply the identical replacement (same `using` swap, same attribute swap) to:
- `backend/src/Innovayse.Email.API/Controllers/ComposeController.cs`
- `backend/src/Innovayse.Email.API/Controllers/AttachmentController.cs`

In `backend/src/Innovayse.Email.API/Controllers/QuotaController.cs`, apply the same `using`/attribute swap AND remove the now-redundant manual null check (the filter already guarantees it):

```csharp
// backend/src/Innovayse.Email.API/Controllers/QuotaController.cs
namespace Innovayse.Email.API.Controllers;

using Innovayse.Email.API.Filters;
using Innovayse.Email.Application.Quota.Queries.GetQuota;
using Innovayse.Email.Application.Session;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/quota")]
[ServiceFilter(typeof(RequireActiveMailboxFilter))]
public sealed class QuotaController(
    GetQuotaHandler getQuota,
    MailboxSessionHolder session) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetQuota(CancellationToken ct)
    {
        var quota = await getQuota.HandleAsync(new GetQuotaQuery(session.ActiveMailbox!.Email), ct);
        return Ok(quota);
    }
}
```

- [ ] **Step 6: Remove the JWT Bearer package reference**

Edit `backend/src/Innovayse.Email.API/Innovayse.Email.API.csproj`, delete the line:

```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="9.*" />
```

- [ ] **Step 7: Remove `Sso` and `Hostpanel` from `appsettings.json`**

```json
// backend/src/Innovayse.Email.API/appsettings.json
{
  "Mailcow": {
    "ApiUrl": "",
    "ApiKey": ""
  },
  "Imap": {
    "Host": "mail.innovayse.local",
    "Port": 993,
    "UseSsl": true
  },
  "Smtp": {
    "Host": "mail.innovayse.local",
    "Port": 587,
    "UseSsl": true
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:4003"]
  },
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [{ "Name": "Console" }]
  }
}
```

- [ ] **Step 8: Build and run the full test suite**

```bash
dotnet build backend/Innovayse.Email.sln
dotnet test backend/Innovayse.Email.sln
```

Expected: build succeeds with 0 errors; all tests from Tasks 1–5 pass (15 tests total: 1 + 4 + 5 + 5, 0 failures).

- [ ] **Step 9: Commit**

```bash
git add backend/src
git commit -m "feat(email): wire up cookie auth end-to-end, drop SSO/Hostpanel from backend"
```

---

### Task 7: Frontend — replace SSO login with email/password form

**Files:**
- Delete: `frontend/server/routes/auth/login.get.ts`
- Delete: `frontend/server/routes/auth/callback.get.ts`
- Delete: `frontend/pages/select-mailbox.vue`
- Delete: `frontend/components/MailboxPicker.vue`
- Create: `frontend/pages/login.vue`
- Modify: `frontend/composables/useMailbox.ts`
- Modify: `frontend/pages/index.vue`
- Modify: `frontend/server/routes/api/[...path].ts`
- Modify: `frontend/nuxt.config.ts`

**Interfaces:**
- Consumes: backend `POST /api/auth/login`, `POST /api/auth/logout`, `GET /api/mailboxes/active` (Tasks 4/6).
- Produces: `useMailbox().activeMailbox: Mailbox | null` (shape unchanged: `email`, `displayName`, optional `quotaMb`) and `useMailbox().fetchActiveMailbox(): Promise<Mailbox | null>` — consumed as-is by the untouched `pages/mail.vue` and `components/MailSidebar.vue`.

No backend to spin up for this task's own verification (no test framework in this frontend project, matching the existing codebase — confirmed in research). Task 9 covers end-to-end manual verification against the live docker-compose stack.

- [ ] **Step 1: Delete the SSO routes and mailbox-picker UI**

```bash
rm frontend/server/routes/auth/login.get.ts
rm frontend/server/routes/auth/callback.get.ts
rm frontend/pages/select-mailbox.vue
rm frontend/components/MailboxPicker.vue
```

- [ ] **Step 2: Simplify `useMailbox.ts` to a single active mailbox**

```typescript
// frontend/composables/useMailbox.ts
export interface Mailbox {
  email: string
  displayName: string
  quotaMb?: number
}

export const useMailbox = () => {
  const activeMailbox = useState<Mailbox | null>('activeMailbox', () => null)
  const loading = useState<boolean>('mailboxLoading', () => false)
  const error = useState<string | null>('mailboxError', () => null)

  async function fetchActiveMailbox(): Promise<Mailbox | null> {
    loading.value = true
    error.value = null
    try {
      const data = await $fetch<Mailbox>('/api/mailboxes/active')
      activeMailbox.value = data
      return data
    } catch (e: any) {
      // Re-throw 401 so the caller can redirect to /login
      const status = e?.status || e?.statusCode || e?.response?.status
      if (status === 401) {
        throw e
      }
      error.value = e?.message ?? 'Failed to load mailbox'
      return null
    } finally {
      loading.value = false
    }
  }

  return {
    activeMailbox,
    loading,
    error,
    fetchActiveMailbox,
  }
}
```

- [ ] **Step 3: Add the login page**

```vue
<!-- frontend/pages/login.vue -->
<script setup lang="ts">
const email = ref('')
const password = ref('')
const errorMessage = ref('')
const submitting = ref(false)

async function handleSubmit() {
  errorMessage.value = ''
  submitting.value = true
  try {
    await $fetch('/api/auth/login', {
      method: 'POST',
      body: { email: email.value, password: password.value },
    })
    await navigateTo('/')
  } catch (e: any) {
    const status = e?.status || e?.statusCode || e?.response?.status
    errorMessage.value = status === 503
      ? 'Mail server is unavailable. Please try again shortly.'
      : 'Invalid email or password.'
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <div class="login-screen">
    <form class="login-card" @submit.prevent="handleSubmit">
      <div class="login-mark">
        <svg width="24" height="24" viewBox="0 0 24 24" fill="none">
          <path d="M12 2L14.5 9.5L22 12L14.5 14.5L12 22L9.5 14.5L2 12L9.5 9.5L12 2Z" fill="white" fill-opacity="0.95"/>
        </svg>
      </div>
      <h1>Innovayse Mail</h1>
      <label>
        <span>Email</span>
        <input v-model="email" type="email" required autocomplete="username" />
      </label>
      <label>
        <span>Password</span>
        <input v-model="password" type="password" required autocomplete="current-password" />
      </label>
      <p v-if="errorMessage" class="login-error">{{ errorMessage }}</p>
      <button type="submit" :disabled="submitting">{{ submitting ? 'Signing in…' : 'Sign in' }}</button>
    </form>
  </div>
</template>

<style scoped>
.login-screen {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 100vh;
  background: #07080d;
}
.login-card {
  width: 320px;
  display: flex;
  flex-direction: column;
  gap: 14px;
  padding: 32px;
  border-radius: 16px;
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid rgba(255, 255, 255, 0.08);
}
.login-mark {
  width: 48px;
  height: 48px;
  border-radius: 14px;
  background: linear-gradient(135deg, #29A3E8, #8B5CF6);
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 4px;
}
.login-card h1 {
  color: white;
  font-size: 18px;
  margin: 0 0 8px;
}
.login-card label {
  display: flex;
  flex-direction: column;
  gap: 6px;
  font-size: 13px;
  color: rgba(255, 255, 255, 0.7);
}
.login-card input {
  padding: 10px 12px;
  border-radius: 8px;
  border: 1px solid rgba(255, 255, 255, 0.12);
  background: rgba(255, 255, 255, 0.03);
  color: white;
  font-size: 14px;
}
.login-error {
  color: #f87171;
  font-size: 13px;
  margin: 0;
}
.login-card button {
  margin-top: 4px;
  padding: 10px 12px;
  border-radius: 8px;
  border: none;
  background: linear-gradient(135deg, #29A3E8, #8B5CF6);
  color: white;
  font-weight: 600;
  cursor: pointer;
}
.login-card button:disabled {
  opacity: 0.6;
  cursor: default;
}
</style>
```

- [ ] **Step 4: Simplify `index.vue`'s auth check**

```vue
<!-- frontend/pages/index.vue -->
<script setup lang="ts">
// Entry point: check for an active mailbox session → /mail, else → /login

const { fetchActiveMailbox } = useMailbox()

onMounted(async () => {
  try {
    await fetchActiveMailbox()
    await navigateTo('/mail')
  } catch {
    await navigateTo('/login')
  }
})
</script>

<template>
  <!-- Loading splash while redirect resolves -->
  <div class="splash">
    <div class="splash-mark">
      <svg width="28" height="28" viewBox="0 0 24 24" fill="none">
        <path d="M12 2L14.5 9.5L22 12L14.5 14.5L12 22L9.5 14.5L2 12L9.5 9.5L12 2Z" fill="white" fill-opacity="0.95"/>
      </svg>
    </div>
  </div>
</template>

<style scoped>
.splash {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100vh;
  background: #07080d;
}
.splash-mark {
  width: 64px;
  height: 64px;
  border-radius: 18px;
  background: linear-gradient(135deg, #29A3E8, #8B5CF6);
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 16px 48px rgba(41, 163, 232, 0.4);
  animation: pulse 2s ease-in-out infinite;
}
@keyframes pulse {
  0%, 100% { transform: scale(1); opacity: 1; }
  50% { transform: scale(1.06); opacity: 0.85; }
}
</style>
```

- [ ] **Step 5: Stop injecting SSO/mailbox-selection headers in the API proxy**

In `frontend/server/routes/api/[...path].ts`, delete these two blocks (the `mail_session` cookie is already forwarded automatically — it isn't in `HOP_BY_HOP_HEADERS`, so the existing generic header-forwarding loop above these blocks already carries it through):

```typescript
  // Inject Bearer token from httpOnly auth_token cookie
  const authToken = getCookie(event, 'auth_token')
  if (authToken) {
    reqHeaders['authorization'] = `Bearer ${authToken}`
  }

  // Inject active mailbox from cookie
  const mailboxEmail = getCookie(event, 'innovayse_active_mailbox')
  if (mailboxEmail) {
    reqHeaders['x-mailbox-email'] = mailboxEmail
  }
```

The file should read (full contents after the edit):

```typescript
// frontend/server/routes/api/[...path].ts
export default defineEventHandler(async (event) => {
  const config = useRuntimeConfig()
  const target = config.apiProxyTarget as string

  const path = event.path
  const targetUrl = `${target}${path}`

  // Build headers, forwarding all except hop-by-hop headers that undici's
  // fetch() refuses to set manually (connection, content-length is recomputed
  // from the forwarded body, etc.) — leaving these in throws UND_ERR_INVALID_ARG.
  const HOP_BY_HOP_HEADERS = new Set([
    'host', 'connection', 'keep-alive', 'transfer-encoding', 'upgrade', 'content-length'
  ])
  const reqHeaders: Record<string, string> = {}
  const incomingHeaders = getRequestHeaders(event)
  for (const [key, value] of Object.entries(incomingHeaders)) {
    if (HOP_BY_HOP_HEADERS.has(key)) continue
    if (value) reqHeaders[key] = value as string
  }

  const method = getMethod(event)
  const hasBody = method !== 'GET' && method !== 'HEAD'

  // For multipart/form-data (file uploads), stream the raw request body
  // For other content types, use readRawBody
  const contentType = incomingHeaders['content-type'] ?? ''
  let body: any = undefined
  if (hasBody) {
    if (contentType.includes('multipart/form-data')) {
      // Stream the raw body from the incoming request
      body = getRequestWebStream(event)
    } else {
      body = await readRawBody(event)
    }
  }

  const response = await fetch(targetUrl, {
    method,
    headers: reqHeaders,
    body,
    redirect: 'manual',
    // @ts-ignore - duplex required for streaming body
    duplex: 'half',
  })

  // Forward response headers
  for (const [key, value] of response.headers.entries()) {
    if (key === 'transfer-encoding') continue
    setResponseHeader(event, key, value)
  }

  setResponseStatus(event, response.status)

  const respContentType = response.headers.get('content-type') ?? ''
  if (respContentType.includes('application/json')) {
    return await response.json()
  }
  return await response.text()
})
```

- [ ] **Step 6: Drop SSO runtime config**

```typescript
// frontend/nuxt.config.ts
export default defineNuxtConfig({
  compatibilityDate: '2025-01-01',
  modules: ['@nuxtjs/tailwindcss'],
  app: {
    head: {
      script: [{ src: `${process.env.NUXT_PUBLIC_MAIN_URL || 'http://app.local'}/widget/header.js`, defer: true }]
    }
  },
  css: ['~/assets/css/mail.css'],
  runtimeConfig: {
    apiProxyTarget: process.env.API_PROXY_TARGET ?? 'http://localhost:4002',
    public: {
      apiBase: '/api',
      mainUrl: process.env.NUXT_PUBLIC_MAIN_URL ?? 'https://stage.innovayse.com',
    },
  },
})
```

- [ ] **Step 7: Confirm the frontend builds**

```bash
cd frontend
npm install
npm run build
cd ..
```

Expected: build succeeds with no TypeScript/import errors (confirms `select-mailbox.vue`/`MailboxPicker.vue` removal left no dangling references — `mail.vue` and `MailSidebar.vue` were verified during planning to only depend on `activeMailbox`, which keeps the same shape).

- [ ] **Step 8: Commit**

```bash
git add frontend
git commit -m "feat(email): replace SSO/mailbox-picker with direct email+password login"
```

---

### Task 8: Docker, env, and docs cleanup

**Files:**
- Modify: `docker-compose.yml`
- Modify: `README.md`
- Modify: `docs/2026-07-08-innovayse-email-design.md`
- Modify: `docs/2026-07-08-innovayse-email-plan.md`

**Interfaces:** none — configuration and documentation only, no code interfaces.

- [ ] **Step 1: Remove Hostpanel/SSO env vars from `docker-compose.yml`**

```yaml
# docker-compose.yml
services:
  email-api:
    build:
      context: .
      dockerfile: docker/api.Dockerfile
    ports:
      - "4002:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - Mailcow__ApiUrl=${MAILCOW_API_URL:-}
      - Mailcow__ApiKey=${MAILCOW_API_KEY:-}
      - Imap__Host=${IMAP_HOST:-stage-mail.innovayse.com}
      - Imap__Port=${IMAP_PORT:-993}
      - Imap__UseSsl=true
      - Smtp__Host=${SMTP_HOST:-stage-mail.innovayse.com}
      - Smtp__Port=${SMTP_PORT:-587}
      - Smtp__UseSsl=true
      - Cors__AllowedOrigins__0=https://stage-mail.innovayse.com
      - EncryptionKey=${ENCRYPTION_KEY:-Uf2LNuJEOIDuLW1GBXu75paqpCYnEXyCtPoCxOlelK0=}
    extra_hosts:
      - "stage-mail.innovayse.com:host-gateway"
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 10s
      timeout: 5s
      retries: 5
    networks:
      - default
      - innovayse_network

  email-frontend:
    build:
      context: .
      dockerfile: docker/frontend.Dockerfile
      args:
        NUXT_PUBLIC_MAIN_URL: https://stage.innovayse.com
    ports:
      - "4003:3000"
    environment:
      - NUXT_API_PROXY_TARGET=http://email-api:8080
      - NUXT_PUBLIC_MAIN_URL=https://stage.innovayse.com
      - SECURE_COOKIES=true
    depends_on:
      email-api:
        condition: service_healthy
    networks:
      - default
      - innovayse_network

networks:
  default:
  innovayse_network:
    external: true
    name: innovayse-cloud-local_innovayse_network
```

- [ ] **Step 2: Regenerate an independent `ENCRYPTION_KEY`**

```bash
openssl rand -base64 32
```

Replace the `EncryptionKey=${ENCRYPTION_KEY:-...}` default value in `docker-compose.yml` (Step 1, already applied above — swap in the freshly generated value) with the new key, and set the same value in whatever `.env` file backs the running stage stack (not tracked in git; update it on the server directly, do not commit real secrets).

- [ ] **Step 3: Update `README.md`, line ~20, to describe the new auth flow**

Find the line describing Hostpanel as the credential source and replace it with:

```markdown
Users authenticate directly with their mailbox email and password (validated live via IMAP against Mailcow's Dovecot) — no SSO, no Hostpanel dependency. One login session maps to exactly one mailbox.
```

- [ ] **Step 4: Mark the old design/plan docs as superseded**

At the top of `docs/2026-07-08-innovayse-email-design.md`, insert:

```markdown
> **Superseded 2026-07-19:** the SSO + Hostpanel-mediated auth flow described below was replaced with direct email/password IMAP login. See `docs/2026-07-19-decouple-hostpanel-design.md`.

```

At the top of `docs/2026-07-08-innovayse-email-plan.md`, insert:

```markdown
> **Superseded 2026-07-19:** Task 1 ("Hostpanel — Store Encrypted Mailbox Passwords + Credentials Endpoint") below was never implemented and is no longer needed. See `docs/2026-07-19-decouple-hostpanel-design.md`.

```

- [ ] **Step 5: Commit**

```bash
git add docker-compose.yml README.md docs/2026-07-08-innovayse-email-design.md docs/2026-07-08-innovayse-email-plan.md
git commit -m "chore(email): drop Hostpanel/SSO from compose+docs, rotate EncryptionKey"
```

---

### Task 9: End-to-end manual verification against the docker-compose stack

**Files:** none — verification only.

**Interfaces:** exercises the full stack built in Tasks 1–8.

- [ ] **Step 1: Bring up the stack**

```bash
cd /home/innovayse/www/innovayse-workflows/innovayse-email
docker compose up -d --build
docker compose ps
```

Expected: `email-api` healthy, `email-frontend` running.

- [ ] **Step 2: Confirm no Hostpanel/SSO reference remains in running config**

```bash
docker compose config | grep -iE "hostpanel|sso"
```

Expected: no output.

- [ ] **Step 3: Log in with a real Mailcow test mailbox**

Open `http://localhost:4003` in a browser (or `https://stage-mail.innovayse.com` on the staging host). Confirm redirect to `/login`. Enter a known-good mailbox email/password for a mailbox that exists on the configured Mailcow instance. Confirm redirect to `/mail` and the inbox loads.

- [ ] **Step 4: Confirm session persists across reload**

Reload the `/mail` page. Confirm it does NOT bounce back to `/login` (cookie-based session survives a full page reload).

- [ ] **Step 5: Confirm logout invalidates the session**

```bash
curl -i -X POST http://localhost:4003/api/auth/logout -b "mail_session=<value-from-browser-devtools>"
```

Then reload `/mail` in the browser after clearing cookies (or call `POST /api/auth/logout` from the browser devtools console via `fetch('/api/auth/logout', {method: 'POST'})`) and confirm it redirects to `/login`.

- [ ] **Step 6: Confirm wrong password is rejected**

```bash
curl -i -X POST http://localhost:4003/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"<real-mailbox-email>","password":"definitely-wrong"}'
```

Expected: HTTP 401, body `{"error":"Invalid email or password"}`, no `Set-Cookie` header.

- [ ] **Step 7: Confirm a down mail server returns 503**

```bash
docker compose stop <mailcow-container-name-or-service>
curl -i -X POST http://localhost:4003/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"<real-mailbox-email>","password":"<real-password>"}'
docker compose start <mailcow-container-name-or-service>
```

Expected: HTTP 503 while stopped, body `{"error":"Mail server is unavailable"}`.

- [ ] **Step 8: No commit needed** — this task only verifies Tasks 1–8; if any step fails, fix the relevant task and re-run from Step 1.
