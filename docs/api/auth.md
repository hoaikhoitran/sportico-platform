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
