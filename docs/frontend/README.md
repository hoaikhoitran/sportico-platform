# Frontend Docs

How a frontend (React / Next.js assumed) should consume the Sportico backend. Start with [11 — Frontend Integration Guide](../11-frontend-integration-guide.md) for the high-level orientation; these files go deeper per topic.

## Contents

| Doc | Purpose |
|---|---|
| [routes-and-pages.md](routes-and-pages.md) | Recommended page/route map |
| [api-contracts.md](api-contracts.md) | `Result<T>`, `PagedResult<T>`, error shape, auth header, pagination |
| [auth-integration.md](auth-integration.md) | Token storage, refresh, role guards, 401/403 |
| [booking-flow-ui.md](booking-flow-ui.md) | Public listing → detail → purchase → sessions |
| [coach-dashboard-ui.md](coach-dashboard-ui.md) | Packages, bookings, sessions, plans, wallet |
| [learner-dashboard-ui.md](learner-dashboard-ui.md) | Bookings, assessment, sessions, plan, check-ins, chat |
| [admin-dashboard-ui.md](admin-dashboard-ui.md) | Package approval, payout verification, withdrawals |
| [error-handling.md](error-handling.md) | Mapping backend error codes to UI messages |

## Feature Areas (by role)

- **Public**: auth screens, browse/inspect published training packages.
- **Learner**: purchase bookings, request sessions, fill assessment, log progress, chat.
- **Coach**: manage packages, manage bookings, confirm/complete sessions, author training plans, manage wallet/payout/withdrawals.
- **Admin**: approve packages, verify payout accounts, process withdrawals.

## Conventions to respect

- Always send `Authorization: Bearer <accessToken>` on protected calls.
- Treat money as `decimal`/string, never float.
- All list endpoints are paged — read `hasNext`/`totalPages` from `PagedResult`.
- No websockets — poll chat messages and the unread-notification count.
- Check both HTTP status and the body's `isSuccess`.
