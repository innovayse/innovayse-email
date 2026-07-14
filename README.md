# Innovayse Mail

[![CI](https://github.com/innovayse/innovayse-email/actions/workflows/ci.yml/badge.svg)](https://github.com/innovayse/innovayse-email/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

A custom Gmail-style webmail client for [Mailcow](https://mailcow.email/) — connects to
Mailcow's Dovecot (IMAP) and Postfix (SMTP) to give users a full webmail UI (folders,
message list, reading pane, compose) without SOGo or Roundcube.

## Architecture

| Service | Path | Stack |
|---|---|---|
| API | `backend/` | .NET 9, ASP.NET Core, Clean Architecture (CQRS), MailKit |
| Frontend | `frontend/` | Nuxt 4, Vue 3, Tailwind CSS |

The frontend is a BFF (backend-for-frontend): its Nuxt server routes handle the SSO
OAuth/PKCE flow and proxy `/api/*` calls to the backend, injecting the bearer token and
active-mailbox header. The backend talks to IMAP/SMTP directly via MailKit, and looks up
mailbox credentials and quota from external services (Hostpanel, Mailcow admin API).

## Prerequisites

- .NET 9 SDK
- Node.js 22.12+
- A Mailcow instance (IMAP/SMTP endpoints) to connect to
- An OIDC-compatible SSO provider (the app expects an OAuth2/PKCE authorization code flow)

## Setup

### Backend (API)

```bash
cd backend
dotnet run --project src/Innovayse.Email.API
```

Configure `backend/src/Innovayse.Email.API/appsettings.Development.json` (or environment
variables) with your SSO authority, Mailcow API URL/key, and IMAP/SMTP host — see
`appsettings.json` for the full shape.

### Frontend

```bash
cd frontend
cp ../.env.example .env
yarn install
yarn dev
```

Key environment variables (see `.env.example` and `nuxt.config.ts`):

| Variable | Purpose |
|---|---|
| `API_PROXY_TARGET` | Backend API base URL |
| `SSO_URL`, `SSO_CLIENT_ID`, `SSO_CLIENT_SECRET`, `SSO_CALLBACK_URL` | OIDC login flow |
| `MAILCOW_API_URL`, `MAILCOW_API_KEY` | Mailcow admin API (quota lookups) |
| `IMAP_HOST`/`IMAP_PORT`, `SMTP_HOST`/`SMTP_PORT` | Mail server connection |
| `ENCRYPTION_KEY` | Encrypts stored mailbox credentials |

### Docker

`docker/api.Dockerfile` and `docker/frontend.Dockerfile` build production images for each
service; see `docker-compose.yml` for how they're wired together (ports 4002/4003 by
default).

## Known gaps

The frontend doesn't currently pass a full `vue-tsc` type check (see open issues) — some
components/composables have real type errors that predate the CI setup. `yarn build`
succeeds regardless, since Nuxt's build doesn't run a full type check.

## License

[MIT](LICENSE)
