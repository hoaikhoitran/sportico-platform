# Frontend — Error Handling

The backend returns a uniform error envelope (see [api-contracts.md](api-contracts.md)). Map `error.type` to behaviour and `error.code` to specific user-facing copy.

## General strategy

```ts
function handleApiError(error: ApiError) {
  switch (error.type) {
    case "Validation":   return showFieldErrors(error.details ?? [error.message]);
    case "Unauthorized": return goToLoginAfterRefresh();
    case "Forbidden":    return showNotAllowed(error.message);
    case "NotFound":     return showNotFound(error.message);
    case "Conflict":     return showConflict(error.code, error.message);
    default:             return showGenericError(); // Failure / 500
  }
}
```

For `Validation`, render `error.details` (each string is a human-readable message from FluentValidation or model state). For everything else, prefer mapping `error.code` to copy and fall back to `error.message`.

## Code → message map (suggested)

| Code | Suggested message |
|---|---|
| `AUTH_INVALID_CREDENTIALS` | "Incorrect email or password." |
| `COMMON_ACCOUNT_NOT_ACTIVE` | "Please verify your email before signing in." |
| `AUTH_INVALID_REFRESH_TOKEN` / `AUTH_REFRESH_TOKEN_EXPIRED` | "Your session expired. Please sign in again." |
| `USER_EMAIL_ALREADY_EXISTS` | "An account with this email already exists." |
| `COACH_PROFILE_ALREADY_EXISTS` | "You are already registered as a coach." |
| `SPORT_INVALID` | "One or more selected sports are invalid." |
| `TRAINING_PACKAGE_NOT_PUBLISHED` | "This package is not available for purchase." |
| `COMMON_FORBIDDEN` | "You can't perform this action." (e.g. buying your own package) |
| `BOOKING_NOT_ACTIVE` | "This booking isn't active yet." |
| `SESSION_LIMIT_EXCEEDED` | "All sessions in this package are already scheduled." |
| `SCHEDULE_CONFLICT` | "That time overlaps another session. Pick a different slot." |
| `INVALID_TRAINING_SESSION_STATUS` | "This session can't change to that state." |
| `PAYOUT_ACCOUNT_REQUIRED` | "Add and verify a payout account first." |
| `INSUFFICIENT_WALLET_BALANCE` | "Withdrawal exceeds your available balance." |
| `CHAT_NOT_ALLOWED` | "Chat is available after an active booking." |
| `*_NOT_OWNED` | "You don't have access to this resource." |
| `*_NOT_FOUND` | "We couldn't find that." |
| `PAYOS_CREATE_PAYMENT_FAILED` | "Payment couldn't be started. Try again later." |
| `COMMON_INTERNAL_SERVER_ERROR` | "Something went wrong. Please try again." |

Full code list: `SporticoApp.Shared/Constants/ErrorCodes.cs`.

## Validation display

`type: "Validation"` (code `COMMON_VALIDATION_ERROR`) carries messages in `error.details`. If you can map a message to a field, show it inline; otherwise show the list near the form's submit button.

## 401 handling

Intercept globally: on `401`, attempt one refresh (`/api/auth/refresh-token`), retry the original request, and only redirect to login if refresh fails. Don't show a raw error toast for the transparent-refresh case.

## 403 handling

`403` is terminal for that request — the user lacks the role or isn't the owner. Show an inline "not allowed" state; do not retry or refresh.

## Defensive check

Even on HTTP `200`, verify `isSuccess`. If `false`, route the body's `error` through `handleApiError`.
