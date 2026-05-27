# 05 — API Overview

All endpoints are prefixed with `/api`. Responses are JSON, `camelCase`, enums serialized as strings, wrapped in `Result<T>` / `PagedResult<T>`. Authentication is JWT Bearer (see [06 — Auth and Roles](06-auth-and-roles.md)).

Roles: `learner`, `coach`, `admin`. "Any (auth)" means any authenticated user; "Public" means `[AllowAnonymous]`.

Detailed per-endpoint request/response bodies are in [api/](api/).

## Auth — `AuthController` (`/api/auth`)
Account lifecycle: register, verify email, login, refresh token.

| Method | Route | Role |
|---|---|---|
| POST | `/api/auth/register` | Public |
| GET | `/api/auth/verify-email?token=` | Public |
| POST | `/api/auth/login` | Public |
| POST | `/api/auth/refresh-token` | Public |

Coach onboarding — `CoachesController` (`/api/coaches`):

| Method | Route | Role |
|---|---|---|
| POST | `/api/coaches/register` | Any (auth) — grants `coach` role |

See [api/auth.md](api/auth.md).

## Sports — `SportsController` (`/api/sports`)
Sports catalog management.

| Method | Route | Role |
|---|---|---|
| POST | `/api/sports` | Admin |

> NOTE: Only a create endpoint exists on this controller. Public listing of sports was not found in the reviewed controllers.

## Training Packages
Coach-owned offerings, admin moderation, and public catalog.

**Coach — `TrainingPackagesController` (`/api/training-packages`, role `coach`):**

| Method | Route | Purpose |
|---|---|---|
| POST | `/api/training-packages` | Create (status `pending`) |
| GET | `/api/training-packages/me` | List own (paged) |
| GET | `/api/training-packages/me/{id}` | Get own by id |
| PUT | `/api/training-packages/{id}` | Update |
| PUT | `/api/training-packages/{id}/archive` | Archive |

**Admin — `AdminTrainingPackagesController` (`/api/admin/training-packages`, role `admin`):**

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/admin/training-packages/pending` | List pending (paged) |
| PUT | `/api/admin/training-packages/{id}/approve` | Approve → published |
| PUT | `/api/admin/training-packages/{id}/reject` | Reject (with reason) |

**Public — `PublicTrainingPackagesController` (`/api/public/training-packages`, anonymous):**

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/public/training-packages` | List published (paged) |
| GET | `/api/public/training-packages/{id}` | Get by id |

See [api/training-packages.md](api/training-packages.md).

## Bookings — `BookingsController` (`/api/bookings`)
Purchasing packages and viewing bookings.

| Method | Route | Role |
|---|---|---|
| POST | `/api/bookings/purchase/manual` | Learner |
| POST | `/api/bookings/purchase/payos` | Learner |
| GET | `/api/bookings/me` | Learner |
| GET | `/api/bookings/{id}` | Learner |
| GET | `/api/bookings/coach` | Coach |
| GET | `/api/bookings/coach/{id}` | Coach |

See [api/bookings.md](api/bookings.md).

## Training Sessions — `TrainingSessionsController`
Session scheduling within a booking.

| Method | Route | Role |
|---|---|---|
| POST | `/api/bookings/{bookingId}/sessions` | Learner (request) |
| GET | `/api/bookings/{bookingId}/sessions` | Any (auth) participant |
| PUT | `/api/training-sessions/{id}/confirm` | Coach |
| PUT | `/api/training-sessions/{id}/cancel` | Any (auth) participant |
| PUT | `/api/training-sessions/{id}/complete` | Coach |

See [api/training-sessions.md](api/training-sessions.md).

## Personalized Training
Assessment, plan hierarchy, progress check-ins.

**Assessment — `LearnerAssessmentsController`:**

| Method | Route | Role |
|---|---|---|
| POST | `/api/bookings/{bookingId}/assessment` | Learner |
| GET | `/api/bookings/{bookingId}/assessment` | Any (auth) participant |
| PUT | `/api/bookings/{bookingId}/assessment` | Learner |

**Training plan — `TrainingPlansController`:**

| Method | Route | Role |
|---|---|---|
| POST | `/api/bookings/{bookingId}/training-plan` | Coach |
| GET | `/api/bookings/{bookingId}/training-plan` | Any (auth) participant |
| PUT | `/api/training-plans/{id}` | Coach |
| POST | `/api/training-plans/{id}/weeks` | Coach |
| POST | `/api/training-plan-weeks/{weekId}/days` | Coach |
| POST | `/api/training-plan-days/{dayId}/exercises` | Coach |
| PUT | `/api/training-plan-exercises/{id}` | Coach |
| DELETE | `/api/training-plan-exercises/{id}` | Coach |

**Progress check-ins — `ProgressCheckInsController`:**

| Method | Route | Role |
|---|---|---|
| POST | `/api/bookings/{bookingId}/progress-checkins` | Learner |
| GET | `/api/bookings/{bookingId}/progress-checkins` | Any (auth) participant |
| PUT | `/api/progress-checkins/{id}/coach-feedback` | Coach |

See [api/personalized-training.md](api/personalized-training.md).

## Wallet & Withdrawals
Coach earnings, payout accounts, and withdrawal lifecycle.

**Wallet — `CoachWalletsController` (role `coach`):**

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/coaches/me/wallet` | Wallet balances |
| GET | `/api/coaches/me/wallet/transactions` | Ledger (paged) |

**Payout account — `CoachPayoutAccountsController` (`/api/coaches/me/payout-account`, role `coach`):**

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/coaches/me/payout-account` | Get own |
| PUT | `/api/coaches/me/payout-account` | Upsert (resets to pending) |

**Admin payout verification — `AdminCoachPayoutAccountsController` (`/api/admin/coach-payout-accounts`, role `admin`):**

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/admin/coach-payout-accounts/pending` | List pending |
| PUT | `/api/admin/coach-payout-accounts/{id}/verify` | Verify |
| PUT | `/api/admin/coach-payout-accounts/{id}/reject` | Reject |

**Withdrawals — `WithdrawalRequestsController`:**

| Method | Route | Role |
|---|---|---|
| POST | `/api/coaches/me/withdrawal-requests` | Coach |
| GET | `/api/coaches/me/withdrawal-requests` | Coach |
| GET | `/api/admin/withdrawal-requests/pending` | Admin |
| PUT | `/api/admin/withdrawal-requests/{id}/approve` | Admin |
| PUT | `/api/admin/withdrawal-requests/{id}/reject` | Admin |
| PUT | `/api/admin/withdrawal-requests/{id}/mark-paid` | Admin |

See [api/wallet-withdrawals.md](api/wallet-withdrawals.md).

## Chat — `ChatController` (`/api/chat`, any auth)

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/chat/rooms` | List rooms for current user |
| GET | `/api/chat/rooms/{roomId}/messages` | Messages (paged) |
| POST | `/api/chat/rooms/{roomId}/messages` | Send message |

See [api/chat.md](api/chat.md).

## Notifications — `NotificationsController` (`/api/notifications`, any auth)

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/notifications/me` | List (paged) |
| GET | `/api/notifications/me/unread-count` | Unread count |
| PUT | `/api/notifications/{id}/read` | Mark one read |
| PUT | `/api/notifications/me/read-all` | Mark all read |

See [api/notifications.md](api/notifications.md).

## Payments / PayOS — `PaymentsController` (`/api/payments`)

| Method | Route | Role |
|---|---|---|
| POST | `/api/payments/payos/webhook` | Public (signature-verified) |

Purchase link creation is on the bookings controller (`/api/bookings/purchase/payos`). See [api/payments.md](api/payments.md).

## Admin Summary
Admin-only capabilities are spread across controllers. See [api/admin.md](api/admin.md) for the consolidated list.

## Legacy Endpoints (do not build on)
- `PackagesController` (`/api/packages`) — subscription tiers.
- `CoachPackagesController` (`/api/coach-packages`) — coach subscription purchase/history.
- `PostsController` (`/api/posts`) and `AdminPostsController` (`/api/admin/posts`) — coach service posts and moderation.

See [17 — Legacy Modules](17-legacy-modules.md).
