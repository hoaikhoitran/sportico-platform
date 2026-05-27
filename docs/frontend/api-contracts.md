# Frontend — API Contracts

Wire format: JSON, **camelCase**, enums serialized as **strings**.

## Result<T>

Success:
```json
{ "isSuccess": true, "data": { /* T */ }, "error": null }
```
Failure:
```json
{ "isSuccess": false, "data": null, "error": { "code": "...", "message": "...", "type": "...", "details": ["..."] } }
```

TypeScript:
```ts
type Result<T> =
  | { isSuccess: true;  data: T;    error: null }
  | { isSuccess: false; data: null; error: ApiError };
```

The non-generic `Result` (used by register / verify-email) is:
```json
{ "isSuccess": true, "message": "Registration successful" }
```

## Error shape

```ts
interface ApiError {
  code: string;   // machine code, e.g. "BOOKING_NOT_ACTIVE"
  message: string;
  type: "Validation" | "NotFound" | "Unauthorized" | "Forbidden" | "Conflict" | "Failure";
  details: string[] | null;  // validation messages live here
}
```

HTTP status is derived from `type` by the server middleware:

| type | HTTP |
|---|---|
| Validation | 400 |
| Unauthorized | 401 |
| Forbidden | 403 |
| NotFound | 404 |
| Conflict | 409 |
| Failure | 500 |

> Defensive tip: also treat a `200` body with `isSuccess: false` as an error.

## PagedResult<T>

Always wrapped: `Result<PagedResult<T>>`.

```ts
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

Access items at `response.data.items`.

## Auth header

```
Authorization: Bearer <accessToken>
```

Public endpoints (`/api/auth/*`, `/api/public/training-packages`, `/api/payments/payos/webhook`) need no header.

## Pagination request

List endpoints accept query params; defaults are `pageNumber=1`, `pageSize=10`.

```
GET /api/public/training-packages?keyword=strength&sportId=1&pageNumber=1&pageSize=20
```

Common filter params by module:
- Training packages: `keyword`, `sportId`, `coachId`, `status`.
- Bookings: `status`.
- Sessions / check-ins / transactions / withdrawals / notifications: `pageNumber`, `pageSize` (plus module-specific filters — confirm against each `*FilterRequest`).

## Money & dates

- Monetary values are decimals (e.g. `106250`, `0.15`). Parse as decimal/string; do not use JS `number` for accounting math beyond display.
- Dates are ISO-8601 UTC strings.
