# Contributing

Thanks for considering a contribution to Innovayse Mail.

## Getting started

1. Fork the repo and clone your fork.
2. Follow the setup instructions in [README.md](README.md) to get the backend and
   frontend running locally (you'll need a Mailcow instance and an OIDC provider to
   exercise the full login/mail flow — see the README's Prerequisites).
3. Create a branch off `main` for your change.

## Making changes

- Keep pull requests focused on a single change.
- Match the existing code style (`backend/` follows standard .NET/Clean Architecture
  conventions, `frontend/` follows the repo's Nuxt/TypeScript config).
- Build both services before submitting:
  - Backend: `dotnet build` from `backend/`
  - Frontend: `yarn build` from `frontend/`

## Commit messages

Use a short imperative summary line, optionally followed by a blank line and more detail
on the *why* behind the change.

## Submitting a pull request

- Describe what the change does and why.
- Link any related issue.
- Make sure CI passes before requesting review.

## Reporting bugs

Open an issue with steps to reproduce, expected vs. actual behavior, and relevant
environment details.
