# 11 — Frontend Integration Guide

For frontend engineers (React / Next.js assumed). This is the orientation doc; deeper material is in [frontend/](frontend/).

## API Base URL

| Environment | Base URL |
|---|---|
| Local | `http://localhost:5095` or `https://localhost:7058` (see `launchSettings.json`) |
| Production | `https://sportico-api-khoi.azurewebsites.net` |

All routes are under `/api`. Swagger UI is at the **root** (`/`).

Expose the base URL through an env var, e.g. `NEXT_PUBLIC_API_BASE_URL`.

## Response Envelope

Every JSON response is `camelCase` with enums as strings.

```ts
type Result<T> =
  | { isSuccess: true;  data: T;   error: null }
  | { isSuccess: false; data: null; error: ApiError };

interface ApiError {
  code: string;        // e.g. "BOOKING_NOT_ACTIVE"
  message: string;
  type: "Validation" | "NotFound" | "Unauthorized" | "Forbidden" | "Conflict" | "Failure";
  details: string[] | null;
}

interface PagedResult<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}
```

`register` and `verify-email` return the non-generic `{ isSuccess, message }`.

## Auth Token Handling

1. `POST /api/auth/login` → store `accessToken` (15-min lifetime) and `refreshToken` (30-day).
2. Send `Authorization: Bearer <accessToken>` on every protected request.
3. On `401`, call `POST /api/auth/refresh-token` with `{ email, refreshToken }`, store the rotated tokens, and retry once. If refresh fails, redirect to login.

Storage: prefer in-memory access token + httpOnly cookie or secure storage for the refresh token. Avoid `localStorage` for tokens if XSS is a concern.

## Axios Setup (example)

```ts
import axios from "axios";

export const api = axios.create({
  baseURL: process.env.NEXT_PUBLIC_API_BASE_URL,
});

api.interceptors.request.use((config) => {
  const token = getAccessToken();
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

api.interceptors.response.use(
  (res) => res,
  async (error) => {
    if (error.response?.status === 401 && !error.config._retried) {
      error.config._retried = true;
      const ok = await tryRefresh(); // calls /api/auth/refresh-token
      if (ok) {
        error.config.headers.Authorization = `Bearer ${getAccessToken()}`;
        return api(error.config);
      }
    }
    return Promise.reject(error);
  }
);
```

Because the body itself carries `isSuccess`/`error`, treat a `200` with `isSuccess: false` as an error too (the backend mostly uses HTTP status codes via the middleware, but defensively check `isSuccess`).

## Roles & Protected Routes

Decode the JWT (or track roles from a profile call) to guard routes:

- `learner` — browse/purchase, request sessions, assessment, check-ins.
- `coach` — package management, bookings, session confirm/complete, plan authoring, wallet/withdrawals.
- `admin` — package approval, payout verification, withdrawals.

A user can hold multiple roles. Guard by capability, not by assuming a single role.

## Main Pages Needed

| Page | Primary role | Key endpoints |
|---|---|---|
| Login / Register | Public | `auth/login`, `auth/register`, `auth/verify-email` |
| Public training package listing | Public | `public/training-packages` |
| Training package detail | Public | `public/training-packages/{id}` |
| Coach dashboard | Coach | `bookings/coach`, `training-packages/me` |
| Coach create/edit package | Coach | `training-packages` (POST/PUT/archive) |
| Admin package approval | Admin | `admin/training-packages/pending`, approve/reject |
| Learner bookings | Learner | `bookings/me` |
| Coach bookings | Coach | `bookings/coach` |
| Session scheduling | Learner/Coach | `bookings/{id}/sessions`, `training-sessions/{id}/confirm\|cancel\|complete` |
| Training plan view | Both | `bookings/{id}/training-plan` |
| Assessment form | Learner | `bookings/{id}/assessment` |
| Progress check-in | Learner/Coach | `bookings/{id}/progress-checkins`, `progress-checkins/{id}/coach-feedback` |
| Wallet | Coach | `coaches/me/wallet`, `.../transactions` |
| Withdrawal | Coach | `coaches/me/withdrawal-requests`, `coaches/me/payout-account` |
| Chat | Both | `chat/rooms`, `chat/rooms/{id}/messages` |
| Notifications | All | `notifications/me`, unread-count, read, read-all |

## Recommended State Flow

- **Auth state**: tokens + decoded roles, refreshed transparently by the axios interceptor.
- **Server state**: use React Query / SWR keyed by resource (`["bookings","me",filter]`). Invalidate after mutations (e.g. after `complete` session, invalidate wallet + booking + sessions).
- **Pagination**: keep `pageNumber`/`pageSize` in query state; read `hasNext`/`totalPages` from `PagedResult`.
- **Polling**: poll chat messages and `notifications/me/unread-count` on an interval (no websockets).

## Error Handling with Result<T>

Map `error.type` to UX and `error.code` to specific messages. See [frontend/error-handling.md](frontend/error-handling.md). Always surface `error.details` for validation (`type: "Validation"`), since FluentValidation and model-state messages land there.

## Validation Errors

`type: "Validation"`, `code: "COMMON_VALIDATION_ERROR"`, with `details: string[]` of human-readable messages. Render them inline near the relevant fields (or as a list if you cannot map them to fields).

## Handling 401 / 403

- `401` → refresh once, then redirect to login.
- `403` → the user is authenticated but lacks the role or is not the owner (`*_NOT_OWNED`). Show a "not allowed" state; do not retry.
