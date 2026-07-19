# Decouple innovayse-email from Hostpanel & SSO — Design Spec

## Overview

`innovayse-email` currently depends on two sibling services it should have no coupling to:

1. **Hostpanel** — `HostpanelCredentialProvider` calls `GET /api/my-email/mailboxes/credentials` to fetch mailbox credentials. This endpoint **does not exist** in Hostpanel (was planned, never implemented) — the dependency is currently dead in production.
2. **Innovayse SSO** — the backend validates JWT bearer tokens against the SSO authority, and the frontend runs a full OIDC/PKCE flow, purely to identify *which* SSO user to ask Hostpanel about.

Both dependencies exist only to answer one question: "what are this user's IMAP/SMTP credentials?" Since Mailcow's admin API cannot verify a mailbox password (only real IMAP/SMTP AUTH can), the natural, dependency-free answer is to let the user authenticate directly against their mailbox — the same model every standalone webmail client (Gmail, Outlook, Roundcube) uses.

**Goal:** innovayse-email becomes fully self-contained. No calls to Hostpanel. No calls to an SSO authority. Login = email + password, validated live via IMAP.

## Auth & Session Flow (new)

1. User visits the app, sees a login form (email + password) — no redirect anywhere.
2. Frontend `POST /api/auth/login {email, password}` → backend.
3. Backend attempts a live IMAP LOGIN against the configured mail server (`Imap:Host/Port`) using the submitted credentials.
   - Success → backend encrypts `{email, password}` with its own AES key (`EncryptionKey`, generated independently, not shared with any other service), sets it as an httpOnly, Secure, SameSite=Lax cookie (`mail_session`).
   - Failure (bad credentials) → `401` with a generic message ("Invalid email or password") — no user enumeration.
   - Failure (mail server unreachable) → `503`.
4. Every subsequent API request carries `mail_session`. `MailboxSessionMiddleware` decrypts it in place and populates `MailboxSessionHolder.ActiveMailbox` — no outbound HTTP call, no Hostpanel, no SSO token validation.
5. `POST /api/auth/logout` clears the cookie.
6. Mailcow's admin API (`X-API-Key`, already configured) is still called server-side, read-only, solely for cosmetic quota display (`MailcowQuotaProvider`) — unchanged.

**Multi-mailbox note:** the current app lets one SSO identity pick from several mailboxes (`select-mailbox` picker), because Hostpanel returned a list per SSO user. A password-based login is inherently single-mailbox — the password only proves one mailbox. The picker/list concept is dropped; switching mailboxes means logging out and logging back in with different credentials. This matches how every other standalone webmail client works and was agreed as acceptable.

## Backend Changes

### Removed entirely
- `Infrastructure/Providers/HostpanelCredentialProvider.cs`
- `Infrastructure/Settings/HostpanelSettings.cs`
- `Hostpanel` config section in `appsettings.json` / `appsettings.Development.json`
- JWT Bearer / SSO authentication registration in `Program.cs` (`AddJwtBearer`, `Sso:Authority` etc.)
- `Sso` config section in `appsettings.json`
- `MailboxController.SelectMailbox` (already dead code — frontend never called it; superseded by cookie-based login anyway)
- `MailboxSessionHolder.AccessToken` (no bearer token exists anymore)

### Added
- `Domain/Interfaces/IMailboxAuthenticator.cs` — replaces `IMailboxCredentialProvider`:
  ```csharp
  Task<MailboxCredentials?> AuthenticateAsync(string email, string password, CancellationToken ct);
  ```
  Returns `null` on invalid credentials; throws a distinct exception (e.g. `MailServerUnavailableException`) if the IMAP server itself can't be reached, so the controller can distinguish 401 vs 503.
- `Infrastructure/Providers/ImapMailboxAuthenticator.cs` — implements `IMailboxAuthenticator` by attempting a real MailKit IMAP connect+authenticate against `Imap:Host/Port`. On success, builds a `MailboxCredentials` from the input email/password plus configured `Imap`/`Smtp` host/port (single mail server — no per-mailbox host lookup was ever needed beyond what config already provides).
- `API/Controllers/AuthController.cs`:
  - `POST /api/auth/login` — calls `IMailboxAuthenticator`, on success serializes `MailboxCredentials` to JSON, AES-encrypts with `EncryptionKey`, sets `mail_session` cookie (httpOnly, Secure when not dev, SameSite=Lax, sliding-ish fixed TTL e.g. 12h — matches previous `auth_token` cookie lifetime conventions).
  - `POST /api/auth/logout` — `Response.Cookies.Delete("mail_session")`.
- `Infrastructure/Security/SessionCookieCrypto.cs` — small helper wrapping AES-CBC encrypt/decrypt of the session payload (mirrors the decryption logic already used for Hostpanel's `EncryptedPassword`, but now owns both encrypt and decrypt since innovayse-email is the sole writer/reader).

### Changed
- `MailboxSessionMiddleware` — no longer reads `Authorization`/`X-Mailbox-Email` headers or calls a credential provider over HTTP. Reads `mail_session` cookie, decrypts via `SessionCookieCrypto`, deserializes into `session.ActiveMailbox`. Decrypt/parse failure is treated as "no active mailbox" (not a 500) — consistent with today's swallow-and-continue behavior, controllers still gate on `ActiveMailbox is null` → 401.
- Controllers (`MailboxController` minus `SelectMailbox`, `ComposeController`, `QuotaController`, `AttachmentController`, `MessageController`) — drop `[Authorize]` (no more ASP.NET auth scheme); replace with an explicit check (existing pattern already used: return 401 if `session.ActiveMailbox is null`). A single `[ServiceFilter(typeof(RequireActiveMailboxFilter))]` or equivalent action filter can centralize this instead of repeating the null-check per action — implementation detail for the plan.
- `MailboxCredentials` model — unchanged shape, just now constructed locally instead of deserialized from Hostpanel's DTO.
- `Infrastructure/DependencyInjection.cs` — remove Hostpanel `HttpClient` registration (including its dangerous "accept any certificate" handler, which no longer has a reason to exist); register `IMailboxAuthenticator → ImapMailboxAuthenticator`.

### Config
- `appsettings.json` — remove `Hostpanel`, remove `Sso`; keep `Mailcow`, `Imap`, `Smtp`, `EncryptionKey`, add `SessionCookie` section if TTL needs to be configurable.
- `docker-compose.yml` — remove `Hostpanel__ApiUrl` and all `Sso__*` env vars from both `email-api` and `email-frontend`. `innovayse_network` membership stays (still needed to reach Mailcow's admin API and the mail server).
- `ENCRYPTION_KEY` — regenerate as innovayse-email's own secret (no longer needs to match Hostpanel's). Document in `.env.example` that this key is local-only.

## Frontend Changes

### Removed
- `server/routes/auth/login.get.ts`, `server/routes/auth/callback.get.ts` (PKCE/OIDC flow)
- `pages/select-mailbox.vue`, `components/MailboxPicker.vue`
- Multi-mailbox list state in `composables/useMailbox.ts` (`fetchMailboxes`, mailbox array, `innovayse_active_mailbox` cookie) — replaced by a single `activeMailbox` populated from a lightweight `GET /api/auth/me`-style call (or from the login response directly, held in memory/pinia for the session)
- `Sso*` / `NUXT_PUBLIC_SSO_*` env vars and runtime config entries

### Added
- `pages/login.vue` — email + password form, `POST /api/auth/login`, on success redirect to `/mail`; on 401 show inline error; on 503 show "mail server unavailable" message.
- `server/routes/auth/logout.post.ts` (there is currently no logout route at all — new) — proxies to backend logout and clears any client-side state.

### Changed
- `server/routes/api/[...path].ts` — stop injecting `Authorization: Bearer` and `X-Mailbox-Email` headers (both sourced from now-removed cookies); simply forward the `mail_session` cookie through to the backend (cookie forwarding on a same-site proxy — no header translation needed).
- `pages/index.vue` — check auth by attempting a cheap authenticated call (or a dedicated `GET /api/auth/me`); redirect to `/login` on 401 instead of SSO's `/auth/login`. Drop the "single mailbox auto-select" branch entirely since there's no longer a list to check.
- `pages/mail.vue` / `MailSidebar.vue` — read `activeMailbox` from the simplified single-mailbox state; no functional change to quota display.

## Docs
- `README.md`, `docs/2026-07-08-innovayse-email-design.md`, `docs/2026-07-08-innovayse-email-plan.md` — updated to reflect the new auth flow; the plan doc's entire "Task 1: Hostpanel — Store Encrypted Mailbox Passwords" section is struck out/marked superseded (never implemented, no longer needed).

## Error Handling
- Invalid credentials → `401`, generic message, no distinction between "no such mailbox" and "wrong password" (avoid enumeration).
- Mail server unreachable during login → `503`.
- Cookie missing/corrupt/tampered on any authenticated request → treated as unauthenticated → `401`, cookie is not auto-cleared server-side (frontend calls logout explicitly if it wants to reset UI state).
- No changes to existing IMAP/SMTP operational error handling (already independent of Hostpanel/SSO).

## Testing
No tests exist today for this flow (confirmed — no test project in the backend `.sln`, no test framework in the frontend). This work adds the first coverage for it:
- Backend: unit tests for `ImapMailboxAuthenticator` (mocked IMAP client — success/bad-credentials/unreachable-server), `SessionCookieCrypto` round-trip, `AuthController` login/logout happy + error paths, `MailboxSessionMiddleware` with valid/missing/corrupt cookie.
- Manual/integration: log in with a real Mailcow test mailbox against the docker-compose stack, confirm inbox loads, refresh the page (session persists), logout, confirm 401 afterward, confirm wrong password is rejected with 401 and server-down (stop Mailcow container) yields 503.

## Out of Scope
- Password reset / mailbox provisioning flows — unaffected, still Hostpanel's job for actual mailbox creation (only the *reading* of credentials for login is being decoupled).
- Re-introducing multi-mailbox support under the new model (e.g. "add another mailbox" inside the app) — not requested, can be a future spec if needed.
