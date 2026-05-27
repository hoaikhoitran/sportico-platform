# 12 — Deployment Guide

Target: **Azure App Service** (Linux/Windows), database on **PostgreSQL** (Supabase or any managed Postgres). CI/CD via **GitHub Actions**. See [deployment/](deployment/) for per-topic detail.

## Build & Publish

```bash
dotnet restore SporticoApp.Api.sln
dotnet build SporticoApp.Api.sln -c Release --no-restore
dotnet publish SporticoApp.Api.sln -c Release -o publish --no-build
```

The publish output (the API host) is what gets deployed.

## Azure App Service

The app is deployed to App Service `sportico-api-khoi`.

1. **App settings (environment variables)** — set every key from [13 — Environment Variables](13-environment-variables.md) under *App Service → Settings → Environment variables*. Azure maps `__` to the `:` config separator, e.g. `ConnectionStrings__Default`, `JWT__SecretKey`, `PayOs__ApiKey`.
2. **HTTPS** — App Service serves HTTPS by default. The app calls `UseHttpsRedirection()`.
3. **Swagger** — enabled in **all** environments and served at the site root (`RoutePrefix = ""`). After deploy, browse the App Service URL to reach Swagger. If you do not want Swagger public in production, gate it behind environment/auth (see [deployment/production-checklist.md](deployment/production-checklist.md)).
4. **Logs** — use *App Service → Log stream* for live logs. The app logs handled `AppException`s as warnings and unhandled exceptions as errors via `ExceptionMiddleware`.

## CI/CD (GitHub Actions)

[.github/workflows/main_sportico-api-khoi.yml](../.github/workflows/main_sportico-api-khoi.yml) runs on push to `main`:

```
build:  checkout → setup .NET 8 → restore → build -c Release → publish -o myapp → upload artifact
deploy: download artifact → azure/login (OIDC) → azure/webapps-deploy@v3 (app: sportico-api-khoi)
```

Auth uses **OIDC federated credentials** (no publish profile). Required repo secrets:

| Secret | Source |
|---|---|
| `AZURE_CLIENT_ID` | Azure AD app registration |
| `AZURE_TENANT_ID` | Azure AD tenant |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription |

See [deployment/ci-cd.md](deployment/ci-cd.md).

## Database & Migrations on Deployment

The CI workflow **does not run migrations**. Apply them as a deliberate step:

```bash
dotnet ef database update --project src/SporticoApp.Infrastructure --startup-project src/SporticoApp.Api
```

Run this from a machine that can reach the production database (with `ConnectionStrings__Default` pointed at it), **before or just after** the deploy of code that depends on the new schema. See [deployment/migration-strategy.md](deployment/migration-strategy.md).

## Secrets Management

- **Never commit secrets.** `appsettings.json` ships with blank secret values; real values come from environment variables / Azure App Settings. `.env`, `appsettings.*.Local.json`, `secrets.json`, `*.mcp.json`, and `.claude/` are git-ignored.
- Store production secrets in Azure App Settings (or Key Vault references).
- Rotate the JWT signing key and PayOS keys outside source control.

## CORS

> NOTE: No CORS policy is registered in `Program.cs` in the reviewed code (`AddCors`/`UseCors` are absent). If the frontend is served from a different origin (e.g. the Vercel site referenced in config), add a CORS policy before deploying a browser client. Suggested:

```csharp
builder.Services.AddCors(o => o.AddPolicy("frontend", p =>
    p.WithOrigins("https://your-frontend-domain").AllowAnyHeader().AllowAnyMethod()));
// ...
app.UseCors("frontend");   // before UseAuthentication/UseAuthorization
```

Track CORS origins via configuration (see [13 — Environment Variables](13-environment-variables.md)).

## Swagger in Production

Swagger is currently always on. For production hardening, consider disabling it or protecting it. This is a **decision for the team**, not a current behaviour — documented in the production checklist.

## Health Check Recommendation

> NOTE: No health-check endpoint is registered. Recommended addition for App Service / uptime monitoring:

```csharp
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();
app.MapHealthChecks("/health");
```

## Production URL

`https://sportico-api-khoi.azurewebsites.net/`
