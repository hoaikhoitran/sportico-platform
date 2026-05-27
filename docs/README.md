# Sportico Platform — Documentation

This is the documentation hub for the Sportico backend (ASP.NET Core 8, Clean Architecture, PostgreSQL).

All documentation here is derived from the current source code. Where behaviour could not be confirmed in code, it is marked with a `NOTE` or `TODO`.

## Documentation Map

### Core concepts (read in order)

| Doc | Purpose |
|---|---|
| [01 — Project Overview](01-project-overview.md) | What Sportico is, actors, business model, modules, legacy vs current |
| [02 — Architecture](02-architecture.md) | Clean Architecture layers, request flow, DI, validation, error handling |
| [03 — Domain Model](03-domain-model.md) | Entities, fields, relationships, business rules |
| [04 — Database Schema](04-database-schema.md) | PostgreSQL conventions, indexes, constraints, migrations |

### API and security

| Doc | Purpose |
|---|---|
| [05 — API Overview](05-api-overview.md) | Endpoint groups, controllers, roles |
| [06 — Auth and Roles](06-auth-and-roles.md) | JWT login, roles, headers, Swagger usage |
| [api/](api/) | Endpoint-level reference per module |

### Business flows

| Doc | Purpose |
|---|---|
| [07 — Business Flows](07-business-flows.md) | End-to-end flows with status transitions |
| [08 — Payment and Wallet](08-payment-and-wallet.md) | 15% commission, booking snapshot, wallet ledger, payouts, PayOS |
| [09 — Personalized Training](09-personalized-training.md) | Assessment → plan → progress → feedback |
| [10 — Chat and Notifications](10-chat-and-notifications.md) | Chat gating, notification triggers |

### Frontend

| Doc | Purpose |
|---|---|
| [11 — Frontend Integration Guide](11-frontend-integration-guide.md) | API base URL, token handling, pages, error handling |
| [frontend/](frontend/) | Routes, contracts, per-dashboard UI guidance |

### Operations

| Doc | Purpose |
|---|---|
| [12 — Deployment Guide](12-deployment-guide.md) | Build, publish, Azure, env, migrations |
| [13 — Environment Variables](13-environment-variables.md) | All configuration keys |
| [14 — Local Development](14-local-development.md) | Run the API locally |
| [15 — Testing and Smoke Test](15-testing-and-smoke-test.md) | Verified end-to-end smoke test |
| [16 — Troubleshooting](16-troubleshooting.md) | Common problems and fixes |
| [deployment/](deployment/) | Azure, Supabase, migration strategy, CI/CD, checklist |

### Legacy

| Doc | Purpose |
|---|---|
| [17 — Legacy Modules](17-legacy-modules.md) | Package / CoachPackage / Post — old subscription model |

## Who Should Read What

- **New backend engineer** — 01, 02, 03, 04, then the `api/` reference.
- **Frontend engineer** — 11, 05, 06, then `frontend/` and `api/`.
- **DevOps / deployment** — 12, 13, 14, then `deployment/`.
- **Product / business** — 01, 07, 08, 09.
- **QA** — 07, 15, 16.
