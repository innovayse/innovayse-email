> **Superseded 2026-07-19:** the SSO + Hostpanel-mediated auth flow described below was replaced with direct email/password IMAP login. See `docs/2026-07-19-decouple-hostpanel-design.md`.

# Innovayse Email — Webmail Client Design Spec

## Overview

A custom webmail client replacing SOGo/Roundcube that ships with Mailcow. Users authenticate via Innovayse SSO, select a mailbox (if they have multiple), and interact with their email through a glassmorphic Gmail-style UI. The backend connects to Mailcow's Dovecot (IMAP) and Postfix (SMTP) using stored mailbox credentials from the hostpanel database. No direct user access to Mailcow's admin panel — all Mailcow API calls are server-side only.

**Project codename:** Innovayse Mail
**Architecture:** Same as innovayse-sso — ASP.NET Core 9 backend (Clean Architecture + CQRS) + Nuxt 4 frontend (BFF proxy pattern)
**Dev ports:** API `localhost:4002`, Frontend `localhost:4003`

---

## Auth & Mailbox Resolution Flow

1. User visits `localhost:4003` (or `mail.innovayse.com` in prod)
2. Redirected to Innovayse SSO for authentication
3. SSO returns access token with user identity (sub, email, name)
4. Frontend calls Email API → API calls Hostpanel API (with same SSO token) to fetch user's mailboxes + encrypted credentials
5. If multiple mailboxes → show mailbox picker; if one → auto-select
6. User selects mailbox → selection stored in encrypted session cookie
7. All subsequent requests use the selected mailbox's credentials to connect IMAP/SMTP

**Mailcow admin API** is called server-side only (with API key in config) for:
- Mailbox quota/storage usage

No Mailcow URLs, credentials, or admin endpoints are exposed to the frontend.

---

## Backend Architecture

### Domain Layer

**Entities:**
- `EmailMessage` — Id (IMAP UID), Folder, From (name + address), To (list), Cc (list), Subject, BodyHtml, BodyPlain, Date, IsRead, HasAttachments, Snippet (preview text)
- `EmailFolder` — enum: Inbox, Drafts, Sent, Archive, Junk, Templates, Trash
- `EmailAttachment` — Filename, ContentType, Size, ContentId (for inline images)
- `MailboxSession` — active mailbox identity for the current request (email, IMAP host/port, SMTP host/port)

**Interfaces:**
- `IImapService` — ListMessages, GetMessage, MoveMessage, DeleteMessage, MarkAsRead, MarkAsUnread, SearchMessages, GetAttachment, SaveDraft, ListFolderCounts
- `ISmtpService` — SendMessage (with attachments)
- `IMailboxCredentialProvider` — GetMailboxesForUser, GetCredentials
- `IMailboxQuotaProvider` — GetQuotaUsage

### Application Layer (CQRS Handlers)

**Queries:**
- `ListMailboxesQuery` → calls hostpanel API, returns user's available mailboxes
- `ListMessagesQuery(folder, page, pageSize)` → paginated message list (subject, sender, date, snippet, isRead, hasAttachments)
- `GetMessageQuery(folder, uid)` → full message with HTML/plain body + attachment metadata
- `SearchMessagesQuery(query)` → cross-folder IMAP SEARCH by sender, subject, body
- `GetAttachmentQuery(folder, uid, attachmentIndex)` → download specific attachment as stream
- `InsertTemplateQuery(uid)` → fetch template message subject/body for compose pre-fill
- `GetQuotaQuery` → storage usage from Mailcow admin API
- `ListFolderCountsQuery` → unread counts per folder

**Commands:**
- `SendMessageCommand(to, cc, subject, bodyHtml, attachments)` → send via SMTP, copy to Sent folder
- `SaveDraftCommand(to, subject, body, existingDraftUid?)` → save/update in Drafts folder
- `DeleteMessageCommand(folder, uid)` → move to Trash; if already in Trash, permanent delete
- `MoveMessageCommand(sourceFolder, uid, targetFolder)` → move between folders (archive, junk, etc.)
- `MarkAsReadCommand(folder, uid)`
- `MarkAsUnreadCommand(folder, uid)`

### Infrastructure Layer

- `ImapMailService` — MailKit `ImapClient`, connects per-request using credentials from `IMailboxCredentialProvider`. Implements `IImapService`.
- `SmtpMailService` — MailKit `SmtpClient` for sending. Implements `ISmtpService`.
- `HostpanelCredentialProvider` — HTTP client that calls hostpanel's API to get mailbox list + credentials for the authenticated SSO user. Implements `IMailboxCredentialProvider`.
- `MailcowQuotaProvider` — HTTP client that calls Mailcow admin API (server-side, X-API-Key auth) for storage usage. Implements `IMailboxQuotaProvider`.

### API Layer

**Controllers:**
- `MailboxController`
  - `GET /api/mailboxes` — list available mailboxes for the authenticated user
  - `POST /api/mailboxes/select` — set active mailbox (stores in session cookie)
  - `GET /api/mailboxes/active` — get currently selected mailbox info

- `MessageController`
  - `GET /api/messages?folder=inbox&page=1&pageSize=50` — paginated message list
  - `GET /api/messages/{folder}/{uid}` — full message
  - `POST /api/messages/search?q=keyword` — cross-folder search
  - `PUT /api/messages/{folder}/{uid}/read` — mark as read
  - `PUT /api/messages/{folder}/{uid}/unread` — mark as unread
  - `POST /api/messages/{folder}/{uid}/move` — move to another folder
  - `DELETE /api/messages/{folder}/{uid}` — delete (move to trash or permanent)
  - `GET /api/messages/counts` — unread counts per folder

- `ComposeController`
  - `POST /api/compose/send` — send message (multipart form for attachments)
  - `POST /api/compose/draft` — save draft
  - `GET /api/compose/template/{uid}` — get template content for pre-fill

- `AttachmentController`
  - `GET /api/attachments/{folder}/{uid}/{index}` — download attachment

- `QuotaController`
  - `GET /api/quota` — mailbox storage usage

**Auth:** SSO JWT Bearer validation (same setup as hostpanel's SSO mode). Mailbox selection stored in encrypted session cookie.

---

## Frontend Architecture

### Pages

- `pages/index.vue` — if not authenticated, redirect to SSO; if authenticated but no mailbox selected, redirect to `/select-mailbox`; otherwise redirect to `/mail`
- `pages/select-mailbox.vue` — card grid of available mailboxes, click to select and redirect to `/mail`
- `pages/mail.vue` — the main webmail UI (single page: sidebar + message list + reading pane)

### Components

**Layout:**
- `MailSidebar.vue` — brand mark + wordmark, profile card (avatar, name, email, settings icon), Compose button, folder nav with unread badge, storage usage bar
- `SidebarParticles.vue` — non-interactive drifting particle canvas (accent colors, connecting lines)
- `TopBar.vue` — current date (left), icon buttons for calendar/contacts/settings/history (right)

**Message List:**
- `MessageList.vue` — folder title, filter/refresh icons, search input, scrollable list of `MessageRow`
- `MessageRow.vue` — gradient avatar with initials, sender name, timestamp, subject, preview snippet, unread dot

**Reading Pane:**
- `ReadingPane.vue` — "Back to inbox" link, subject, sender avatar/name/email/time, reply/forward/archive/delete actions, message body (HTML rendered), attachment list
- `AttachmentItem.vue` — file icon, name, size, download button

**Compose:**
- `ComposeModal.vue` — glass card overlay with ambient glow, To/Cc fields, Subject field, formatting toolbar (bold/italic/underline/list/link/attach), body textarea, footer (discard icon, "Save draft" button, "Send" button)

**Shared:**
- `ToastNotification.vue` — auto-dismiss after 2.4s ("Message sent", "Draft saved", "Draft discarded")
- `MailboxPicker.vue` — card component showing mailbox email, domain, quota summary
- `GradientAvatar.vue` — initials avatar with per-sender gradient

### Composables

- `useMailbox()` — fetch available mailboxes, select/switch active mailbox, active mailbox state
- `useMail()` — messages list, selected message, current folder, folder counts, search query, 20s polling interval, pagination
- `useCompose()` — compose modal open/close, send/save draft/discard actions, template insertion, attachment upload state, toast trigger

### BFF Server Routes

- `server/routes/api/[...path].ts` — proxies all `/api/*` requests to Email API backend (`http://email-api:8080`), forwards auth headers and cookies. Same pattern as innovayse-sso.

### Key UI Behaviors

**List/Reading Pane Collapse (Gmail-style):**
- No email selected → list panel fills 100% width, reading pane collapsed
- Email selected → list panel animates to 340px (`transition: width .32s cubic-bezier(.22,1,.36,1)`), reading pane fills remaining space as `flex: 1`
- Folder switch → resets selection to null, list goes back to 100%
- "Back to inbox" link → same as folder switch
- **Critical:** only the list panel's `width` is transitioned. Reading pane sizing is passive (flex: 1). Do not transition flex-grow/min-width on the reading pane.

**Reactive Reading Glow:**
- Blurred glow blob behind reading pane
- Message open: `opacity 0.12 → 0.32`, `scale(0.8) → scale(1)`
- Message close: reverse
- Timing: `.38s cubic-bezier(.22,1,.36,1)`

**Unread State:**
- Opening an email marks it as read (API call + local state update)
- Unread dot, bold sender/subject text, Inbox nav badge count
- Badge only counts unread in Inbox

**Compose Modal:**
- Independent of selection state
- Send/Save draft/Discard all close modal and show toast (2.4s auto-dismiss)
- Template insertion pre-fills subject + body

**Search:**
- Cross-folder IMAP SEARCH
- Results displayed in the message list with folder indicators

---

## Design Tokens

Same visual system as the auth redesign (reuse existing tokens):

| Token | Value |
|---|---|
| Background | `#07080d` |
| Sidebar/panels | `rgba(14,16,26,0.55)` / `rgba(12,14,22,0.4)` with `backdrop-filter: blur(20-24px) saturate(160%)` |
| Accent gradient | `linear-gradient(135deg, #29A3E8, #8B5CF6)` |
| Font | Manrope, weights 400-800 |
| Radii | 24px (compose modal), 20-22px (cards), 12-14px (buttons/inputs/rows), 10-11px (icon buttons, badges) |
| Text heading | `#f5f6fa` |
| Text body | `#c3c7d4` |
| Text muted | `#8890a3` |
| Text faint | `#565d70` |
| Panel border | `rgba(255,255,255,0.07)` |
| Per-sender avatars | Data-driven gradient (two-color CSS gradient per sender) |
| Background glows | 4 radial circles (`#29A3E8` / `#8B5CF6`, 9-22% opacity, 110-140px blur) with 18-24s CSS keyframe drift |
| Scrollbar | 8px, `rgba(255,255,255,0.12)` thumb, transparent track |
| Icons | Inline SVGs, stroke-based, `stroke-width: 1.6-2.2`. No icon library. |

---

## Docker & Infrastructure

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
      - Mailcow__ApiUrl=https://mail.innovayse.local
      - Mailcow__ApiKey=${MAILCOW_API_KEY}
      - Imap__Host=mail.innovayse.local
      - Imap__Port=993
      - Smtp__Host=mail.innovayse.local
      - Smtp__Port=587
      - Cors__AllowedOrigins__0=http://localhost:4003
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

No dedicated database. All email data lives on IMAP, mailbox credentials come from hostpanel API, quota from Mailcow admin API.

---

## Hostpanel API Extension

One new endpoint needed in hostpanel to support the email app:

`GET /api/portal/client/email/mailboxes/credentials`

Returns the authenticated user's mailboxes with IMAP/SMTP connection details:
```json
[
  {
    "email": "test@owlacademy.am",
    "password": "encrypted-password",
    "imapHost": "mail.innovayse.com",
    "imapPort": 993,
    "smtpHost": "mail.innovayse.com",
    "smtpPort": 587,
    "displayName": "Test User",
    "quotaMb": 2048
  }
]
```

The email app decrypts the password using a shared encryption key.

---

## SSO Client Registration

Register `innovayse-email` as an OIDC client in the SSO seeder:
- Client ID: `innovayse-email`
- Redirect URI: `http://localhost:4003/api/auth/callback`
- Scopes: `openid`, `profile`, `email`
- Grant type: Authorization Code with PKCE

---

## Out of Scope (v1)

- Real-time push (IMAP IDLE + WebSockets) — polling at 20s for now
- Calendar/Contacts integration (top bar icons are placeholder)
- Email rules/filters
- Multiple account view (single mailbox active at a time, switch via picker)
- Rich text editor (plain textarea with basic formatting toolbar for v1)
- Email signatures management
