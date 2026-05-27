# Deployment — Azure App Service

The API is deployed to Azure App Service `sportico-api-khoi` via GitHub Actions ([ci-cd.md](ci-cd.md)).

## Prerequisites
- An Azure App Service (Linux or Windows) running the .NET 8 runtime.
- A reachable PostgreSQL database ([supabase-postgres.md](supabase-postgres.md)).
- OIDC federated credentials configured for the GitHub Actions deploy (or another deploy method).

## App Settings (environment variables)

Set every key from [13 — Environment Variables](../13-environment-variables.md) under **App Service → Settings → Environment variables**. Azure maps `__` to the `:` config separator.

Minimum:
```
ConnectionStrings__Default = Host=...;Port=5432;Database=...;Username=...;Password=...;SslMode=Require;Trust Server Certificate=true
JWT__SecretKey   = <min 32-char secret>
JWT__Issuer      = Sportico
JWT__Audience    = SporticoClient
JWT__AccessTokenExpirationMinutes = 15
JWT__RefreshTokenExpirationDays   = 30
AppSettings__ApiBaseUrl = https://sportico-api-khoi.azurewebsites.net
```
Add `PayOs__*` and `EmailSettings__*` for those features.

> Never store secrets in `appsettings.json`. The committed file has blank secret values by design.

## Connection string
Provide it as the `ConnectionStrings__Default` App Setting (above). The app reads it via `configuration.GetConnectionString("Default")` and uses Npgsql. Use a **direct** (non-pooled) endpoint for migrations.

## Build / publish
CI builds and publishes the solution:
```bash
dotnet restore SporticoApp.Api.sln
dotnet build SporticoApp.Api.sln -c Release --no-restore
dotnet publish SporticoApp.Api.sln -c Release -o myapp --no-build
```
The `myapp` artifact is deployed with `azure/webapps-deploy@v3`.

## Migrations
The pipeline does **not** run migrations. Apply them as a deliberate step against the production database before/after deploy:
```bash
dotnet ef database update --project src/SporticoApp.Infrastructure --startup-project src/SporticoApp.Api
```
See [migration-strategy.md](migration-strategy.md).

## Swagger
Swagger UI is served at the **site root** in all environments (`RoutePrefix = ""`). After deploy, browse the App Service URL to access it. To restrict it in production, gate `UseSwagger`/`UseSwaggerUI` behind environment or auth (see [production-checklist.md](production-checklist.md)).

## Logs
- Live logs: **App Service → Log stream**.
- `ExceptionMiddleware` logs handled `AppException`s as warnings and unhandled exceptions as errors.
- Consider enabling Application Insights for structured telemetry (not currently wired).

## Restart / troubleshoot
- Restart: **App Service → Overview → Restart**.
- Common deploy issues: missing App Settings (startup throws on missing JWT config), unreachable DB (check connection string + firewall), migrations not applied (see [16 — Troubleshooting](../16-troubleshooting.md)).
