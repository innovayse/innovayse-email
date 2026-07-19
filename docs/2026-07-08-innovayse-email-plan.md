> **Superseded 2026-07-19:** Task 1 ("Hostpanel — Store Encrypted Mailbox Passwords + Credentials Endpoint") below was never implemented and is no longer needed. See `docs/2026-07-19-decouple-hostpanel-design.md`.

# Innovayse Email — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a custom webmail client (Innovayse Mail) that replaces SOGo/Roundcube, connecting to Mailcow via IMAP/SMTP with a glassmorphic Gmail-style UI.

**Architecture:** ASP.NET Core 9 backend (Clean Architecture + CQRS) proxied by a Nuxt 4 frontend (BFF pattern). No own database — reads/writes emails via IMAP/SMTP using MailKit, fetches mailbox credentials from hostpanel API, quota from Mailcow admin API. Auth via Innovayse SSO (JWT Bearer).

**Tech Stack:** .NET 9, MailKit 4.x, Nuxt 4, Vue 3, Tailwind CSS, Docker, Manrope font, inline SVG icons

## Global Constraints

- .NET 9 target framework, C# nullable enabled, implicit usings enabled
- Nuxt 4 with `compatibilityDate: '2025-01-01'`
- No icon libraries — all icons are inline SVGs with `stroke-width: 1.6-2.2`
- Font: Manrope (Google Fonts), weights 400-800
- Design tokens from `docs/reference.html` — pixel-close fidelity
- Dark mode only (`class="dark"` on html)
- No database for the email app — all state lives on IMAP server
- Mailcow admin API is server-side only — never exposed to frontend
- No references to Claude/Anthropic in commits, PRs, or code

---

## File Map

### Hostpanel Modifications (Task 1) — push separately to hostpanel repo
- Modify: `hostpanel/backend/src/Innovayse.Domain/Email/Mailbox.cs` — add `EncryptedPassword` property
- Modify: `hostpanel/backend/src/Innovayse.Infrastructure/Email/Configurations/MailboxConfiguration.cs` — map new column
- Modify: `hostpanel/backend/src/Innovayse.Application/Email/Commands/CreateMailbox/CreateMailboxHandler.cs` — encrypt + store password
- Modify: `hostpanel/backend/src/Innovayse.Application/Email/Commands/UpdateMailboxPassword/UpdateMailboxPasswordHandler.cs` — update encrypted password
- Create: `hostpanel/backend/src/Innovayse.API/Email/MailboxCredentialsController.cs` — credentials endpoint
- Create: EF migration for `encrypted_password` column

### Backend — Solution & Projects (Task 2)
- Create: `innovayse-email/backend/Innovayse.Email.sln`
- Create: `innovayse-email/backend/src/Innovayse.Email.Domain/Innovayse.Email.Domain.csproj`
- Create: `innovayse-email/backend/src/Innovayse.Email.Application/Innovayse.Email.Application.csproj`
- Create: `innovayse-email/backend/src/Innovayse.Email.Infrastructure/Innovayse.Email.Infrastructure.csproj`
- Create: `innovayse-email/backend/src/Innovayse.Email.API/Innovayse.Email.API.csproj`
- Create: `innovayse-email/backend/.gitignore`

### Backend — Domain Layer (Task 3)
- Create: `innovayse-email/backend/src/Innovayse.Email.Domain/Models/EmailMessage.cs`
- Create: `innovayse-email/backend/src/Innovayse.Email.Domain/Models/EmailFolder.cs`
- Create: `innovayse-email/backend/src/Innovayse.Email.Domain/Models/EmailAttachment.cs`
- Create: `innovayse-email/backend/src/Innovayse.Email.Domain/Models/MailboxCredentials.cs`
- Create: `innovayse-email/backend/src/Innovayse.Email.Domain/Models/QuotaInfo.cs`
- Create: `innovayse-email/backend/src/Innovayse.Email.Domain/Models/FolderCount.cs`
- Create: `innovayse-email/backend/src/Innovayse.Email.Domain/Interfaces/IImapService.cs`
- Create: `innovayse-email/backend/src/Innovayse.Email.Domain/Interfaces/ISmtpService.cs`
- Create: `innovayse-email/backend/src/Innovayse.Email.Domain/Interfaces/IMailboxCredentialProvider.cs`
- Create: `innovayse-email/backend/src/Innovayse.Email.Domain/Interfaces/IMailboxQuotaProvider.cs`

### Backend — Infrastructure Layer (Task 4)
- Create: `innovayse-email/backend/src/Innovayse.Email.Infrastructure/Imap/ImapMailService.cs`
- Create: `innovayse-email/backend/src/Innovayse.Email.Infrastructure/Smtp/SmtpMailService.cs`
- Create: `innovayse-email/backend/src/Innovayse.Email.Infrastructure/Providers/HostpanelCredentialProvider.cs`
- Create: `innovayse-email/backend/src/Innovayse.Email.Infrastructure/Providers/MailcowQuotaProvider.cs`
- Create: `innovayse-email/backend/src/Innovayse.Email.Infrastructure/Settings/ImapSettings.cs`
- Create: `innovayse-email/backend/src/Innovayse.Email.Infrastructure/Settings/SmtpSettings.cs`
- Create: `innovayse-email/backend/src/Innovayse.Email.Infrastructure/Settings/HostpanelSettings.cs`
- Create: `innovayse-email/backend/src/Innovayse.Email.Infrastructure/Settings/MailcowSettings.cs`
- Create: `innovayse-email/backend/src/Innovayse.Email.Infrastructure/DependencyInjection.cs`

### Backend — Application Layer (Task 5)
- Create: `innovayse-email/backend/src/Innovayse.Email.Application/Mailboxes/Queries/ListMailboxes/ListMailboxesQuery.cs`
- Create: `innovayse-email/backend/src/Innovayse.Email.Application/Mailboxes/Queries/ListMailboxes/ListMailboxesHandler.cs`
- Create: `innovayse-email/backend/src/Innovayse.Email.Application/Messages/Queries/ListMessages/ListMessagesQuery.cs`
- Create: `innovayse-email/backend/src/Innovayse.Email.Application/Messages/Queries/ListMessages/ListMessagesHandler.cs`
- Create: `innovayse-email/backend/src/Innovayse.Email.Application/Messages/Queries/GetMessage/GetMessageQuery.cs`
- Create: `innovayse-email/backend/src/Innovayse.Email.Application/Messages/Queries/GetMessage/GetMessageHandler.cs`
- Create: `innovayse-email/backend/src/Innovayse.Email.Application/Messages/Queries/SearchMessages/SearchMessagesQuery.cs`
- Create: `innovayse-email/backend/src/Innovayse.Email.Application/Messages/Queries/SearchMessages/SearchMessagesHandler.cs`
- Create: `innovayse-email/backend/src/Innovayse.Email.Application/Messages/Queries/GetAttachment/GetAttachmentQuery.cs`
- Create: `innovayse-email/backend/src/Innovayse.Email.Application/Messages/Queries/GetAttachment/GetAttachmentHandler.cs`
- Create: `innovayse-email/backend/src/Innovayse.Email.Application/Messages/Queries/GetFolderCounts/GetFolderCountsQuery.cs`
- Create: `innovayse-email/backend/src/Innovayse.Email.Application/Messages/Queries/GetFolderCounts/GetFolderCountsHandler.cs`
- Create: `innovayse-email/backend/src/Innovayse.Email.Application/Messages/Commands/SendMessage/SendMessageCommand.cs`
- Create: `innovayse-email/backend/src/Innovayse.Email.Application/Messages/Commands/SendMessage/SendMessageHandler.cs`
- Create: `innovayse-email/backend/src/Innovayse.Email.Application/Messages/Commands/SaveDraft/SaveDraftCommand.cs`
- Create: `innovayse-email/backend/src/Innovayse.Email.Application/Messages/Commands/SaveDraft/SaveDraftHandler.cs`
- Create: `innovayse-email/backend/src/Innovayse.Email.Application/Messages/Commands/DeleteMessage/DeleteMessageCommand.cs`
- Create: `innovayse-email/backend/src/Innovayse.Email.Application/Messages/Commands/DeleteMessage/DeleteMessageHandler.cs`
- Create: `innovayse-email/backend/src/Innovayse.Email.Application/Messages/Commands/MoveMessage/MoveMessageCommand.cs`
- Create: `innovayse-email/backend/src/Innovayse.Email.Application/Messages/Commands/MoveMessage/MoveMessageHandler.cs`
- Create: `innovayse-email/backend/src/Innovayse.Email.Application/Messages/Commands/MarkAsRead/MarkAsReadCommand.cs`
- Create: `innovayse-email/backend/src/Innovayse.Email.Application/Messages/Commands/MarkAsRead/MarkAsReadHandler.cs`
- Create: `innovayse-email/backend/src/Innovayse.Email.Application/Compose/Queries/GetTemplate/GetTemplateQuery.cs`
- Create: `innovayse-email/backend/src/Innovayse.Email.Application/Compose/Queries/GetTemplate/GetTemplateHandler.cs`
- Create: `innovayse-email/backend/src/Innovayse.Email.Application/Quota/Queries/GetQuota/GetQuotaQuery.cs`
- Create: `innovayse-email/backend/src/Innovayse.Email.Application/Quota/Queries/GetQuota/GetQuotaHandler.cs`
- Create: `innovayse-email/backend/src/Innovayse.Email.Application/DependencyInjection.cs`

### Backend — API Layer (Task 6)
- Create: `innovayse-email/backend/src/Innovayse.Email.API/Program.cs`
- Create: `innovayse-email/backend/src/Innovayse.Email.API/appsettings.json`
- Create: `innovayse-email/backend/src/Innovayse.Email.API/appsettings.Development.json`
- Create: `innovayse-email/backend/src/Innovayse.Email.API/Middleware/MailboxSessionMiddleware.cs`
- Create: `innovayse-email/backend/src/Innovayse.Email.API/Controllers/MailboxController.cs`
- Create: `innovayse-email/backend/src/Innovayse.Email.API/Controllers/MessageController.cs`
- Create: `innovayse-email/backend/src/Innovayse.Email.API/Controllers/ComposeController.cs`
- Create: `innovayse-email/backend/src/Innovayse.Email.API/Controllers/AttachmentController.cs`
- Create: `innovayse-email/backend/src/Innovayse.Email.API/Controllers/QuotaController.cs`

### Docker (Task 7)
- Create: `innovayse-email/docker/api.Dockerfile`
- Create: `innovayse-email/docker/frontend.Dockerfile`
- Create: `innovayse-email/docker-compose.yml`
- Create: `innovayse-email/.env.example`

### Frontend — Scaffolding (Task 8)
- Create: `innovayse-email/frontend/nuxt.config.ts`
- Create: `innovayse-email/frontend/package.json`
- Create: `innovayse-email/frontend/app.vue`
- Create: `innovayse-email/frontend/tailwind.config.ts`
- Create: `innovayse-email/frontend/.gitignore`
- Create: `innovayse-email/frontend/server/routes/api/[...path].ts`
- Create: `innovayse-email/frontend/assets/css/mail.css`

### Frontend — Composables (Task 9)
- Create: `innovayse-email/frontend/composables/useMailbox.ts`
- Create: `innovayse-email/frontend/composables/useMail.ts`
- Create: `innovayse-email/frontend/composables/useCompose.ts`

### Frontend — Pages & Components (Task 10)
- Create: `innovayse-email/frontend/pages/index.vue`
- Create: `innovayse-email/frontend/pages/select-mailbox.vue`
- Create: `innovayse-email/frontend/pages/mail.vue`
- Create: `innovayse-email/frontend/components/MailSidebar.vue`
- Create: `innovayse-email/frontend/components/SidebarParticles.vue`
- Create: `innovayse-email/frontend/components/TopBar.vue`
- Create: `innovayse-email/frontend/components/MessageList.vue`
- Create: `innovayse-email/frontend/components/MessageRow.vue`
- Create: `innovayse-email/frontend/components/ReadingPane.vue`
- Create: `innovayse-email/frontend/components/AttachmentItem.vue`
- Create: `innovayse-email/frontend/components/GradientAvatar.vue`
- Create: `innovayse-email/frontend/components/ComposeModal.vue`
- Create: `innovayse-email/frontend/components/ToastNotification.vue`
- Create: `innovayse-email/frontend/components/MailboxPicker.vue`

### SSO Registration (Task 11)
- Modify: `innovayse-sso/backend/src/Innovayse.SSO.API/Seed/SsoSeeder.cs` — register `innovayse-email` OIDC client

---

### Task 1: Hostpanel — Store Encrypted Mailbox Passwords + Credentials Endpoint

**Files:**
- Modify: `hostpanel/backend/src/Innovayse.Domain/Email/Mailbox.cs`
- Modify: `hostpanel/backend/src/Innovayse.Infrastructure/Email/Configurations/MailboxConfiguration.cs`
- Modify: `hostpanel/backend/src/Innovayse.Application/Email/Commands/CreateMailbox/CreateMailboxHandler.cs`
- Modify: `hostpanel/backend/src/Innovayse.Application/Email/Commands/UpdateMailboxPassword/UpdateMailboxPasswordHandler.cs`
- Create: `hostpanel/backend/src/Innovayse.API/Email/MailboxCredentialsController.cs`
- Create: EF migration

**Interfaces:**
- Consumes: `IEncryptionService` from `Innovayse.Infrastructure.Security`
- Produces: `GET /api/portal/client/email/mailboxes/credentials` endpoint returning `MailboxCredentialDto[]`

- [ ] **Step 1: Add `EncryptedPassword` to `Mailbox` entity**

In `hostpanel/backend/src/Innovayse.Domain/Email/Mailbox.cs`, add the property and update the `Create` method:

```csharp
/// <summary>Gets the AES-encrypted password for IMAP/SMTP access.</summary>
public string? EncryptedPassword { get; private set; }

/// <summary>Sets the encrypted password (called from application layer).</summary>
public void SetEncryptedPassword(string encryptedPassword) => EncryptedPassword = encryptedPassword;
```

- [ ] **Step 2: Map `EncryptedPassword` in EF config**

In `hostpanel/backend/src/Innovayse.Infrastructure/Email/Configurations/MailboxConfiguration.cs`, add inside `Configure`:

```csharp
builder.Property(x => x.EncryptedPassword).HasMaxLength(512);
```

- [ ] **Step 3: Create EF migration**

```bash
cd /home/innovayse/Desktop/innovayse-workspace/hostpanel/backend
dotnet ef migrations add AddMailboxEncryptedPassword \
  --project src/Innovayse.Infrastructure \
  --startup-project src/Innovayse.API \
  -- --connection "Host=localhost;Port=5434;Database=hostpanel_dev;Username=postgres;Password=postgres"
```

- [ ] **Step 4: Update `CreateMailboxHandler` to encrypt and store password**

In `hostpanel/backend/src/Innovayse.Application/Email/Commands/CreateMailbox/CreateMailboxHandler.cs`:

```csharp
namespace Innovayse.Application.Email.Commands.CreateMailbox;

using Innovayse.Application.Common;
using Innovayse.Application.Email.DTOs;
using Innovayse.Domain.Email.Interfaces;
using Innovayse.Infrastructure.Security;

public sealed class CreateMailboxHandler(
    IEmailDomainRepository repo,
    IMailServerClient mailServer,
    IEncryptionService encryption,
    IUnitOfWork uow)
{
    public async Task<MailboxDto> HandleAsync(CreateMailboxCommand cmd, CancellationToken ct)
    {
        var domain = await repo.FindByIdAsync(cmd.EmailDomainId, ct)
            ?? throw new InvalidOperationException($"Email domain {cmd.EmailDomainId} not found.");

        var mailbox = domain.AddMailbox(cmd.LocalPart, cmd.DisplayName, cmd.QuotaMb);
        mailbox.SetEncryptedPassword(encryption.Encrypt(cmd.Password));
        var email = mailbox.Email(domain.DomainName);

        await mailServer.CreateMailboxAsync(email, cmd.Password, cmd.DisplayName, cmd.QuotaMb, ct);
        await uow.SaveChangesAsync(ct);

        return MailboxDto.From(mailbox, domain.DomainName);
    }
}
```

Note: `IEncryptionService` is defined in `Innovayse.Infrastructure.Security` but since the interface is there, Application layer can reference the interface. If the interface is only in Infrastructure, move it to Domain or Application first. Check the actual interface location — it's already in `Innovayse.Infrastructure.Security.AesEncryptionService.cs` (the interface `IEncryptionService` is defined there). Since Application references Infrastructure indirectly through DI, inject `IEncryptionService` directly.

- [ ] **Step 5: Update `UpdateMailboxPasswordHandler` to re-encrypt**

In `hostpanel/backend/src/Innovayse.Application/Email/Commands/UpdateMailboxPassword/UpdateMailboxPasswordHandler.cs`:

```csharp
namespace Innovayse.Application.Email.Commands.UpdateMailboxPassword;

using Innovayse.Application.Common;
using Innovayse.Domain.Email.Interfaces;
using Innovayse.Infrastructure.Security;

public sealed class UpdateMailboxPasswordHandler(
    IEmailDomainRepository repo,
    IMailServerClient mailServer,
    IEncryptionService encryption,
    IUnitOfWork uow)
{
    public async Task HandleAsync(UpdateMailboxPasswordCommand cmd, CancellationToken ct)
    {
        var domain = await repo.FindByIdAsync(cmd.EmailDomainId, ct)
            ?? throw new InvalidOperationException($"Email domain {cmd.EmailDomainId} not found.");

        var mailbox = domain.Mailboxes.FirstOrDefault(m => m.Id == cmd.MailboxId)
            ?? throw new InvalidOperationException($"Mailbox {cmd.MailboxId} not found.");

        mailbox.SetEncryptedPassword(encryption.Encrypt(cmd.NewPassword));
        await mailServer.UpdateMailboxPasswordAsync(mailbox.Email(domain.DomainName), cmd.NewPassword, ct);
        await uow.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 6: Create credentials endpoint**

Create `hostpanel/backend/src/Innovayse.API/Email/MailboxCredentialsController.cs`:

```csharp
namespace Innovayse.API.Email;

using Innovayse.API.Common;
using Innovayse.Application.Clients.Queries.GetMyProfile;
using Innovayse.Domain.Email.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Wolverine;

[ApiController]
[Route("api/portal/client/email/mailboxes")]
[Authorize]
public sealed class MailboxCredentialsController(
    IMessageBus bus,
    IEmailDomainRepository repo,
    IConfiguration config) : ControllerBase
{
    [HttpGet("credentials")]
    public async Task<IActionResult> GetCredentials(CancellationToken ct)
    {
        var profile = await bus.InvokeAsync<ClientProfileDto>(
            new GetMyProfileQuery(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? throw new InvalidOperationException("User ID not found")),
            ct);

        var domains = await repo.ListByClientAsync(profile.ClientId, ct);
        var mailHostname = config["Mailcow:MailHostname"] ?? "mail.innovayse.com";

        var credentials = domains
            .Where(d => d.Status == Domain.Email.EmailDomainStatus.Active)
            .SelectMany(d => d.Mailboxes
                .Where(m => m.IsActive && m.EncryptedPassword != null)
                .Select(m => new
                {
                    email = m.Email(d.DomainName),
                    encryptedPassword = m.EncryptedPassword,
                    displayName = m.DisplayName,
                    quotaMb = m.QuotaMb,
                    imapHost = mailHostname,
                    imapPort = 993,
                    smtpHost = mailHostname,
                    smtpPort = 587,
                }))
            .ToList();

        return Ok(credentials);
    }
}
```

- [ ] **Step 7: Build and verify migration applies**

```bash
cd /home/innovayse/Desktop/innovayse-workspace/hostpanel
docker compose exec api dotnet ef database update \
  --project src/Innovayse.Infrastructure \
  --startup-project src/Innovayse.API
```

Or rebuild the container (auto-migrates in dev):
```bash
cd /home/innovayse/Desktop/innovayse-workspace/hostpanel
docker compose up -d --build api
```

- [ ] **Step 8: Commit**

```bash
cd /home/innovayse/Desktop/innovayse-workspace/hostpanel
git add backend/src/Innovayse.Domain/Email/Mailbox.cs \
  backend/src/Innovayse.Infrastructure/Email/Configurations/MailboxConfiguration.cs \
  backend/src/Innovayse.Application/Email/Commands/CreateMailbox/CreateMailboxHandler.cs \
  backend/src/Innovayse.Application/Email/Commands/UpdateMailboxPassword/UpdateMailboxPasswordHandler.cs \
  backend/src/Innovayse.API/Email/MailboxCredentialsController.cs \
  backend/src/Innovayse.Infrastructure/Persistence/Migrations/
git commit -m "feat(email): store encrypted mailbox passwords and add credentials endpoint"
```

---

### Task 2: Backend Scaffolding — Solution, Projects, Docker

**Files:**
- Create: Solution file, 4 `.csproj` files, Dockerfiles, docker-compose.yml, .gitignore, appsettings

**Interfaces:**
- Consumes: nothing
- Produces: A buildable .NET solution with 4 empty projects + Docker infrastructure

- [ ] **Step 1: Create the .NET solution and projects**

```bash
cd /home/innovayse/Desktop/innovayse-workspace/innovayse-email/backend
dotnet new sln -n Innovayse.Email
dotnet new classlib -n Innovayse.Email.Domain -o src/Innovayse.Email.Domain --framework net9.0
dotnet new classlib -n Innovayse.Email.Application -o src/Innovayse.Email.Application --framework net9.0
dotnet new classlib -n Innovayse.Email.Infrastructure -o src/Innovayse.Email.Infrastructure --framework net9.0
dotnet new web -n Innovayse.Email.API -o src/Innovayse.Email.API --framework net9.0
dotnet sln add src/Innovayse.Email.Domain src/Innovayse.Email.Application src/Innovayse.Email.Infrastructure src/Innovayse.Email.API
```

- [ ] **Step 2: Set project references and NuGet packages**

`src/Innovayse.Email.Domain/Innovayse.Email.Domain.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

`src/Innovayse.Email.Application/Innovayse.Email.Application.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="9.*" />
    <ProjectReference Include="..\Innovayse.Email.Domain\Innovayse.Email.Domain.csproj" />
  </ItemGroup>
</Project>
```

`src/Innovayse.Email.Infrastructure/Innovayse.Email.Infrastructure.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="MailKit" Version="4.*" />
    <PackageReference Include="Microsoft.Extensions.Http" Version="9.*" />
    <PackageReference Include="Microsoft.Extensions.Options.ConfigurationExtensions" Version="9.*" />
    <ProjectReference Include="..\Innovayse.Email.Application\Innovayse.Email.Application.csproj" />
  </ItemGroup>
</Project>
```

`src/Innovayse.Email.API/Innovayse.Email.API.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Serilog.AspNetCore" Version="8.*" />
    <PackageReference Include="Serilog.Sinks.Console" Version="6.*" />
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="9.*" />
    <ProjectReference Include="..\Innovayse.Email.Infrastructure\Innovayse.Email.Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Create `.gitignore`**

`innovayse-email/backend/.gitignore`:
```
bin/
obj/
*.user
.vs/
```

- [ ] **Step 4: Create `appsettings.json`**

`src/Innovayse.Email.API/appsettings.json`:
```json
{
  "Sso": {
    "Authority": "http://sso-api:8080",
    "ClientId": "innovayse-email"
  },
  "Hostpanel": {
    "ApiUrl": "http://api:5148"
  },
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

- [ ] **Step 5: Create `api.Dockerfile`**

`innovayse-email/docker/api.Dockerfile`:
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY backend/src/ src/
RUN dotnet restore src/Innovayse.Email.API/Innovayse.Email.API.csproj
RUN dotnet publish src/Innovayse.Email.API/Innovayse.Email.API.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Innovayse.Email.API.dll"]
```

- [ ] **Step 6: Create `frontend.Dockerfile`**

`innovayse-email/docker/frontend.Dockerfile`:
```dockerfile
FROM node:22-alpine AS build
WORKDIR /app
COPY frontend/package.json frontend/yarn.lock ./
RUN yarn install --frozen-lockfile
COPY frontend/ .
ENV NUXT_PROXY_TARGET=http://email-api:8080
RUN yarn build

FROM node:22-alpine
WORKDIR /app
COPY --from=build /app/.output .
EXPOSE 3000
ENV NUXT_HOST=0.0.0.0
ENV NUXT_PORT=3000
CMD ["node", "server/index.mjs"]
```

- [ ] **Step 7: Create `docker-compose.yml`**

`innovayse-email/docker-compose.yml`:
```yaml
services:
  email-api:
    build:
      context: .
      dockerfile: docker/api.Dockerfile
    ports:
      - "4002:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - Sso__Authority=http://innovayse-sso-sso-api-1:8080
      - Sso__ClientId=innovayse-email
      - Hostpanel__ApiUrl=http://innovayse-hostpanel-api-1:5148
      - Mailcow__ApiUrl=${MAILCOW_API_URL:-}
      - Mailcow__ApiKey=${MAILCOW_API_KEY:-}
      - Imap__Host=${IMAP_HOST:-mail.innovayse.local}
      - Imap__Port=${IMAP_PORT:-993}
      - Imap__UseSsl=true
      - Smtp__Host=${SMTP_HOST:-mail.innovayse.local}
      - Smtp__Port=${SMTP_PORT:-587}
      - Smtp__UseSsl=true
      - Cors__AllowedOrigins__0=http://localhost:4003
      - EncryptionKey=${ENCRYPTION_KEY:-}
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
    ports:
      - "4003:3000"
    environment:
      - NUXT_PUBLIC_API_BASE=http://localhost:4002
      - NUXT_PROXY_TARGET=http://email-api:8080
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

- [ ] **Step 8: Create `.env.example`**

`innovayse-email/.env.example`:
```
MAILCOW_API_URL=https://mail.innovayse.local/api/v1
MAILCOW_API_KEY=
IMAP_HOST=mail.innovayse.local
IMAP_PORT=993
SMTP_HOST=mail.innovayse.local
SMTP_PORT=587
ENCRYPTION_KEY=
```

- [ ] **Step 9: Delete auto-generated Class1.cs files and verify build**

```bash
cd /home/innovayse/Desktop/innovayse-workspace/innovayse-email/backend
rm -f src/Innovayse.Email.Domain/Class1.cs src/Innovayse.Email.Application/Class1.cs src/Innovayse.Email.Infrastructure/Class1.cs
dotnet build
```

Expected: BUILD SUCCEEDED

- [ ] **Step 10: Commit**

```bash
cd /home/innovayse/Desktop/innovayse-workspace/innovayse-email
git add backend/ docker/ docker-compose.yml .env.example
git commit -m "feat: scaffold backend solution, Docker infrastructure"
```

---

### Task 3: Domain Layer — Models and Interfaces

**Files:**
- Create: All files under `backend/src/Innovayse.Email.Domain/`

**Interfaces:**
- Consumes: nothing
- Produces: `EmailMessage`, `EmailFolder`, `EmailAttachment`, `MailboxCredentials`, `QuotaInfo`, `FolderCount` records; `IImapService`, `ISmtpService`, `IMailboxCredentialProvider`, `IMailboxQuotaProvider` interfaces

- [ ] **Step 1: Create `EmailFolder` enum**

`backend/src/Innovayse.Email.Domain/Models/EmailFolder.cs`:
```csharp
namespace Innovayse.Email.Domain.Models;

public enum EmailFolder
{
    Inbox,
    Drafts,
    Sent,
    Archive,
    Junk,
    Templates,
    Trash
}
```

- [ ] **Step 2: Create `EmailAttachment` record**

`backend/src/Innovayse.Email.Domain/Models/EmailAttachment.cs`:
```csharp
namespace Innovayse.Email.Domain.Models;

public sealed record EmailAttachment(
    int Index,
    string Filename,
    string ContentType,
    long Size,
    string? ContentId);
```

- [ ] **Step 3: Create `EmailMessage` record**

`backend/src/Innovayse.Email.Domain/Models/EmailMessage.cs`:
```csharp
namespace Innovayse.Email.Domain.Models;

public sealed record EmailAddress(string Name, string Address);

public sealed record EmailMessageSummary(
    uint Uid,
    EmailFolder Folder,
    EmailAddress From,
    string Subject,
    string Snippet,
    DateTimeOffset Date,
    bool IsRead,
    bool HasAttachments);

public sealed record EmailMessageDetail(
    uint Uid,
    EmailFolder Folder,
    EmailAddress From,
    List<EmailAddress> To,
    List<EmailAddress> Cc,
    string Subject,
    string? BodyHtml,
    string? BodyPlain,
    DateTimeOffset Date,
    bool IsRead,
    bool HasAttachments,
    List<EmailAttachment> Attachments);
```

- [ ] **Step 4: Create `MailboxCredentials` record**

`backend/src/Innovayse.Email.Domain/Models/MailboxCredentials.cs`:
```csharp
namespace Innovayse.Email.Domain.Models;

public sealed record MailboxCredentials(
    string Email,
    string Password,
    string DisplayName,
    int QuotaMb,
    string ImapHost,
    int ImapPort,
    string SmtpHost,
    int SmtpPort);
```

- [ ] **Step 5: Create `QuotaInfo` and `FolderCount` records**

`backend/src/Innovayse.Email.Domain/Models/QuotaInfo.cs`:
```csharp
namespace Innovayse.Email.Domain.Models;

public sealed record QuotaInfo(long UsedBytes, long LimitBytes);
```

`backend/src/Innovayse.Email.Domain/Models/FolderCount.cs`:
```csharp
namespace Innovayse.Email.Domain.Models;

public sealed record FolderCount(EmailFolder Folder, int Unread, int Total);
```

- [ ] **Step 6: Create `IImapService` interface**

`backend/src/Innovayse.Email.Domain/Interfaces/IImapService.cs`:
```csharp
namespace Innovayse.Email.Domain.Interfaces;

using Innovayse.Email.Domain.Models;

public interface IImapService
{
    Task<List<EmailMessageSummary>> ListMessagesAsync(
        MailboxCredentials creds, EmailFolder folder, int page, int pageSize, CancellationToken ct);

    Task<EmailMessageDetail?> GetMessageAsync(
        MailboxCredentials creds, EmailFolder folder, uint uid, CancellationToken ct);

    Task<List<EmailMessageSummary>> SearchAsync(
        MailboxCredentials creds, string query, CancellationToken ct);

    Task MarkAsReadAsync(
        MailboxCredentials creds, EmailFolder folder, uint uid, CancellationToken ct);

    Task MarkAsUnreadAsync(
        MailboxCredentials creds, EmailFolder folder, uint uid, CancellationToken ct);

    Task MoveAsync(
        MailboxCredentials creds, EmailFolder sourceFolder, uint uid, EmailFolder targetFolder, CancellationToken ct);

    Task DeleteAsync(
        MailboxCredentials creds, EmailFolder folder, uint uid, CancellationToken ct);

    Task<uint> SaveDraftAsync(
        MailboxCredentials creds, string to, string subject, string body, uint? existingDraftUid, CancellationToken ct);

    Task<Stream> GetAttachmentAsync(
        MailboxCredentials creds, EmailFolder folder, uint uid, int attachmentIndex, CancellationToken ct);

    Task<List<FolderCount>> GetFolderCountsAsync(
        MailboxCredentials creds, CancellationToken ct);
}
```

- [ ] **Step 7: Create `ISmtpService` interface**

`backend/src/Innovayse.Email.Domain/Interfaces/ISmtpService.cs`:
```csharp
namespace Innovayse.Email.Domain.Interfaces;

using Innovayse.Email.Domain.Models;

public interface ISmtpService
{
    Task SendAsync(
        MailboxCredentials creds,
        List<string> to,
        List<string>? cc,
        string subject,
        string bodyHtml,
        List<(string Filename, string ContentType, Stream Content)>? attachments,
        CancellationToken ct);
}
```

- [ ] **Step 8: Create `IMailboxCredentialProvider` interface**

`backend/src/Innovayse.Email.Domain/Interfaces/IMailboxCredentialProvider.cs`:
```csharp
namespace Innovayse.Email.Domain.Interfaces;

using Innovayse.Email.Domain.Models;

public interface IMailboxCredentialProvider
{
    Task<List<MailboxCredentials>> GetMailboxesAsync(string accessToken, CancellationToken ct);
}
```

- [ ] **Step 9: Create `IMailboxQuotaProvider` interface**

`backend/src/Innovayse.Email.Domain/Interfaces/IMailboxQuotaProvider.cs`:
```csharp
namespace Innovayse.Email.Domain.Interfaces;

using Innovayse.Email.Domain.Models;

public interface IMailboxQuotaProvider
{
    Task<QuotaInfo> GetQuotaAsync(string mailboxEmail, CancellationToken ct);
}
```

- [ ] **Step 10: Verify build**

```bash
cd /home/innovayse/Desktop/innovayse-workspace/innovayse-email/backend
dotnet build
```

Expected: BUILD SUCCEEDED

- [ ] **Step 11: Commit**

```bash
cd /home/innovayse/Desktop/innovayse-workspace/innovayse-email
git add backend/src/Innovayse.Email.Domain/
git commit -m "feat: add domain layer models and interfaces"
```

---

### Task 4: Infrastructure Layer — IMAP, SMTP, Providers

**Files:**
- Create: All files under `backend/src/Innovayse.Email.Infrastructure/`

**Interfaces:**
- Consumes: `IImapService`, `ISmtpService`, `IMailboxCredentialProvider`, `IMailboxQuotaProvider` from Domain; `MailboxCredentials`, `EmailFolder`, `EmailMessageSummary`, `EmailMessageDetail` from Domain
- Produces: `ImapMailService`, `SmtpMailService`, `HostpanelCredentialProvider`, `MailcowQuotaProvider` implementations; `DependencyInjection.AddInfrastructure()` extension method

- [ ] **Step 1: Create settings classes**

`backend/src/Innovayse.Email.Infrastructure/Settings/ImapSettings.cs`:
```csharp
namespace Innovayse.Email.Infrastructure.Settings;

public sealed class ImapSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 993;
    public bool UseSsl { get; set; } = true;
}
```

`backend/src/Innovayse.Email.Infrastructure/Settings/SmtpSettings.cs`:
```csharp
namespace Innovayse.Email.Infrastructure.Settings;

public sealed class SmtpSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
}
```

`backend/src/Innovayse.Email.Infrastructure/Settings/HostpanelSettings.cs`:
```csharp
namespace Innovayse.Email.Infrastructure.Settings;

public sealed class HostpanelSettings
{
    public string ApiUrl { get; set; } = "";
}
```

`backend/src/Innovayse.Email.Infrastructure/Settings/MailcowSettings.cs`:
```csharp
namespace Innovayse.Email.Infrastructure.Settings;

public sealed class MailcowSettings
{
    public string ApiUrl { get; set; } = "";
    public string ApiKey { get; set; } = "";
}
```

- [ ] **Step 2: Create IMAP folder name mapping helper**

`backend/src/Innovayse.Email.Infrastructure/Imap/FolderMapping.cs`:
```csharp
namespace Innovayse.Email.Infrastructure.Imap;

using Innovayse.Email.Domain.Models;
using MailKit;

internal static class FolderMapping
{
    internal static readonly Dictionary<EmailFolder, SpecialFolder?> ToSpecial = new()
    {
        [EmailFolder.Inbox] = null, // Use client.Inbox
        [EmailFolder.Drafts] = SpecialFolder.Drafts,
        [EmailFolder.Sent] = SpecialFolder.Sent,
        [EmailFolder.Archive] = SpecialFolder.Archive,
        [EmailFolder.Junk] = SpecialFolder.Junk,
        [EmailFolder.Trash] = SpecialFolder.Trash,
        [EmailFolder.Templates] = null, // Custom folder by name
    };

    internal static readonly Dictionary<string, EmailFolder> FromName = new(StringComparer.OrdinalIgnoreCase)
    {
        ["INBOX"] = EmailFolder.Inbox,
        ["Drafts"] = EmailFolder.Drafts,
        ["Sent"] = EmailFolder.Sent,
        ["Archive"] = EmailFolder.Archive,
        ["Junk"] = EmailFolder.Junk,
        ["Templates"] = EmailFolder.Templates,
        ["Trash"] = EmailFolder.Trash,
    };
}
```

- [ ] **Step 3: Create `ImapMailService`**

`backend/src/Innovayse.Email.Infrastructure/Imap/ImapMailService.cs`:
```csharp
namespace Innovayse.Email.Infrastructure.Imap;

using Innovayse.Email.Domain.Interfaces;
using Innovayse.Email.Domain.Models;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using Microsoft.Extensions.Logging;
using MimeKit;

public sealed class ImapMailService(ILogger<ImapMailService> logger) : IImapService
{
    private async Task<ImapClient> ConnectAsync(MailboxCredentials creds, CancellationToken ct)
    {
        var client = new ImapClient();
        client.ServerCertificateValidationCallback = (s, c, h, e) => true; // Dev only — restrict in prod
        await client.ConnectAsync(creds.ImapHost, creds.ImapPort, MailKit.Security.SecureSocketOptions.SslOnConnect, ct);
        await client.AuthenticateAsync(creds.Email, creds.Password, ct);
        return client;
    }

    private async Task<IMailFolder> GetFolderAsync(ImapClient client, EmailFolder folder, CancellationToken ct)
    {
        if (folder == EmailFolder.Inbox)
            return client.Inbox;

        if (folder == EmailFolder.Templates)
        {
            var personal = client.GetFolder(client.PersonalNamespaces[0]);
            return await personal.GetSubfolderAsync("Templates", ct);
        }

        var special = FolderMapping.ToSpecial[folder];
        if (special.HasValue)
            return client.GetFolder(special.Value);

        throw new InvalidOperationException($"Unknown folder: {folder}");
    }

    public async Task<List<EmailMessageSummary>> ListMessagesAsync(
        MailboxCredentials creds, EmailFolder folder, int page, int pageSize, CancellationToken ct)
    {
        using var client = await ConnectAsync(creds, ct);
        var mailFolder = await GetFolderAsync(client, folder, ct);
        await mailFolder.OpenAsync(FolderAccess.ReadOnly, ct);

        var total = mailFolder.Count;
        var start = Math.Max(0, total - (page * pageSize));
        var end = Math.Max(0, total - ((page - 1) * pageSize) - 1);
        if (start > end || total == 0)
            return [];

        var summaries = await mailFolder.FetchAsync(start, end,
            MessageSummaryItems.UniqueId | MessageSummaryItems.Envelope |
            MessageSummaryItems.Flags | MessageSummaryItems.BodyStructure |
            MessageSummaryItems.PreviewText, ct);

        return summaries
            .OrderByDescending(s => s.Date)
            .Select(s => new EmailMessageSummary(
                s.UniqueId.Id,
                folder,
                new EmailAddress(
                    s.Envelope.From.Mailboxes.FirstOrDefault()?.Name ?? "",
                    s.Envelope.From.Mailboxes.FirstOrDefault()?.Address ?? ""),
                s.Envelope.Subject ?? "(no subject)",
                s.PreviewText ?? "",
                s.Date,
                s.Flags.HasValue && s.Flags.Value.HasFlag(MessageFlags.Seen),
                s.Body is BodyPartMultipart multi && multi.BodyParts.Any(p => p is BodyPartBasic b && b.IsAttachment)))
            .ToList();
    }

    public async Task<EmailMessageDetail?> GetMessageAsync(
        MailboxCredentials creds, EmailFolder folder, uint uid, CancellationToken ct)
    {
        using var client = await ConnectAsync(creds, ct);
        var mailFolder = await GetFolderAsync(client, folder, ct);
        await mailFolder.OpenAsync(FolderAccess.ReadWrite, ct);

        var message = await mailFolder.GetMessageAsync(new UniqueId(uid), ct);
        if (message is null) return null;

        // Mark as seen
        await mailFolder.AddFlagsAsync(new UniqueId(uid), MessageFlags.Seen, true, ct);

        var attachments = message.Attachments
            .Select((a, i) => new EmailAttachment(
                i,
                a.ContentDisposition?.FileName ?? a.ContentType.Name ?? $"attachment-{i}",
                a.ContentType.MimeType,
                a is MimePart mp ? mp.Content?.Stream?.Length ?? 0 : 0,
                a.ContentId))
            .ToList();

        return new EmailMessageDetail(
            uid, folder,
            new EmailAddress(
                message.From.Mailboxes.FirstOrDefault()?.Name ?? "",
                message.From.Mailboxes.FirstOrDefault()?.Address ?? ""),
            message.To.Mailboxes.Select(m => new EmailAddress(m.Name ?? "", m.Address)).ToList(),
            message.Cc.Mailboxes.Select(m => new EmailAddress(m.Name ?? "", m.Address)).ToList(),
            message.Subject ?? "(no subject)",
            message.HtmlBody,
            message.TextBody,
            message.Date,
            true, // just marked as seen
            attachments.Count > 0,
            attachments);
    }

    public async Task<List<EmailMessageSummary>> SearchAsync(
        MailboxCredentials creds, string query, CancellationToken ct)
    {
        using var client = await ConnectAsync(creds, ct);
        var results = new List<EmailMessageSummary>();

        foreach (var folderEnum in Enum.GetValues<EmailFolder>())
        {
            try
            {
                var mailFolder = await GetFolderAsync(client, folderEnum, ct);
                await mailFolder.OpenAsync(FolderAccess.ReadOnly, ct);

                var searchQuery = SearchQuery.Or(
                    SearchQuery.SubjectContains(query),
                    SearchQuery.Or(
                        SearchQuery.FromContains(query),
                        SearchQuery.BodyContains(query)));

                var uids = await mailFolder.SearchAsync(searchQuery, ct);
                if (uids.Count == 0) continue;

                var limitedUids = uids.TakeLast(50).ToList();
                var summaries = await mailFolder.FetchAsync(limitedUids,
                    MessageSummaryItems.UniqueId | MessageSummaryItems.Envelope |
                    MessageSummaryItems.Flags | MessageSummaryItems.PreviewText, ct);

                results.AddRange(summaries.Select(s => new EmailMessageSummary(
                    s.UniqueId.Id, folderEnum,
                    new EmailAddress(
                        s.Envelope.From.Mailboxes.FirstOrDefault()?.Name ?? "",
                        s.Envelope.From.Mailboxes.FirstOrDefault()?.Address ?? ""),
                    s.Envelope.Subject ?? "(no subject)",
                    s.PreviewText ?? "",
                    s.Date,
                    s.Flags.HasValue && s.Flags.Value.HasFlag(MessageFlags.Seen),
                    false)));
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to search folder {Folder}", folderEnum);
            }
        }

        return results.OrderByDescending(m => m.Date).Take(100).ToList();
    }

    public async Task MarkAsReadAsync(MailboxCredentials creds, EmailFolder folder, uint uid, CancellationToken ct)
    {
        using var client = await ConnectAsync(creds, ct);
        var mailFolder = await GetFolderAsync(client, folder, ct);
        await mailFolder.OpenAsync(FolderAccess.ReadWrite, ct);
        await mailFolder.AddFlagsAsync(new UniqueId(uid), MessageFlags.Seen, true, ct);
    }

    public async Task MarkAsUnreadAsync(MailboxCredentials creds, EmailFolder folder, uint uid, CancellationToken ct)
    {
        using var client = await ConnectAsync(creds, ct);
        var mailFolder = await GetFolderAsync(client, folder, ct);
        await mailFolder.OpenAsync(FolderAccess.ReadWrite, ct);
        await mailFolder.RemoveFlagsAsync(new UniqueId(uid), MessageFlags.Seen, true, ct);
    }

    public async Task MoveAsync(MailboxCredentials creds, EmailFolder sourceFolder, uint uid, EmailFolder targetFolder, CancellationToken ct)
    {
        using var client = await ConnectAsync(creds, ct);
        var source = await GetFolderAsync(client, sourceFolder, ct);
        var target = await GetFolderAsync(client, targetFolder, ct);
        await source.OpenAsync(FolderAccess.ReadWrite, ct);
        await source.MoveToAsync(new UniqueId(uid), target, ct);
    }

    public async Task DeleteAsync(MailboxCredentials creds, EmailFolder folder, uint uid, CancellationToken ct)
    {
        using var client = await ConnectAsync(creds, ct);
        var mailFolder = await GetFolderAsync(client, folder, ct);
        await mailFolder.OpenAsync(FolderAccess.ReadWrite, ct);

        if (folder == EmailFolder.Trash)
        {
            await mailFolder.AddFlagsAsync(new UniqueId(uid), MessageFlags.Deleted, true, ct);
            await mailFolder.ExpungeAsync(ct);
        }
        else
        {
            var trash = await GetFolderAsync(client, EmailFolder.Trash, ct);
            await mailFolder.MoveToAsync(new UniqueId(uid), trash, ct);
        }
    }

    public async Task<uint> SaveDraftAsync(MailboxCredentials creds, string to, string subject, string body, uint? existingDraftUid, CancellationToken ct)
    {
        using var client = await ConnectAsync(creds, ct);
        var drafts = await GetFolderAsync(client, EmailFolder.Drafts, ct);
        await drafts.OpenAsync(FolderAccess.ReadWrite, ct);

        if (existingDraftUid.HasValue)
        {
            await drafts.AddFlagsAsync(new UniqueId(existingDraftUid.Value), MessageFlags.Deleted, true, ct);
            await drafts.ExpungeAsync(ct);
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(creds.DisplayName, creds.Email));
        if (!string.IsNullOrWhiteSpace(to))
            message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject ?? "";
        message.Body = new TextPart("plain") { Text = body ?? "" };
        message.Headers.Add(HeaderId.XDraft, "true");

        var uid = await drafts.AppendAsync(message, MessageFlags.Draft | MessageFlags.Seen, ct);
        return uid?.Id ?? 0;
    }

    public async Task<Stream> GetAttachmentAsync(MailboxCredentials creds, EmailFolder folder, uint uid, int attachmentIndex, CancellationToken ct)
    {
        using var client = await ConnectAsync(creds, ct);
        var mailFolder = await GetFolderAsync(client, folder, ct);
        await mailFolder.OpenAsync(FolderAccess.ReadOnly, ct);

        var message = await mailFolder.GetMessageAsync(new UniqueId(uid), ct);
        var attachment = message.Attachments.ElementAtOrDefault(attachmentIndex)
            ?? throw new InvalidOperationException("Attachment not found");

        var stream = new MemoryStream();
        if (attachment is MimePart part)
            await part.Content.DecodeToAsync(stream, ct);
        stream.Position = 0;
        return stream;
    }

    public async Task<List<FolderCount>> GetFolderCountsAsync(MailboxCredentials creds, CancellationToken ct)
    {
        using var client = await ConnectAsync(creds, ct);
        var counts = new List<FolderCount>();

        foreach (var folderEnum in Enum.GetValues<EmailFolder>())
        {
            try
            {
                var mailFolder = await GetFolderAsync(client, folderEnum, ct);
                await mailFolder.OpenAsync(FolderAccess.ReadOnly, ct);
                var unread = mailFolder.Unread;
                var total = mailFolder.Count;
                counts.Add(new FolderCount(folderEnum, unread, total));
            }
            catch
            {
                counts.Add(new FolderCount(folderEnum, 0, 0));
            }
        }

        return counts;
    }
}
```

- [ ] **Step 4: Create `SmtpMailService`**

`backend/src/Innovayse.Email.Infrastructure/Smtp/SmtpMailService.cs`:
```csharp
namespace Innovayse.Email.Infrastructure.Smtp;

using Innovayse.Email.Domain.Interfaces;
using Innovayse.Email.Domain.Models;
using MailKit.Net.Smtp;
using MimeKit;

public sealed class SmtpMailService : ISmtpService
{
    public async Task SendAsync(
        MailboxCredentials creds,
        List<string> to,
        List<string>? cc,
        string subject,
        string bodyHtml,
        List<(string Filename, string ContentType, Stream Content)>? attachments,
        CancellationToken ct)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(creds.DisplayName, creds.Email));
        foreach (var addr in to)
            message.To.Add(MailboxAddress.Parse(addr));
        if (cc is not null)
            foreach (var addr in cc)
                message.Cc.Add(MailboxAddress.Parse(addr));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = bodyHtml };
        if (attachments is not null)
        {
            foreach (var (filename, contentType, content) in attachments)
            {
                var ct2 = ContentType.Parse(contentType);
                await bodyBuilder.Attachments.AddAsync(filename, content, ct2, ct);
            }
        }
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        client.ServerCertificateValidationCallback = (s, c, h, e) => true;
        await client.ConnectAsync(creds.SmtpHost, creds.SmtpPort, MailKit.Security.SecureSocketOptions.StartTls, ct);
        await client.AuthenticateAsync(creds.Email, creds.Password, ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);

        // Copy to Sent folder via IMAP
        using var imap = new MailKit.Net.Imap.ImapClient();
        imap.ServerCertificateValidationCallback = (s, c, h, e) => true;
        await imap.ConnectAsync(creds.ImapHost, creds.ImapPort, MailKit.Security.SecureSocketOptions.SslOnConnect, ct);
        await imap.AuthenticateAsync(creds.Email, creds.Password, ct);
        var sent = imap.GetFolder(MailKit.SpecialFolder.Sent);
        await sent.OpenAsync(MailKit.FolderAccess.ReadWrite, ct);
        await sent.AppendAsync(message, MailKit.MessageFlags.Seen, ct);
        await imap.DisconnectAsync(true, ct);
    }
}
```

- [ ] **Step 5: Create `HostpanelCredentialProvider`**

`backend/src/Innovayse.Email.Infrastructure/Providers/HostpanelCredentialProvider.cs`:
```csharp
namespace Innovayse.Email.Infrastructure.Providers;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Innovayse.Email.Domain.Interfaces;
using Innovayse.Email.Domain.Models;
using Microsoft.Extensions.Configuration;

public sealed class HostpanelCredentialProvider(
    HttpClient httpClient,
    IConfiguration config) : IMailboxCredentialProvider
{
    public async Task<List<MailboxCredentials>> GetMailboxesAsync(string accessToken, CancellationToken ct)
    {
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await httpClient.GetAsync("/api/portal/client/email/mailboxes/credentials", ct);
        response.EnsureSuccessStatusCode();

        var items = await response.Content.ReadFromJsonAsync<List<CredentialDto>>(ct) ?? [];
        var encryptionKey = config["EncryptionKey"] ?? "";

        return items.Select(i => new MailboxCredentials(
            i.Email,
            DecryptPassword(i.EncryptedPassword, encryptionKey),
            i.DisplayName,
            i.QuotaMb,
            i.ImapHost,
            i.ImapPort,
            i.SmtpHost,
            i.SmtpPort
        )).ToList();
    }

    private static string DecryptPassword(string encrypted, string base64Key)
    {
        if (string.IsNullOrEmpty(base64Key) || string.IsNullOrEmpty(encrypted))
            return encrypted;

        var key = Convert.FromBase64String(base64Key);
        var fullBytes = Convert.FromBase64String(encrypted);
        if (fullBytes.Length < 17) return encrypted;

        using var aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.IV = fullBytes[..16];
        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(fullBytes, 16, fullBytes.Length - 16);
        return Encoding.UTF8.GetString(plainBytes);
    }

    private sealed record CredentialDto(
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("encryptedPassword")] string EncryptedPassword,
        [property: JsonPropertyName("displayName")] string DisplayName,
        [property: JsonPropertyName("quotaMb")] int QuotaMb,
        [property: JsonPropertyName("imapHost")] string ImapHost,
        [property: JsonPropertyName("imapPort")] int ImapPort,
        [property: JsonPropertyName("smtpHost")] string SmtpHost,
        [property: JsonPropertyName("smtpPort")] int SmtpPort);
}
```

- [ ] **Step 6: Create `MailcowQuotaProvider`**

`backend/src/Innovayse.Email.Infrastructure/Providers/MailcowQuotaProvider.cs`:
```csharp
namespace Innovayse.Email.Infrastructure.Providers;

using System.Net.Http.Json;
using System.Text.Json;
using Innovayse.Email.Domain.Interfaces;
using Innovayse.Email.Domain.Models;
using Innovayse.Email.Infrastructure.Settings;
using Microsoft.Extensions.Options;

public sealed class MailcowQuotaProvider(
    HttpClient httpClient,
    IOptions<MailcowSettings> settings) : IMailboxQuotaProvider
{
    public async Task<QuotaInfo> GetQuotaAsync(string mailboxEmail, CancellationToken ct)
    {
        var opts = settings.Value;
        if (string.IsNullOrEmpty(opts.ApiUrl) || string.IsNullOrEmpty(opts.ApiKey))
            return new QuotaInfo(0, 0);

        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-API-Key", opts.ApiKey);
        var response = await httpClient.GetAsync(
            $"{opts.ApiUrl}/api/v1/get/mailbox/{Uri.EscapeDataString(mailboxEmail)}", ct);

        if (!response.IsSuccessStatusCode)
            return new QuotaInfo(0, 0);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        var usedBytes = json.TryGetProperty("quota_used", out var used) ? used.GetInt64() : 0;
        var limitBytes = json.TryGetProperty("quota", out var limit) ? limit.GetInt64() * 1024 * 1024 : 0;

        return new QuotaInfo(usedBytes, limitBytes);
    }
}
```

- [ ] **Step 7: Create `DependencyInjection.cs`**

`backend/src/Innovayse.Email.Infrastructure/DependencyInjection.cs`:
```csharp
namespace Innovayse.Email.Infrastructure;

using Innovayse.Email.Domain.Interfaces;
using Innovayse.Email.Infrastructure.Imap;
using Innovayse.Email.Infrastructure.Providers;
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
        services.Configure<HostpanelSettings>(config.GetSection("Hostpanel"));
        services.Configure<MailcowSettings>(config.GetSection("Mailcow"));

        services.AddScoped<IImapService, ImapMailService>();
        services.AddScoped<ISmtpService, SmtpMailService>();

        services.AddHttpClient<IMailboxCredentialProvider, HostpanelCredentialProvider>((sp, client) =>
        {
            var hostpanelUrl = config["Hostpanel:ApiUrl"] ?? "http://localhost:5148";
            client.BaseAddress = new Uri(hostpanelUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        });

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

- [ ] **Step 8: Verify build**

```bash
cd /home/innovayse/Desktop/innovayse-workspace/innovayse-email/backend
dotnet build
```

Expected: BUILD SUCCEEDED

- [ ] **Step 9: Commit**

```bash
cd /home/innovayse/Desktop/innovayse-workspace/innovayse-email
git add backend/src/Innovayse.Email.Infrastructure/
git commit -m "feat: add infrastructure layer — IMAP, SMTP, credential and quota providers"
```

---

### Task 5: Application Layer — CQRS Handlers

**Files:**
- Create: All files under `backend/src/Innovayse.Email.Application/`

**Interfaces:**
- Consumes: `IImapService`, `ISmtpService`, `IMailboxCredentialProvider`, `IMailboxQuotaProvider` from Domain
- Produces: All query/command handlers + `DependencyInjection.AddApplication()` extension method

- [ ] **Step 1: Create `MailboxSession` — a scoped service holding the active mailbox**

`backend/src/Innovayse.Email.Application/Session/MailboxSessionHolder.cs`:
```csharp
namespace Innovayse.Email.Application.Session;

using Innovayse.Email.Domain.Models;

public sealed class MailboxSessionHolder
{
    public MailboxCredentials? ActiveMailbox { get; set; }
    public string? AccessToken { get; set; }
}
```

- [ ] **Step 2: Create `ListMailboxesQuery` + handler**

`backend/src/Innovayse.Email.Application/Mailboxes/Queries/ListMailboxes/ListMailboxesQuery.cs`:
```csharp
namespace Innovayse.Email.Application.Mailboxes.Queries.ListMailboxes;

public sealed record ListMailboxesQuery(string AccessToken);
```

`backend/src/Innovayse.Email.Application/Mailboxes/Queries/ListMailboxes/ListMailboxesHandler.cs`:
```csharp
namespace Innovayse.Email.Application.Mailboxes.Queries.ListMailboxes;

using Innovayse.Email.Domain.Interfaces;
using Innovayse.Email.Domain.Models;

public sealed class ListMailboxesHandler(IMailboxCredentialProvider provider)
{
    public async Task<List<MailboxCredentials>> HandleAsync(ListMailboxesQuery query, CancellationToken ct)
        => await provider.GetMailboxesAsync(query.AccessToken, ct);
}
```

- [ ] **Step 3: Create message query handlers**

`backend/src/Innovayse.Email.Application/Messages/Queries/ListMessages/ListMessagesQuery.cs`:
```csharp
namespace Innovayse.Email.Application.Messages.Queries.ListMessages;

using Innovayse.Email.Domain.Models;

public sealed record ListMessagesQuery(EmailFolder Folder, int Page = 1, int PageSize = 50);
```

`backend/src/Innovayse.Email.Application/Messages/Queries/ListMessages/ListMessagesHandler.cs`:
```csharp
namespace Innovayse.Email.Application.Messages.Queries.ListMessages;

using Innovayse.Email.Application.Session;
using Innovayse.Email.Domain.Interfaces;
using Innovayse.Email.Domain.Models;

public sealed class ListMessagesHandler(IImapService imap, MailboxSessionHolder session)
{
    public async Task<List<EmailMessageSummary>> HandleAsync(ListMessagesQuery query, CancellationToken ct)
    {
        var creds = session.ActiveMailbox ?? throw new InvalidOperationException("No mailbox selected");
        return await imap.ListMessagesAsync(creds, query.Folder, query.Page, query.PageSize, ct);
    }
}
```

`backend/src/Innovayse.Email.Application/Messages/Queries/GetMessage/GetMessageQuery.cs`:
```csharp
namespace Innovayse.Email.Application.Messages.Queries.GetMessage;

using Innovayse.Email.Domain.Models;

public sealed record GetMessageQuery(EmailFolder Folder, uint Uid);
```

`backend/src/Innovayse.Email.Application/Messages/Queries/GetMessage/GetMessageHandler.cs`:
```csharp
namespace Innovayse.Email.Application.Messages.Queries.GetMessage;

using Innovayse.Email.Application.Session;
using Innovayse.Email.Domain.Interfaces;
using Innovayse.Email.Domain.Models;

public sealed class GetMessageHandler(IImapService imap, MailboxSessionHolder session)
{
    public async Task<EmailMessageDetail?> HandleAsync(GetMessageQuery query, CancellationToken ct)
    {
        var creds = session.ActiveMailbox ?? throw new InvalidOperationException("No mailbox selected");
        return await imap.GetMessageAsync(creds, query.Folder, query.Uid, ct);
    }
}
```

`backend/src/Innovayse.Email.Application/Messages/Queries/SearchMessages/SearchMessagesQuery.cs`:
```csharp
namespace Innovayse.Email.Application.Messages.Queries.SearchMessages;

public sealed record SearchMessagesQuery(string Query);
```

`backend/src/Innovayse.Email.Application/Messages/Queries/SearchMessages/SearchMessagesHandler.cs`:
```csharp
namespace Innovayse.Email.Application.Messages.Queries.SearchMessages;

using Innovayse.Email.Application.Session;
using Innovayse.Email.Domain.Interfaces;
using Innovayse.Email.Domain.Models;

public sealed class SearchMessagesHandler(IImapService imap, MailboxSessionHolder session)
{
    public async Task<List<EmailMessageSummary>> HandleAsync(SearchMessagesQuery query, CancellationToken ct)
    {
        var creds = session.ActiveMailbox ?? throw new InvalidOperationException("No mailbox selected");
        return await imap.SearchAsync(creds, query.Query, ct);
    }
}
```

`backend/src/Innovayse.Email.Application/Messages/Queries/GetAttachment/GetAttachmentQuery.cs`:
```csharp
namespace Innovayse.Email.Application.Messages.Queries.GetAttachment;

using Innovayse.Email.Domain.Models;

public sealed record GetAttachmentQuery(EmailFolder Folder, uint Uid, int AttachmentIndex);
```

`backend/src/Innovayse.Email.Application/Messages/Queries/GetAttachment/GetAttachmentHandler.cs`:
```csharp
namespace Innovayse.Email.Application.Messages.Queries.GetAttachment;

using Innovayse.Email.Application.Session;
using Innovayse.Email.Domain.Interfaces;

public sealed class GetAttachmentHandler(IImapService imap, MailboxSessionHolder session)
{
    public async Task<Stream> HandleAsync(GetAttachmentQuery query, CancellationToken ct)
    {
        var creds = session.ActiveMailbox ?? throw new InvalidOperationException("No mailbox selected");
        return await imap.GetAttachmentAsync(creds, query.Folder, query.Uid, query.AttachmentIndex, ct);
    }
}
```

`backend/src/Innovayse.Email.Application/Messages/Queries/GetFolderCounts/GetFolderCountsQuery.cs`:
```csharp
namespace Innovayse.Email.Application.Messages.Queries.GetFolderCounts;

public sealed record GetFolderCountsQuery;
```

`backend/src/Innovayse.Email.Application/Messages/Queries/GetFolderCounts/GetFolderCountsHandler.cs`:
```csharp
namespace Innovayse.Email.Application.Messages.Queries.GetFolderCounts;

using Innovayse.Email.Application.Session;
using Innovayse.Email.Domain.Interfaces;
using Innovayse.Email.Domain.Models;

public sealed class GetFolderCountsHandler(IImapService imap, MailboxSessionHolder session)
{
    public async Task<List<FolderCount>> HandleAsync(GetFolderCountsQuery query, CancellationToken ct)
    {
        var creds = session.ActiveMailbox ?? throw new InvalidOperationException("No mailbox selected");
        return await imap.GetFolderCountsAsync(creds, ct);
    }
}
```

- [ ] **Step 4: Create message command handlers**

`backend/src/Innovayse.Email.Application/Messages/Commands/SendMessage/SendMessageCommand.cs`:
```csharp
namespace Innovayse.Email.Application.Messages.Commands.SendMessage;

using Microsoft.AspNetCore.Http;

public sealed record SendMessageCommand(
    List<string> To,
    List<string>? Cc,
    string Subject,
    string BodyHtml,
    List<IFormFile>? Attachments);
```

`backend/src/Innovayse.Email.Application/Messages/Commands/SendMessage/SendMessageHandler.cs`:
```csharp
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
```

`backend/src/Innovayse.Email.Application/Messages/Commands/SaveDraft/SaveDraftCommand.cs`:
```csharp
namespace Innovayse.Email.Application.Messages.Commands.SaveDraft;

public sealed record SaveDraftCommand(string To, string Subject, string Body, uint? ExistingDraftUid);
```

`backend/src/Innovayse.Email.Application/Messages/Commands/SaveDraft/SaveDraftHandler.cs`:
```csharp
namespace Innovayse.Email.Application.Messages.Commands.SaveDraft;

using Innovayse.Email.Application.Session;
using Innovayse.Email.Domain.Interfaces;

public sealed class SaveDraftHandler(IImapService imap, MailboxSessionHolder session)
{
    public async Task<uint> HandleAsync(SaveDraftCommand cmd, CancellationToken ct)
    {
        var creds = session.ActiveMailbox ?? throw new InvalidOperationException("No mailbox selected");
        return await imap.SaveDraftAsync(creds, cmd.To, cmd.Subject, cmd.Body, cmd.ExistingDraftUid, ct);
    }
}
```

`backend/src/Innovayse.Email.Application/Messages/Commands/DeleteMessage/DeleteMessageCommand.cs`:
```csharp
namespace Innovayse.Email.Application.Messages.Commands.DeleteMessage;

using Innovayse.Email.Domain.Models;

public sealed record DeleteMessageCommand(EmailFolder Folder, uint Uid);
```

`backend/src/Innovayse.Email.Application/Messages/Commands/DeleteMessage/DeleteMessageHandler.cs`:
```csharp
namespace Innovayse.Email.Application.Messages.Commands.DeleteMessage;

using Innovayse.Email.Application.Session;
using Innovayse.Email.Domain.Interfaces;

public sealed class DeleteMessageHandler(IImapService imap, MailboxSessionHolder session)
{
    public async Task HandleAsync(DeleteMessageCommand cmd, CancellationToken ct)
    {
        var creds = session.ActiveMailbox ?? throw new InvalidOperationException("No mailbox selected");
        await imap.DeleteAsync(creds, cmd.Folder, cmd.Uid, ct);
    }
}
```

`backend/src/Innovayse.Email.Application/Messages/Commands/MoveMessage/MoveMessageCommand.cs`:
```csharp
namespace Innovayse.Email.Application.Messages.Commands.MoveMessage;

using Innovayse.Email.Domain.Models;

public sealed record MoveMessageCommand(EmailFolder SourceFolder, uint Uid, EmailFolder TargetFolder);
```

`backend/src/Innovayse.Email.Application/Messages/Commands/MoveMessage/MoveMessageHandler.cs`:
```csharp
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
```

`backend/src/Innovayse.Email.Application/Messages/Commands/MarkAsRead/MarkAsReadCommand.cs`:
```csharp
namespace Innovayse.Email.Application.Messages.Commands.MarkAsRead;

using Innovayse.Email.Domain.Models;

public sealed record MarkAsReadCommand(EmailFolder Folder, uint Uid);
public sealed record MarkAsUnreadCommand(EmailFolder Folder, uint Uid);
```

`backend/src/Innovayse.Email.Application/Messages/Commands/MarkAsRead/MarkAsReadHandler.cs`:
```csharp
namespace Innovayse.Email.Application.Messages.Commands.MarkAsRead;

using Innovayse.Email.Application.Session;
using Innovayse.Email.Domain.Interfaces;

public sealed class MarkAsReadHandler(IImapService imap, MailboxSessionHolder session)
{
    public async Task HandleReadAsync(MarkAsReadCommand cmd, CancellationToken ct)
    {
        var creds = session.ActiveMailbox ?? throw new InvalidOperationException("No mailbox selected");
        await imap.MarkAsReadAsync(creds, cmd.Folder, cmd.Uid, ct);
    }

    public async Task HandleUnreadAsync(MarkAsUnreadCommand cmd, CancellationToken ct)
    {
        var creds = session.ActiveMailbox ?? throw new InvalidOperationException("No mailbox selected");
        await imap.MarkAsUnreadAsync(creds, cmd.Folder, cmd.Uid, ct);
    }
}
```

- [ ] **Step 5: Create compose/template + quota handlers**

`backend/src/Innovayse.Email.Application/Compose/Queries/GetTemplate/GetTemplateQuery.cs`:
```csharp
namespace Innovayse.Email.Application.Compose.Queries.GetTemplate;

public sealed record GetTemplateQuery(uint Uid);
```

`backend/src/Innovayse.Email.Application/Compose/Queries/GetTemplate/GetTemplateHandler.cs`:
```csharp
namespace Innovayse.Email.Application.Compose.Queries.GetTemplate;

using Innovayse.Email.Application.Session;
using Innovayse.Email.Domain.Interfaces;
using Innovayse.Email.Domain.Models;

public sealed class GetTemplateHandler(IImapService imap, MailboxSessionHolder session)
{
    public async Task<EmailMessageDetail?> HandleAsync(GetTemplateQuery query, CancellationToken ct)
    {
        var creds = session.ActiveMailbox ?? throw new InvalidOperationException("No mailbox selected");
        return await imap.GetMessageAsync(creds, EmailFolder.Templates, query.Uid, ct);
    }
}
```

`backend/src/Innovayse.Email.Application/Quota/Queries/GetQuota/GetQuotaQuery.cs`:
```csharp
namespace Innovayse.Email.Application.Quota.Queries.GetQuota;

public sealed record GetQuotaQuery(string MailboxEmail);
```

`backend/src/Innovayse.Email.Application/Quota/Queries/GetQuota/GetQuotaHandler.cs`:
```csharp
namespace Innovayse.Email.Application.Quota.Queries.GetQuota;

using Innovayse.Email.Domain.Interfaces;
using Innovayse.Email.Domain.Models;

public sealed class GetQuotaHandler(IMailboxQuotaProvider provider)
{
    public async Task<QuotaInfo> HandleAsync(GetQuotaQuery query, CancellationToken ct)
        => await provider.GetQuotaAsync(query.MailboxEmail, ct);
}
```

- [ ] **Step 6: Create `DependencyInjection.cs`**

`backend/src/Innovayse.Email.Application/DependencyInjection.cs`:
```csharp
namespace Innovayse.Email.Application;

using Innovayse.Email.Application.Compose.Queries.GetTemplate;
using Innovayse.Email.Application.Mailboxes.Queries.ListMailboxes;
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
        services.AddScoped<ListMailboxesHandler>();
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

- [ ] **Step 7: Verify build**

```bash
cd /home/innovayse/Desktop/innovayse-workspace/innovayse-email/backend
dotnet build
```

Expected: BUILD SUCCEEDED

- [ ] **Step 8: Commit**

```bash
cd /home/innovayse/Desktop/innovayse-workspace/innovayse-email
git add backend/src/Innovayse.Email.Application/
git commit -m "feat: add application layer CQRS handlers"
```

---

### Task 6: API Layer — Program.cs, Middleware, Controllers

**Files:**
- Create: All files under `backend/src/Innovayse.Email.API/`

**Interfaces:**
- Consumes: All application layer handlers, `MailboxSessionHolder`
- Produces: REST API endpoints, SSO JWT auth, mailbox session middleware

This task creates the full API layer. Due to its size, the implementation is split across Program.cs, middleware, and 5 controllers. Refer to the design spec for endpoint signatures.

- [ ] **Step 1: Create `Program.cs`**

Create the file at `backend/src/Innovayse.Email.API/Program.cs` with:
- Serilog logging
- SSO JWT Bearer auth (same pattern as hostpanel SSO mode — `MapInboundClaims = false`, `NameClaimType = "sub"`)
- CORS from config
- `AddApplication()` + `AddInfrastructure()` DI calls
- Health check endpoint
- Controller mapping

- [ ] **Step 2: Create `MailboxSessionMiddleware`**

Reads the `X-Mailbox` header (or cookie), looks up credentials from `IMailboxCredentialProvider` using the bearer token, and populates `MailboxSessionHolder.ActiveMailbox` for the request scope.

- [ ] **Step 3: Create `MailboxController`** — GET /api/mailboxes, POST /api/mailboxes/select, GET /api/mailboxes/active

- [ ] **Step 4: Create `MessageController`** — GET /api/messages, GET /api/messages/{folder}/{uid}, POST /api/messages/search, PUT read/unread, POST move, DELETE, GET counts

- [ ] **Step 5: Create `ComposeController`** — POST /api/compose/send (multipart), POST /api/compose/draft, GET /api/compose/template/{uid}

- [ ] **Step 6: Create `AttachmentController`** — GET /api/attachments/{folder}/{uid}/{index}

- [ ] **Step 7: Create `QuotaController`** — GET /api/quota

- [ ] **Step 8: Verify build + commit**

```bash
cd /home/innovayse/Desktop/innovayse-workspace/innovayse-email/backend
dotnet build
```

```bash
cd /home/innovayse/Desktop/innovayse-workspace/innovayse-email
git add backend/src/Innovayse.Email.API/
git commit -m "feat: add API layer — controllers, auth, session middleware"
```

---

### Task 7: Frontend Scaffolding — Nuxt Config, BFF Routes, Styles

**Files:**
- Create: All frontend config files, BFF proxy route, global CSS

**Interfaces:**
- Consumes: Email API backend at `http://email-api:8080`
- Produces: Running Nuxt dev server, API proxy, global design tokens

- [ ] **Step 1: Create `package.json`**

```json
{
  "name": "innovayse-email-frontend",
  "private": true,
  "scripts": {
    "dev": "nuxt dev --port 4003",
    "build": "nuxt build",
    "preview": "nuxt preview"
  },
  "dependencies": {
    "nuxt": "^4.3.0",
    "vue": "^3.5.27",
    "@nuxtjs/tailwindcss": "^6.12.0"
  },
  "devDependencies": {
    "typescript": "~5.8.0"
  }
}
```

- [ ] **Step 2: Create `nuxt.config.ts`**

```typescript
export default defineNuxtConfig({
  compatibilityDate: '2025-01-01',
  modules: ['@nuxtjs/tailwindcss'],
  runtimeConfig: {
    apiProxyTarget: process.env.NUXT_PROXY_TARGET ?? 'http://localhost:4002',
    public: {
      apiBase: process.env.NUXT_PUBLIC_API_BASE ?? 'http://localhost:4002',
    },
  },
})
```

- [ ] **Step 3: Create `app.vue`**

Same as SSO — loads Manrope font, sets `dark` class on html, renders `<NuxtPage />`.

- [ ] **Step 4: Create `tailwind.config.ts`**

Same as SSO config with Manrope font and sky/purple color palette.

- [ ] **Step 5: Create `.gitignore`**

```
node_modules
.nuxt
.output
dist
```

- [ ] **Step 6: Create BFF proxy route `server/routes/api/[...path].ts`**

Same pattern as SSO — forwards all requests to `apiProxyTarget`, strips host header, forwards cookies with 'secure' flag removed for local dev.

- [ ] **Step 7: Create `assets/css/mail.css`**

All design tokens from `reference.html` as CSS custom properties + global styles (scrollbar, selection, keyframe animations, dot grid, glow blobs). This is the master stylesheet for the webmail UI.

- [ ] **Step 8: Install dependencies and verify dev server starts**

```bash
cd /home/innovayse/Desktop/innovayse-workspace/innovayse-email/frontend
yarn install
yarn dev
```

Expected: Nuxt dev server starts on port 4003

- [ ] **Step 9: Commit**

```bash
cd /home/innovayse/Desktop/innovayse-workspace/innovayse-email
git add frontend/
git commit -m "feat: scaffold frontend — Nuxt config, BFF proxy, design tokens"
```

---

### Task 8: Frontend Composables

**Files:**
- Create: `frontend/composables/useMailbox.ts`, `useMail.ts`, `useCompose.ts`

**Interfaces:**
- Consumes: BFF API proxy at `/api/*`
- Produces: `useMailbox()`, `useMail()`, `useCompose()` composables used by all pages/components

- [ ] **Step 1: Create `useMailbox.ts`**

State management for mailbox selection: `fetchMailboxes()`, `selectMailbox(email)`, `activeMailbox` ref, cookie persistence.

- [ ] **Step 2: Create `useMail.ts`**

State management for email list/reading: `messages` ref, `selectedMessage` ref, `currentFolder` ref, `folderCounts` ref, `searchQuery` ref, `fetchMessages()`, `selectMessage(uid)`, `switchFolder()`, `search()`, 20s polling interval, pagination.

- [ ] **Step 3: Create `useCompose.ts`**

Compose modal state: `isOpen` ref, `openCompose()`, `closeCompose()`, `sendMessage()`, `saveDraft()`, `discardDraft()`, `insertTemplate(uid)`, `toastMessage` ref, `showToast()`.

- [ ] **Step 4: Commit**

```bash
cd /home/innovayse/Desktop/innovayse-workspace/innovayse-email
git add frontend/composables/
git commit -m "feat: add frontend composables — mailbox, mail, compose"
```

---

### Task 9: Frontend — Pages and All Components

**Files:**
- Create: 3 pages + 14 components under `frontend/`

**Interfaces:**
- Consumes: `useMailbox()`, `useMail()`, `useCompose()` composables
- Produces: Complete webmail UI matching `reference.html` pixel-close

This is the largest frontend task. All visual code comes from `docs/reference.html` — port the exact CSS values, animations, and layout structure into Vue components.

- [ ] **Step 1: Create `pages/index.vue`**

Redirect logic: if not authenticated → SSO; if no mailbox selected → `/select-mailbox`; otherwise → `/mail`.

- [ ] **Step 2: Create `pages/select-mailbox.vue`**

Glassmorphic card grid showing available mailboxes. Uses same `AuthLayout`-style background (dot grid, glow blobs, particles). Each card shows email, display name, quota. Click to select and navigate to `/mail`.

- [ ] **Step 3: Create `components/SidebarParticles.vue`**

Port the `#sidebar-particles` canvas script from `reference.html` — non-interactive drifting particles with connecting lines, accent colors, slow drift (no mouse interaction). Same code pattern as SSO's `ParticleCanvas.vue` but without mouse tracking and scoped to sidebar dimensions.

- [ ] **Step 4: Create `components/GradientAvatar.vue`**

Reusable initials avatar with per-sender gradient. Props: `name` (string), `size` (number, default 38). Computes initials from name, generates a deterministic gradient from the name string (hash-based color selection from a curated palette).

- [ ] **Step 5: Create `components/ToastNotification.vue`**

Fixed-position toast at bottom center. Props: `message` (string), `visible` (boolean). Auto-dismisses after 2.4s. Uses `toastIn` animation from reference.

- [ ] **Step 6: Create `components/MailSidebar.vue`**

Port sidebar section from `reference.html`:
- Brand mark + "Innovayse Mail" wordmark
- Profile card (avatar, name, email, settings icon)
- Compose button (gradient, hover lift)
- Folder nav list with icons + unread badge on Inbox
- Storage usage bar at bottom
- `SidebarParticles` canvas behind all content

- [ ] **Step 7: Create `components/TopBar.vue`**

Port topbar from `reference.html`: current date (left), icon buttons for calendar/contacts/settings/history (right), last one accent-filled.

- [ ] **Step 8: Create `components/MessageRow.vue`**

Single email row component. Props: `message` (EmailMessageSummary), `selected` (boolean). Shows gradient avatar, sender, time, subject, preview, unread dot. Selected state: tinted background + accent border.

- [ ] **Step 9: Create `components/MessageList.vue`**

Message list panel: folder title, filter/refresh icons, search input, scrollable list of `MessageRow` components. Empty folder state. Uses `useMail()` composable for data.

- [ ] **Step 10: Create `components/AttachmentItem.vue`**

Attachment display: file icon, filename, size, download button. Props: `attachment` (EmailAttachment), `folder` (string), `uid` (number).

- [ ] **Step 11: Create `components/ReadingPane.vue`**

Reading pane: "Back to inbox" link, subject header, sender avatar/name/email/time, action icons (reply/forward/archive/delete), HTML body rendering, attachment list. Empty state when no message selected.

- [ ] **Step 12: Create `components/ComposeModal.vue`**

Compose modal overlay: glass card with ambient glow, To/Cc/Subject fields, formatting toolbar, body textarea, attachment upload, footer (discard, save draft, send). Template insertion support.

- [ ] **Step 13: Create `components/MailboxPicker.vue`**

Card component for select-mailbox page. Props: `mailbox` (MailboxCredentials). Shows email, display name, quota bar.

- [ ] **Step 14: Create `pages/mail.vue`**

The main webmail page composing all components:
- Dot grid + glow background
- `MailSidebar` (fixed 224px)
- Main column: `TopBar` + main row (`MessageList` + `ReadingPane`)
- `ComposeModal` (overlay)
- `ToastNotification`
- Reading glow blob with reactive bloom
- Gmail-style list/pane collapse animation (list width transitions between 100% and 340px)

- [ ] **Step 15: Verify dev server renders the UI**

```bash
cd /home/innovayse/Desktop/innovayse-workspace/innovayse-email/frontend
yarn dev
```

Open `http://localhost:4003/mail` and verify the layout matches `reference.html`.

- [ ] **Step 16: Commit**

```bash
cd /home/innovayse/Desktop/innovayse-workspace/innovayse-email
git add frontend/pages/ frontend/components/
git commit -m "feat: add webmail UI — pages, sidebar, message list, reading pane, compose modal"
```

---

### Task 10: SSO Client Registration + Docker Build

**Files:**
- Modify: `innovayse-sso/backend/src/Innovayse.SSO.API/Seed/SsoSeeder.cs`

**Interfaces:**
- Consumes: OpenIddict client registration API
- Produces: `innovayse-email` OIDC client registered in SSO

- [ ] **Step 1: Read SsoSeeder.cs**

```bash
cat /home/innovayse/Desktop/innovayse-workspace/innovayse-sso/backend/src/Innovayse.SSO.API/Seed/SsoSeeder.cs
```

- [ ] **Step 2: Add `innovayse-email` client registration**

Add a new client block in `SsoSeeder.SeedAsync()` following the existing pattern:
- Client ID: `innovayse-email`
- Display Name: `Innovayse Mail`
- Client Secret: `dev-secret-email` (dev only)
- Redirect URI: `http://localhost:4003/api/auth/callback`
- Post-logout URI: `http://localhost:4003`
- Scopes: `openid`, `profile`, `email`
- Grant type: authorization code

- [ ] **Step 3: Rebuild SSO to register the new client**

```bash
cd /home/innovayse/Desktop/innovayse-workspace/innovayse-sso
docker compose up -d --build sso-api
```

- [ ] **Step 4: Build and start the email app**

```bash
cd /home/innovayse/Desktop/innovayse-workspace/innovayse-email
docker compose up -d --build
```

- [ ] **Step 5: End-to-end smoke test**

1. Open `http://localhost:4003` → should redirect to SSO login
2. Login with SSO credentials → should redirect back
3. Mailbox picker appears (if multiple) or goes straight to inbox
4. Email list loads from IMAP
5. Click an email → reading pane opens with animation
6. Click Compose → modal opens, send test email
7. Check storage bar shows real quota

- [ ] **Step 6: Commit**

```bash
cd /home/innovayse/Desktop/innovayse-workspace/innovayse-sso
git add backend/src/Innovayse.SSO.API/Seed/SsoSeeder.cs
git commit -m "feat: register innovayse-email OIDC client"
```
