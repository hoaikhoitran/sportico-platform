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
| `PayOs__AutoPayoutEnabled` | `PayOs:AutoPayoutEnabled` | `false` | **Legacy fallback** for the withdrawal payout mode — only used when `PayOsPayout__AutoPayoutEnabled` is unset. Prefer the dedicated `PayOsPayout__*` keys below. |
| `PayOs__PayoutCategory` | `PayOs:PayoutCategory` | `salary` | **Legacy fallback** for the payout category — only used when `PayOsPayout__PayoutCategory` is unset. |

If any of the inbound-payment keys are blank, creating a PayOS payment throws `PAYOS_CREATE_PAYMENT_FAILED` listing the missing keys.

## PayOS Payout / Chi channel (required for auto payouts)

Outbound coach withdrawals use a **dedicated PayOS Payout ("Kênh chuyển tiền" / Chi) channel** whose
credentials are kept separate from the inbound payment-link channel above — PayOS may issue a different
`ClientId` / `ApiKey` / `ChecksumKey` per channel. Bound from the `PayOsPayout` section; the
`AutoPayoutEnabled` / `PayoutCategory` values fall back to the legacy `PayOs__*` keys when unset.

| Env var | Config key | Placeholder | Notes |
|---|---|---|---|
| `PayOsPayout__ClientId` | `PayOsPayout:ClientId` | `<payout-client-id>` | Sent as `x-client-id` on payout calls. |
| `PayOsPayout__ApiKey` | `PayOsPayout:ApiKey` | `<payout-api-key>` | Sent as `x-api-key` on payout calls. |
| `PayOsPayout__ChecksumKey` | `PayOsPayout:ChecksumKey` | `<payout-checksum-key>` | HMAC key for payout request signatures. |
| `PayOsPayout__BaseUrl` | `PayOsPayout:BaseUrl` | `https://api-merchant.payos.vn` | Payout API base (falls back to `PayOs__BaseUrl`, then the PayOS default). |
| `PayOsPayout__AutoPayoutEnabled` | `PayOsPayout:AutoPayoutEnabled` | `false` | **Withdrawal payout mode.** `false` (default) = manual: admin approves, transfers externally, then marks paid. `true` = admin **approval** triggers an automatic PayOS Chi payout. Coach withdrawal requests never send money in either mode. Set `PayOsPayout__AutoPayoutEnabled=true` in production to enable automatic payouts. |
| `PayOsPayout__PayoutCategory` | `PayOsPayout:PayoutCategory` | *(empty)* | **Optional.** PayOS payout category (e.g. `salary`, `business`). Leave unset to omit `category` from the request body entirely — PayOS rejects unrecognised category values, so only set this when your PayOS Chi merchant account explicitly requires it. |

When auto mode is on and a payout is attempted with any of `ClientId` / `ApiKey` / `ChecksumKey` blank,
the payout fails loudly with `PAYOS_PAYOUT_FAILED` listing the missing keys (it never calls PayOS with
empty credentials). Auto payout additionally requires a verified coach payout account (`BankBin`,
`BankAccountNumber`).

## Withdrawal payout reconciliation (background job)

Periodically polls PayOS for `processing` withdrawals and finalizes them to `paid`/`failed`. All keys
are optional — sensible defaults make it work out of the box.

| Env var | Config key | Default | Notes |
|---|---|---|---|
| `WithdrawalPayoutReconciliation__Enabled` | `WithdrawalPayoutReconciliation:Enabled` | `true` | Master switch for the background loop. |
| `WithdrawalPayoutReconciliation__IntervalSeconds` | `WithdrawalPayoutReconciliation:IntervalSeconds` | `60` | Seconds between passes (min 10). |
| `WithdrawalPayoutReconciliation__BatchSize` | `WithdrawalPayoutReconciliation:BatchSize` | `20` | Max processing withdrawals reconciled per pass. |

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

## Gemini (Advisory Chatbot)

Powers the AI advisory chatbot (`POST /api/v1/advisory/messages`, roles `learner` + `admin`) that gives
sports-training advice and recommends coaches. Calls the Google Generative Language `generateContent` endpoint.

| Env var | Config key | Placeholder | Notes |
|---|---|---|---|
| `Gemini__ApiKey` | `Gemini:ApiKey` | `<google-generative-language-api-key>` | **Secret.** Never commit. Locally set via user secrets: `dotnet user-secrets set "Gemini:ApiKey" "<key>" --project src/SporticoApp.Api`. When blank, the advisory endpoint fails with `ADVISORY_REPLY_FAILED`. |
| `Gemini__Model` | `Gemini:Model` | `gemini-2.0-flash` | Model id used for `generateContent`. Defaults to `gemini-2.0-flash`. |
| `Gemini__BaseUrl` | `Gemini:BaseUrl` | `https://generativelanguage.googleapis.com` | API base. Optional — defaults to the Google Generative Language host. |

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
# Outbound payout (Chi) channel — separate credentials; leave AutoPayoutEnabled=false locally
PayOsPayout__ClientId=...
PayOsPayout__ApiKey=...
PayOsPayout__ChecksumKey=...
PayOsPayout__BaseUrl=https://api-merchant.payos.vn
PayOsPayout__AutoPayoutEnabled=false
PayOsPayout__PayoutCategory=salary

# ── Google sign-in ────────────────────────────────────────────────────────────
# GOOGLE_CLIENT_ID is public (the frontend needs it for Google Identity Services).
# GOOGLE_CLIENT_SECRET is BACKEND-ONLY — never expose it to the browser or commit it.
GOOGLE_CLIENT_ID=<google-oauth-client-id>.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=<google-oauth-client-secret>
GOOGLE_CALLBACK_URL=https://sportico.click/api/auth/google/callback
FRONTEND_URL=https://sportico-fe.vercel.app
```

## The single local environment file

**`<repository-root>/.env` is the only environment file this repository loads.**

Both the API host (`Program.LoadEnvIfPresent`) and the EF design-time factory
(`AppDbContextFactory`) call the same helper, `SporticoApp.Shared.Configuration.EnvironmentFileLoader`,
which locates the repository root by walking up from the current working directory (then from
`AppContext.BaseDirectory`) until it finds `SporticoApp.Api.sln`, and loads `<root>/.env`.

This matters because `dotnet ef` runs with the **startup project's** folder as its working
directory. The factory previously used the relative path `../SporticoApp.Api/.env`, so
`dotnet ef database update` and `dotnet run` could read **different files** and therefore migrate a
different database than the application talked to.

> ⚠️ **Do not create `src/SporticoApp.Api/.env`.** It is no longer read. If the file exists, startup
> and EF tooling print a warning that it was ignored — the warning never prints its contents.

Startup prints only non-sensitive facts, never a key name or value:

```
[config] Environment file loaded from repository root.
[ef] Database target: Supabase (session pooler)
```

### Supabase: direct endpoint vs. session pooler

Supabase exposes two endpoints for the same database:

| Endpoint | Host pattern | DNS | Use when |
|---|---|---|---|
| Direct | `db.<project-ref>.supabase.co` | **AAAA only (IPv6)** | Your network has working outbound IPv6 |
| Session pooler | `aws-<n>-<region>.pooler.supabase.com` | A (IPv4) + AAAA | **IPv4-only networks** — most local machines and CI runners |

If `dotnet ef database update` fails with
`SocketException … hostname resolution` / `SocketErrorCode=NoData`, the cause is almost always that
the direct endpoint is IPv6-only while the machine has no IPv6 route. Use the **session pooler**
connection string from *Supabase Dashboard → Connect → Session pooler*; note it uses the username
form `postgres.<project-ref>`. Put that string in `ConnectionStrings__Default` so runtime and EF
tooling share one working endpoint.

## Google sign-in configuration

| Variable | Required for | Notes |
|---|---|---|
| `GOOGLE_CLIENT_ID` | Both flows | Public value. Must match the `aud` of every accepted Google ID token. |
| `GOOGLE_CLIENT_SECRET` | Redirect flow only | **Secret.** Backend only. |
| `GOOGLE_CALLBACK_URL` | Redirect flow only | Absolute URL; must be HTTPS outside Development (plain `http` is accepted only on loopback). Its *path* becomes the handler's `CallbackPath`, and it must exactly match the redirect URI registered in the Google Cloud console. |
| `FRONTEND_URL` | Redirect flow only | Absolute base URL of the SPA. The post-login hop is always rebuilt as `{FRONTEND_URL}/auth/google/callback`, so a request can never redirect elsewhere. |

The .NET-convention form is also supported and takes **priority** when set:
`GoogleAuth__ClientId`, `GoogleAuth__ClientSecret`, `GoogleAuth__CallbackUrl`, `GoogleAuth__FrontendUrl`,
`GoogleAuth__ExchangeCodeLifetimeSeconds` (default 90, clamped to 30–300).

**Behaviour when Google configuration is absent:** the application still starts and every non-Google
endpoint keeps working. The Google endpoints answer `503 AUTH_GOOGLE_CONFIGURATION_MISSING`, whose
`details` array lists the missing configuration **key names only** — never a value.

Provide a committed `.env.example` (the `.gitignore` allows it) with placeholders only.
