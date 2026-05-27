# Deployment — CI/CD

Pipeline: [.github/workflows/main_sportico-api-khoi.yml](../../.github/workflows/main_sportico-api-khoi.yml). Triggers on push to `main`.

## Stages

```
build:
  - actions/checkout@v4
  - actions/setup-dotnet@v4 (8.x)
  - dotnet restore SporticoApp.Api.sln
  - dotnet build SporticoApp.Api.sln -c Release --no-restore
  - dotnet publish SporticoApp.Api.sln -c Release -o myapp --no-build
  - upload artifact (.net-app → myapp)

deploy:   (needs: build)
  - download artifact
  - azure/login@v2  (OIDC federated credentials)
  - azure/webapps-deploy@v3  (app-name: sportico-api-khoi, package: myapp)
```

## Authentication — OIDC

The deploy uses **OpenID Connect** (no stored publish profile/password):

```yaml
permissions:
  id-token: write
  contents: read
```

Required repo secrets (**Settings → Secrets and variables → Actions**):

| Secret | Source |
|---|---|
| `AZURE_CLIENT_ID` | Azure AD app registration (with a federated credential for this repo/branch) |
| `AZURE_TENANT_ID` | Azure AD tenant |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription |

## Environment variables

Application configuration is **not** injected by the pipeline — it lives in Azure **App Settings** ([azure-deployment.md](azure-deployment.md), [13 — Environment Variables](../13-environment-variables.md)). The pipeline only builds and deploys code.

## Migration caution

The workflow does **not** run database migrations. Apply migrations as a separate, deliberate step against the target database ([migration-strategy.md](migration-strategy.md)). A deploy that introduces new schema usage will fail at runtime until migrations are applied.

## Suggested improvements (not yet implemented)

> NOTE: These are recommendations, not current behaviour.
- Add a CI gate that runs `dotnet build` on PRs (and tests once a test project exists).
- Add a manual/approved job to run `dotnet ef database update` against staging/production, or a startup migration step guarded by a flag.
- Promote through environments (staging → production) with environment protection rules.
