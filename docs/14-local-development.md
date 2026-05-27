# 14 — Local Development

## Prerequisites

- **.NET 8 SDK**
- **PostgreSQL** (local install, Docker, or a remote/managed instance such as Supabase)
- **EF Core CLI** (`dotnet-ef`)

Install the EF CLI globally (or restore the local tool manifest in `.config/dotnet-tools.json`):

```bash
dotnet tool install --global dotnet-ef
# or, using the repo's tool manifest:
dotnet tool restore
```

## 1. Configure

Create a `.env` in the repo root (git-ignored, auto-loaded). See [13 — Environment Variables](13-environment-variables.md) for all keys. Minimum to boot:

```env
ConnectionStrings__Default=Host=localhost;Port=5432;Database=sportico;Username=postgres;Password=postgres
JWT__SecretKey=replace-with-a-long-random-secret-min-32-chars
JWT__Issuer=Sportico
JWT__Audience=SporticoClient
JWT__AccessTokenExpirationMinutes=15
JWT__RefreshTokenExpirationDays=30
AppSettings__ApiBaseUrl=http://localhost:5095
```

PayOS and email keys are only needed for those features (PayOS purchase, registration email).

## 2. Restore & Build

```bash
dotnet restore
dotnet build
```

## 3. Apply Migrations

```bash
dotnet ef database update --project src/SporticoApp.Infrastructure --startup-project src/SporticoApp.Api
```

This creates the schema (enables `citext`, `pg_trgm`, `pgcrypto`; creates all tables including the booking/training/wallet flow).

## 4. Run the API

```bash
dotnet run --project src/SporticoApp.Api
```

Default URLs (from `launchSettings.json`): `http://localhost:5095` and `https://localhost:7058`.

## 5. Open Swagger

Browse to the root URL — Swagger UI is served at `/`:

```
http://localhost:5095/
```

Use `POST /api/auth/login`, copy `data.accessToken`, click **Authorize**, paste the token (no `Bearer ` prefix).

## Seed / Test Users

> NOTE: No automated data seeding was found (no `Seed`/`DbInitializer`). You must create users yourself:
> 1. `POST /api/auth/register` → creates an `inactive` learner.
> 2. Verify the email (the link points to `{AppSettings:ApiBaseUrl}/api/auth/verify-email?token=...`). Locally, either configure SMTP so the email arrives, or read the token from the `users.email_verification_token` column and call `GET /api/auth/verify-email?token=...` directly.
> 3. `POST /api/coaches/register` (while authenticated) to grant the `coach` role and create a coach profile.
> 4. The `admin` role and the `roles`/sports rows must be present. Ensure the `roles` table contains `learner`, `coach`, `admin`. Grant `admin` by inserting a `user_roles` row for an existing user, since there is no admin self-service endpoint.

A quick-start sequence for manual testing is in [15 — Testing and Smoke Test](15-testing-and-smoke-test.md).

## Common Local Errors

| Symptom | Cause | Fix |
|---|---|---|
| `InvalidOperationException: JWT configuration is missing required values` at startup | `JWT__SecretKey/Issuer/Audience` blank | Set them in `.env` / env vars |
| `relation "..." does not exist` | Migrations not applied | Run `dotnet ef database update` |
| `password authentication failed` / cannot connect | Wrong `ConnectionStrings__Default` | Fix host/port/credentials; ensure Postgres is running |
| EF CLI cannot find connection | `.env` not loaded for design-time | `AppDbContextFactory` loads `../SporticoApp.Api/.env`; ensure that path or `appsettings.json` has the connection string |
| `AppSettings:ApiBaseUrl is missing` during register | Key not set | Set `AppSettings__ApiBaseUrl` |
| Login returns 401 "Account is not active" | User not verified | Verify the email / set `users.status = 'active'` |

More in [16 — Troubleshooting](16-troubleshooting.md).
