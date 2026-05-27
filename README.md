# Sportico Platform

## Overview

Sportico is a coaching marketplace backend that connects sports/fitness **coaches** with **learners**. A coach publishes paid training packages; a learner buys one (creating a booking), then the pair schedule sessions, chat, and follow a personalized training plan. The platform takes a fixed **15% commission** and pays the coach progressively — one slice per completed session — into an internal wallet the coach can withdraw. Built with ASP.NET Core 8 and Clean Architecture on PostgreSQL.

## Tech Stack

| Area | Choice |
|---|---|
| Backend | ASP.NET Core 8 Web API, Clean Architecture (Api / Application / Core / Infrastructure / Shared) |
| Database | PostgreSQL via EF Core 8 (Npgsql), snake_case schema, committed migrations |
| Auth | JWT Bearer + refresh tokens, role-based authorization (`learner` / `coach` / `admin`) |
| Validation | FluentValidation + data annotations; uniform `Result<T>` / `PagedResult<T>` responses |
| Payment | PayOS (inbound learner payments) + manual; internal coach wallet & withdrawals |
| Deployment | Azure App Service via GitHub Actions (OIDC) |

## Key Features

- Coach training package marketplace (create → admin approval → public listing)
- Booking and training-session scheduling
- 15% platform commission, snapshotted per booking
- Coach wallet credited per completed session, with withdrawal requests
- Personalized training: assessment → plan (weeks/days/exercises) → progress check-ins → coach feedback
- Chat (after an active booking) and notifications
- Admin moderation: packages, payout accounts, withdrawals

## Quick Start

```bash
dotnet restore
dotnet build
dotnet ef database update --project src/SporticoApp.Infrastructure --startup-project src/SporticoApp.Api
dotnet run --project src/SporticoApp.Api
```

Then open Swagger at the app root (e.g. `http://localhost:5095/`). Full setup, including required configuration, is in [docs/14-local-development.md](docs/14-local-development.md).

## Documentation

Full documentation lives in [docs/](docs/). Start here:

- [Project Overview](docs/01-project-overview.md)
- [Architecture](docs/02-architecture.md)
- [API Overview](docs/05-api-overview.md)
- [Frontend Integration Guide](docs/11-frontend-integration-guide.md)
- [Deployment Guide](docs/12-deployment-guide.md)

## Environment

All configuration keys (database, JWT, PayOS, email) are documented in [docs/13-environment-variables.md](docs/13-environment-variables.md). Secrets come from environment variables / Azure App Settings — `appsettings.json` ships with blank secret values and must never contain real ones.

## API & Deployment Pointers

- API reference per module: [docs/api/](docs/api/)
- Deployment (Azure, Supabase/Postgres, migrations, CI/CD, checklist): [docs/deployment/](docs/deployment/)

## Notes

The legacy `Package` / `CoachPackage` / `Post` modules (the old coach-posting subscription model) still exist in the codebase but are **deprecated**. The current business model uses `TrainingPackage` + `Booking`. Do not build new features on the legacy modules — see [docs/17-legacy-modules.md](docs/17-legacy-modules.md).
