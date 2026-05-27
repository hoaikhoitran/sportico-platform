# Frontend — Auth Integration

## Tokens

`POST /api/auth/login` returns:
```json
{ "accessToken": "<JWT, ~15 min>", "refreshToken": "<opaque, ~30 days>", "expiresAt": "ISO-8601" }
```

- **Access token** — short-lived JWT; send as `Authorization: Bearer <accessToken>`.
- **Refresh token** — long-lived; used with the user's email to mint new tokens.

### Storage recommendations
- Keep the access token in memory (or a non-persistent store).
- Store the refresh token in an httpOnly secure cookie if you control a BFF; otherwise secure storage. Avoid `localStorage` for tokens where XSS is a risk.

## Reading roles

The JWT carries role claims (`ClaimTypes.Role`, one per role). Decode the token client-side (no signature verification needed for display — never trust it for security decisions, the server enforces auth) to drive UI and route guards.

```ts
import { jwtDecode } from "jwt-decode";
const claims = jwtDecode<Record<string, unknown>>(accessToken);
// role claim key is the .NET role URI; normalize it:
const roles = ([] as string[]).concat(claims["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] as any ?? []);
```

> NOTE: .NET emits role claims under the long schema URI. Normalize to `learner | coach | admin`.

## Refresh flow

On a `401` (and not already retried):
```
POST /api/auth/refresh-token
{ "email": "<user email>", "refreshToken": "<stored refresh token>" }
```
- Success → store the **rotated** `accessToken` + `refreshToken`, retry the original request once.
- Failure (`AUTH_INVALID_REFRESH_TOKEN`, `AUTH_REFRESH_TOKEN_EXPIRED`, `COMMON_ACCOUNT_NOT_ACTIVE`) → clear session, redirect to `/login`.

Because refresh requires the email, persist the email alongside the refresh token.

## Route guards

```ts
function requireRole(required: "learner" | "coach" | "admin") {
  const roles = getRoles();
  if (!roles.includes(required)) redirect("/"); // or a 403 page
}
```

- Public routes: none.
- Coach/Learner/Admin routes: guard by capability. A user may hold several roles — render every dashboard they qualify for.

## 401 vs 403

| Status | Meaning | UX |
|---|---|---|
| 401 | Not authenticated / token expired / account inactive | Try refresh once, else go to login |
| 403 | Authenticated but lacks role, or not the resource owner (`*_NOT_OWNED`) | Show "not allowed"; do not retry |

## Logout

No server logout endpoint. Discard the access + refresh tokens client-side. Access tokens expire quickly; for stricter invalidation the server would need a refresh-token revocation endpoint (not currently present).
