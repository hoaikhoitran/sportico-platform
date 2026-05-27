# 16 — Troubleshooting

Each entry: symptom → likely cause → fix.

## `relation "..." does not exist`
- **Symptom**: Postgres error on the first query touching a table; 500 response.
- **Cause**: Migrations not applied to the target database.
- **Fix**: `dotnet ef database update --project src/SporticoApp.Infrastructure --startup-project src/SporticoApp.Api` against that database.

## Migration not applied / schema out of date
- **Symptom**: Missing column errors after pulling new code.
- **Cause**: A new migration was added but not run.
- **Fix**: Re-run `database update`. Confirm the migration is committed (`src/SporticoApp.Infrastructure/Migrations`). See [deployment/migration-strategy.md](deployment/migration-strategy.md).

## JWT 401 on protected endpoints
- **Symptom**: `401 Unauthorized`.
- **Causes**: missing/expired access token; account not `active`; clock skew (validated with **zero** `ClockSkew`); wrong issuer/audience/secret between token issuance and validation.
- **Fix**: Send `Authorization: Bearer <accessToken>`. Refresh via `/api/auth/refresh-token`. Ensure server clock is correct. Ensure `JWT__Issuer/Audience/SecretKey` are identical to those used when the token was minted.

## Role 403 (Forbidden)
- **Symptom**: `403` with `COMMON_FORBIDDEN` or a `*_NOT_OWNED` code.
- **Causes**: user lacks the required role; or is authenticated but not the owner of the resource (e.g. coach acting on another coach's session, learner reading another learner's booking).
- **Fix**: Use the correct role's account; operate only on resources you own. For coach features, ensure `/api/coaches/register` was completed (grants `coach` role).

## Swagger "Authorize" not attaching token
- **Symptom**: Protected calls still 401 from Swagger.
- **Cause**: Pasting `Bearer <token>` (the scheme is configured as bearer/JWT; the field expects the token only).
- **Fix**: Paste only the raw JWT in the Authorize dialog.

## PayOS create payment failed (`PAYOS_CREATE_PAYMENT_FAILED`)
- **Symptom**: 500 with `details` listing missing keys, or a PayOS error payload.
- **Causes**: blank `PayOs__ClientId/ApiKey/ChecksumKey/BaseUrl/ReturnUrl/CancelUrl` or non-positive `PaymentLinkExpireMinutes`; or PayOS rejected the request (non-`00` code).
- **Fix**: Set all PayOS env vars ([13 — Environment Variables](13-environment-variables.md)). Inspect the logged raw response; verify the merchant credentials and that `amount` is a positive integer.

## PayOS webhook invalid signature
- **Symptom**: `400 COMMON_VALIDATION_ERROR` "Invalid webhook signature".
- **Causes**: wrong `ChecksumKey`; signature not computed over the canonical `data` (keys sorted ascending, `key=value` joined by `&`, `signature` field excluded); missing signature; `data` not a JSON object. The verifier is fail-closed.
- **Fix**: Recompute HMAC-SHA256 of the canonical data with the correct `ChecksumKey`. Confirm the gateway is configured with the matching checksum key.

## Schedule conflict (`SCHEDULE_CONFLICT`)
- **Symptom**: `409` when requesting a session.
- **Cause**: The proposed `[startTime, endTime)` overlaps an existing `requested`/`scheduled` session for the same coach or learner.
- **Fix**: Choose a non-overlapping slot.

## Session limit exceeded (`SESSION_LIMIT_EXCEEDED`)
- **Symptom**: `409` when requesting a session.
- **Cause**: Count of `requested + scheduled + completed` sessions has reached `Booking.TotalSessions`.
- **Fix**: Cancel an unused requested/scheduled session, or recognize the package is fully scheduled.

## Wallet not credited after completing a session
- **Symptom**: `availableBalance` unchanged after `complete`.
- **Causes**: the session was not in `scheduled` state (only `scheduled → completed` credits); `perSessionCoachAmount` is 0 because the package had `sessionCount = 0`.
- **Fix**: Confirm the session first (`requested → scheduled`), then complete. Ensure packages are created with a positive `sessionCount`.

## Chat not allowed (`CHAT_NOT_ALLOWED`)
- **Symptom**: `403` reading/sending messages.
- **Causes**: caller is not a participant of the room; or the two users have no `active`/`completed` booking together.
- **Fix**: Chat requires a shared active/completed booking. Purchase/activate a booking first.

## Duplicate insert from no-tracking navigation
<a id="duplicate-insert-from-no-tracking-navigation"></a>
- **Symptom**: Unexpected INSERT / unique-violation on `sports` or `training_packages` when creating a booking or post.
- **Cause**: Assigning a navigation property loaded with `AsNoTracking()` onto a new entity before `Add`. EF marks the existing related entity as `Added` and tries to re-insert it.
- **Fix**: Set the FK id (e.g. `TrainingPackageId`) instead of attaching the navigation; assign the navigation **after** `SaveChanges` only for response mapping. This is exactly how `BookingService.CreateBookingSnapshot` and `PostService` handle it.

## appsettings secrets blank
- **Symptom**: Startup `InvalidOperationException` (JWT config missing) or runtime failures connecting to DB / PayOS / SMTP.
- **Cause**: `appsettings.json` intentionally ships blank secrets; values must come from env vars / `.env` / Azure App Settings.
- **Fix**: Provide the keys via environment ([13 — Environment Variables](13-environment-variables.md)). Do not put real secrets in `appsettings.json`.

## Account not active on login
- **Symptom**: `401 COMMON_ACCOUNT_NOT_ACTIVE`.
- **Cause**: Email not verified; user status is `inactive`.
- **Fix**: Complete `GET /api/auth/verify-email?token=...` or set `users.status = 'active'` for testing.
