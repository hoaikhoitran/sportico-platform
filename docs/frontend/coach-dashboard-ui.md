# Frontend — Coach Dashboard UI

Role: `coach`. Requires a coach profile (created via `POST /api/coaches/register`).

## Package management
- List own: `GET /api/training-packages/me` (paged; filter by `status`, `keyword`, `sportId`).
- Create: `POST /api/training-packages` → starts `pending` (awaits admin approval).
- Edit: `PUT /api/training-packages/{id}`.
- Archive: `PUT /api/training-packages/{id}/archive`.
- Show the status badge prominently (`pending`, `published`, `rejected` + `rejectionReason`, `archived`). Only `published` packages are purchasable.

## Booking management
- List: `GET /api/bookings/coach` (paged; filter by `status`).
- Detail: `GET /api/bookings/coach/{id}` — show learner, package, commission snapshot, session progress (`completedSessions/totalSessions`).

## Session confirm / complete
On a booking detail:
- Sessions: `GET /api/bookings/{bookingId}/sessions`.
- Confirm a `requested` session: `PUT /api/training-sessions/{id}/confirm` with optional `{ location, meetingUrl, coachNote }` → `scheduled`.
- Complete a `scheduled` session: `PUT /api/training-sessions/{id}/complete` → `completed`, credits wallet.
- Cancel: `PUT /api/training-sessions/{id}/cancel` with `{ reason }`.
- After completing, refresh the wallet and the booking (`completedSessions` increments; booking may flip to `completed`).

## Training plan authoring
On a booking detail (coach is the author):
1. Read the learner's assessment: `GET /api/bookings/{bookingId}/assessment`.
2. Create the plan: `POST /api/bookings/{bookingId}/training-plan`.
3. Build the tree:
   - Add week: `POST /api/training-plans/{planId}/weeks`.
   - Add day: `POST /api/training-plan-weeks/{weekId}/days`.
   - Add exercise: `POST /api/training-plan-days/{dayId}/exercises`.
   - Edit exercise: `PUT /api/training-plan-exercises/{id}`; delete: `DELETE /api/training-plan-exercises/{id}`.
4. View the assembled plan: `GET /api/bookings/{bookingId}/training-plan`.

Suggested UI: an accordion of weeks → days → ordered exercise rows (use `orderIndex` for ordering; capture `sets`, `reps`, `intensity`, `restSeconds`, `notes`).

## Progress feedback
- Read learner check-ins: `GET /api/bookings/{bookingId}/progress-checkins`.
- Respond: `PUT /api/progress-checkins/{id}/coach-feedback` with `{ coachFeedback }`.

## Wallet & withdrawals
- Wallet: `GET /api/coaches/me/wallet` — show `availableBalance`, `pendingBalance`, `totalEarned`, `totalWithdrawn`.
- Ledger: `GET /api/coaches/me/wallet/transactions` (paged).
- Payout account: `PUT /api/coaches/me/payout-account` (status resets to `pending`; show verification state). Read with `GET`.
- Withdrawals: `POST /api/coaches/me/withdrawal-requests` (`{ amount }`); list with `GET`. Disable the request form unless a **verified** payout account exists and `amount <= availableBalance`. Surface `PAYOUT_ACCOUNT_REQUIRED` and `INSUFFICIENT_WALLET_BALANCE`.

## Chat & notifications
- Chat: `GET /api/chat/rooms`, `.../messages` (poll). Available only with an active/completed booking.
- Notifications: `GET /api/notifications/me`, unread-count, read, read-all.
