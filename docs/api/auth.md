# API — Auth

Controllers: `AuthController` (`/api/auth`), `CoachesController` (`/api/coaches`).
Purpose: account registration, email verification, login, token refresh, and coach onboarding.

All responses use the `Result` / `Result<T>` envelope (see [api-contracts](../frontend/api-contracts.md)).

## POST /api/auth/register
- **Role**: Public.
- **Body**:
```json
{ "email": "user@example.com", "password": "Passw0rd!", "fullName": "Jane Doe" }
```
Validation (data annotations): email required/valid/≤320; password required/8–100; fullName required/2–150.
- **Response** (`Result`): `{ "isSuccess": true, "message": "Registration successful" }`.
- **Effects**: creates an `inactive` user, grants `learner` role, sends a verification email.
- **Errors**: `409 USER_EMAIL_ALREADY_EXISTS`; `404 COMMON_ROLE_NOT_FOUND` (learner role missing); `400 COMMON_VALIDATION_ERROR`.

## GET /api/auth/verify-email?token={token}
- **Role**: Public.
- **Response** (`Result`): `{ "isSuccess": true, "message": "Email verified successfully" }`.
- **Effects**: sets user `active`, clears the verification token.
- **Errors**: `400 AUTH_INVALID_VERIFICATION_TOKEN` (missing/invalid token).

## POST /api/auth/login
- **Role**: Public.
- **Body**: `{ "email": "...", "password": "..." }`.
- **Response** (`Result<LoginResponse>`):
```json
{ "isSuccess": true, "data": { "accessToken": "<JWT>", "refreshToken": "<opaque>", "expiresAt": "2026-05-27T10:15:00Z" } }
```
- **Errors**: `401 AUTH_INVALID_CREDENTIALS` (bad email/password); `401 COMMON_ACCOUNT_NOT_ACTIVE` (not verified).

## POST /api/auth/refresh-token
- **Role**: Public.
- **Body**: `{ "email": "...", "refreshToken": "<opaque>" }`.
- **Response** (`Result<RefreshTokenResponse>`): same shape as login (`accessToken`, `refreshToken`, `expiresAt`). Tokens are **rotated**.
- **Errors**: `401 AUTH_INVALID_REFRESH_TOKEN`; `401 AUTH_REFRESH_TOKEN_EXPIRED`; `401 COMMON_ACCOUNT_NOT_ACTIVE`; `400 COMMON_VALIDATION_ERROR`.

## POST /api/auth/google — Google sign-in (ID token)
- **Role**: Public.
- **Body**: `{ "idToken": "<Google Identity Services credential>" }` (required, ≤ 8192 chars).
- **Response** (`Result<LoginResponse>`): identical shape to `/api/auth/login`.
- **Effects**: verifies the token's signature/issuer/audience/expiry against Google, then creates or links a Sportico account. New accounts are `active`, get the `learner` role, and have `password_hash = NULL`.
- **Errors**: `401 AUTH_GOOGLE_INVALID_TOKEN`; `401 AUTH_GOOGLE_EMAIL_NOT_VERIFIED`; `409 AUTH_GOOGLE_ACCOUNT_CONFLICT` (this Sportico account is already linked to a different Google account); `403 COMMON_ACCOUNT_NOT_ACTIVE` (banned); `401 COMMON_ACCOUNT_NOT_ACTIVE` (not active); `503 AUTH_GOOGLE_CONFIGURATION_MISSING`; `400 COMMON_VALIDATION_ERROR`.

## GET /api/auth/google — start browser redirect login
- **Role**: Public. **This is a browser navigation, not a fetch/XHR call.**
- **Response**: `302` to `https://accounts.google.com/...` with scopes `openid email profile` and `redirect_uri` = the configured `GOOGLE_CALLBACK_URL`.
- **Errors**: `503 AUTH_GOOGLE_CONFIGURATION_MISSING` (`details` lists the missing configuration KEY NAMES only).

## GET /api/auth/google/callback — Google's callback
- Handled by the ASP.NET Google handler. **The frontend never calls this directly.** The path comes from `GOOGLE_CALLBACK_URL`.

## GET /api/auth/google/complete — finish external login
- Reads the temporary external cookie, resolves/links the Sportico account, mints a **one-time exchange code**, clears the cookie, and redirects to:
  - success → `{FRONTEND_URL}/auth/google/callback?code=<one-time-code>`
  - failure → `{FRONTEND_URL}/auth/google/callback?error=<STABLE_ERROR_CODE>`
- **Access and refresh tokens are never placed in a URL.**

## POST /api/auth/google/exchange — trade the code for tokens
- **Role**: Public.
- **Body**: `{ "code": "<one-time-code>" }` (required, ≤ 256 chars).
- **Response** (`Result<LoginResponse>`): same shape as `/api/auth/login`.
- **Effects**: consumes the code atomically (single conditional UPDATE), so two concurrent requests can never both obtain tokens.
- **Errors**: `401 AUTH_GOOGLE_EXCHANGE_CODE_INVALID`; `401 AUTH_GOOGLE_EXCHANGE_CODE_EXPIRED`; `409 AUTH_GOOGLE_EXCHANGE_CODE_ALREADY_USED`; `400 COMMON_VALIDATION_ERROR`.

> **Google-only accounts and passwords.** An account created through Google has `password_hash = NULL`.
> Password login for it returns `401 AUTH_INVALID_CREDENTIALS` (never a 500), and
> `POST /api/auth/change-password` returns `409 AUTH_PASSWORD_NOT_SET`. To gain a local password the
> user goes through forgot-password → reset-password.

## POST /api/coaches/register
- **Role**: Any authenticated user. Grants the `coach` role and creates a coach profile.
- **Body**:
```json
{ "headline": "Certified strength coach", "bio": "...", "experienceYears": 5, "sportIds": [1, 2] }
```
Validation: headline required/5–255; bio ≤2000; experienceYears 0–60.
- **Response** (`Result<CoachProfileResponse>`): the created coach profile.
- **Effects**: creates `CoachProfile`, links `CoachSport`s, grants `coach` role.
- **Errors**: `404 USER_NOT_FOUND`; `401 COMMON_ACCOUNT_NOT_ACTIVE`; `409 COACH_PROFILE_ALREADY_EXISTS`; `404 COMMON_ROLE_NOT_FOUND`; `400 SPORT_INVALID` (one or more sport ids invalid/inactive); `400 COMMON_VALIDATION_ERROR`.

> NOTE: There is no `/me` profile endpoint or logout endpoint in the reviewed controllers. Logout is client-side (discard tokens); access tokens are short-lived.
