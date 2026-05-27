# Deployment — Supabase / PostgreSQL

Supabase is managed PostgreSQL, so the EF Core schema runs on it without changes. Any managed Postgres (Render, Azure Database for PostgreSQL, RDS, etc.) works the same way.

## Connection string

Use the standard Npgsql format as `ConnectionStrings__Default`:
```
Host=<host>;Port=5432;Database=<db>;Username=<user>;Password=<password>;SslMode=Require;Trust Server Certificate=true
```

## Direct connection for migrations

Supabase exposes two endpoints:
- **Direct connection** (port 5432) — use this for `dotnet ef database update`. Migrations issue DDL and prepared statements that the transaction pooler can reject.
- **Pooled / PgBouncer** (transaction mode) — fine for the running app's normal queries, but **not** for migrations.

When in doubt, run migrations against the direct endpoint and let the app use whichever endpoint you prefer at runtime.

## SSL

Set `SslMode=Require`. Add `Trust Server Certificate=true` if the provider's certificate chain is not in the host trust store. Prefer proper certificate validation in production where possible.

## Extensions

`OnModelCreating` declares `citext`, `pg_trgm`, and `pgcrypto`. The migrations enable them via `HasPostgresExtension`. On Supabase these extensions are available; if a managed provider restricts extension creation, ensure the migration user has rights to `CREATE EXTENSION`, or pre-create them.

## Apply the schema

```bash
dotnet ef database update --project src/SporticoApp.Infrastructure --startup-project src/SporticoApp.Api
```

Ensure `ConnectionStrings__Default` (env var or `src/SporticoApp.Api/.env` / `appsettings.json`) points at the target before running. The design-time factory reads `appsettings.json` + env vars.

## Secrets

- **Do not commit** the connection string or DB password. Use env vars / Azure App Settings / a git-ignored `.env`.
- Rotate credentials outside source control.
- The repo `.gitignore` already excludes `.env`, `appsettings.*.Local.json`, and `secrets.json`.

## Backups

Enable the provider's automated backups (Supabase offers scheduled backups on paid tiers). Take a manual backup before destructive migrations or legacy-table cleanup ([migration-strategy.md](migration-strategy.md)).

> NOTE: An older README referenced Render's free Postgres tier (which sleeps when idle). The connection mechanism is identical for Supabase; only the host/credentials differ.
