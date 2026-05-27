# Deployment — Production Checklist

Run through this before and after a production release.

## Secrets & configuration
- [ ] `ConnectionStrings__Default` set in Azure App Settings (not in `appsettings.json`).
- [ ] `JWT__SecretKey` is a strong, unique, ≥32-char secret (app fails to start if blank).
- [ ] `JWT__Issuer`, `JWT__Audience` set and consistent with token issuance.
- [ ] `AppSettings__ApiBaseUrl` set (used for email verification links).
- [ ] `PayOs__ClientId/ApiKey/ChecksumKey/BaseUrl/ReturnUrl/CancelUrl` set (if PayOS is enabled).
- [ ] `EmailSettings__*` set (if registration email is enabled).
- [ ] No secrets committed to git (`.env`, `appsettings.*.Local.json`, `secrets.json`, `*.mcp.json` are git-ignored — verify nothing slipped in).

## CORS
- [ ] If a browser frontend on another origin calls the API, a CORS policy is configured. **Currently CORS is not registered** ([12 — Deployment Guide](../12-deployment-guide.md#cors)) — add it before shipping a web client.

## HTTPS
- [ ] App Service enforces HTTPS; `UseHttpsRedirection()` is active.
- [ ] PayOS `ReturnUrl`/`CancelUrl` and `AppSettings__ApiBaseUrl` use HTTPS.

## Logging
- [ ] Log stream reviewed; no secrets logged.
- [ ] (Recommended) Application Insights or equivalent wired for telemetry.

## Swagger exposure
- [ ] Decide whether Swagger should be public in production. It is **on at the site root by default**. Disable or protect it if you don't want it exposed.

## PayOS configuration
- [ ] Merchant credentials are production keys (not sandbox) if going live.
- [ ] Webhook URL registered with PayOS points at `https://<host>/api/payments/payos/webhook`.
- [ ] Checksum key matches between app config and the PayOS dashboard (webhook signature verification is fail-closed).

## Database
- [ ] Migrations applied to production (`dotnet ef database update`).
- [ ] Backups enabled; a manual backup taken before destructive migrations.
- [ ] `roles` table seeded with `learner`, `coach`, `admin`.
- [ ] At least one active `sports` row exists.

## Admin account
- [ ] At least one user has the `admin` role (granted via a `user_roles` row — no self-service endpoint).

## Health & smoke test
- [ ] (Recommended) `/health` endpoint added and monitored.
- [ ] Post-deploy smoke test run ([15 — Testing and Smoke Test](../15-testing-and-smoke-test.md)): login → create/approve package → purchase → request/confirm/complete session → wallet credit → withdrawal.
