# 13 — Environment Variables

Configuration is read through `IConfiguration`, layering `appsettings.json`, environment variables, and (locally) a `.env` file loaded by `LoadEnvIfPresent()` in `Program.cs` and by `AppDbContextFactory` for EF design-time commands.

Use the **double-underscore** form (`Section__Key`) for environment variables / Azure App Settings; Azure maps `__` to the `:` separator used by the config keys. All values below are **placeholders** — never commit real secrets.

## Required

| Env var | Config key | Example / placeholder | Notes |
|---|---|---|---|
| `ConnectionStrings__Default` | `ConnectionStrings:Default` | `Host=...;Port=5432;Database=sportico;Username=...;Password=...;SslMode=Require;Trust Server Certificate=true` | PostgreSQL (Npgsql). Required. |
| `JWT__SecretKey` | `JWT:SecretKey` | `<min-32-char-random-secret>` | HMAC-SHA256 signing key. App fails to start if blank. |
| `JWT__Issuer` | `JWT:Issuer` | `Sportico` | Required at startup. |
| `JWT__Audience` | `JWT:Audience` | `SporticoClient` | Required at startup. |
| `JWT__AccessTokenExpirationMinutes` | `JWT:AccessTokenExpirationMinutes` | `15` | Must be > 0. |
| `JWT__RefreshTokenExpirationDays` | `JWT:RefreshTokenExpirationDays` | `30` | Refresh token lifetime. |

## PayOS (required for PayOS payments)

| Env var | Config key | Placeholder | Notes |
|---|---|---|---|
| `PayOs__ClientId` | `PayOs:ClientId` | `<payos-client-id>` | Sent as `x-client-id`. |
| `PayOs__ApiKey` | `PayOs:ApiKey` | `<payos-api-key>` | Sent as `x-api-key`. |
| `PayOs__ChecksumKey` | `PayOs:ChecksumKey` | `<payos-checksum-key>` | HMAC key for request + webhook signatures. |
| `PayOs__BaseUrl` | `PayOs:BaseUrl` | `https://api-merchant.payos.vn` | PayOS API base. |
| `PayOs__ReturnUrl` | `PayOs:ReturnUrl` | `https://your-frontend/payment/success` | Frontend success page. |
| `PayOs__CancelUrl` | `PayOs:CancelUrl` | `https://your-frontend/payment/cancel` | Frontend cancel page. |
| `PayOs__PaymentLinkExpireMinutes` | `PayOs:PaymentLinkExpireMinutes` | `15` | Must be > 0. |

If any of these are blank, creating a PayOS payment throws `PAYOS_CREATE_PAYMENT_FAILED` listing the missing keys.

## Email (registration verification)

| Env var | Config key | Placeholder | Notes |
|---|---|---|---|
| `EmailSettings__SmtpServer` | `EmailSettings:SmtpServer` | `smtp.gmail.com` | |
| `EmailSettings__Port` | `EmailSettings:Port` | `587` | |
| `EmailSettings__SenderName` | `EmailSettings:SenderName` | `Sportico` | |
| `EmailSettings__SenderEmail` | `EmailSettings:SenderEmail` | `no-reply@example.com` | |
| `EmailSettings__Username` | `EmailSettings:Username` | `<smtp-username>` | |
| `EmailSettings__Password` | `EmailSettings:Password` | `<smtp-app-password>` | Secret. |

## App Settings

| Env var | Config key | Placeholder | Notes |
|---|---|---|---|
| `AppSettings__ApiBaseUrl` | `AppSettings:ApiBaseUrl` | `https://your-app-base-url` | Used to build the email verification link (`{ApiBaseUrl}/api/auth/verify-email?token=...`). Required for registration. |

## CORS (recommended, not yet implemented)

> NOTE: CORS is not configured in the reviewed `Program.cs`. If/when added, expose origins via configuration, e.g. `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`. See [12 — Deployment Guide](12-deployment-guide.md#cors).

## Local `.env`

`LoadEnvIfPresent()` searches up to 5 parent directories for a `.env` file and loads it. `AppDbContextFactory` additionally loads `../SporticoApp.Api/.env` for EF commands. A local `.env` (git-ignored) is the simplest way to run locally:

```env
ConnectionStrings__Default=Host=localhost;Port=5432;Database=sportico;Username=postgres;Password=postgres
JWT__SecretKey=replace-with-a-long-random-secret-min-32-chars
JWT__Issuer=Sportico
JWT__Audience=SporticoClient
JWT__AccessTokenExpirationMinutes=15
JWT__RefreshTokenExpirationDays=30
AppSettings__ApiBaseUrl=http://localhost:5095
EmailSettings__Password=app-password
PayOs__ClientId=...
PayOs__ApiKey=...
PayOs__ChecksumKey=...
PayOs__BaseUrl=https://api-merchant.payos.vn
PayOs__ReturnUrl=http://localhost:3000/payment/success
PayOs__CancelUrl=http://localhost:3000/payment/cancel
```

Provide a committed `.env.example` (the `.gitignore` allows it) with placeholders only.
