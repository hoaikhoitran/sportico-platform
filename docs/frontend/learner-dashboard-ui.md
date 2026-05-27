# Frontend — Learner Dashboard UI

Role: `learner` (default after registration).

## Bookings
- List: `GET /api/bookings/me` (paged; filter by `status`).
- Detail: `GET /api/bookings/{id}` — show package, coach, commission snapshot (informational), and session progress.
- Status badges: `pending_payment`, `active`, `completed`, `cancelled`, `refunded`.

## Assessment
On a booking (do this early so the coach can build the plan):
- Create: `POST /api/bookings/{bookingId}/assessment` (only `goalType` required; include body metrics, injuries, equipment, availability).
- View: `GET /api/bookings/{bookingId}/assessment`.
- Update: `PUT /api/bookings/{bookingId}/assessment`.

## Session request
- Request: `POST /api/bookings/{bookingId}/sessions` (`startTime`, `endTime`, optional `location`, `meetingUrl`, `learnerNote`).
- List: `GET /api/bookings/{bookingId}/sessions`.
- Cancel a `requested`/`scheduled` session: `PUT /api/training-sessions/{id}/cancel` with `{ reason }`.
- Errors to surface: `SESSION_LIMIT_EXCEEDED`, `SCHEDULE_CONFLICT`, `BOOKING_NOT_ACTIVE`, future-`startTime` validation.

## Plan view
- `GET /api/bookings/{bookingId}/training-plan` — render the coach-authored plan read-only: weeks → days → exercises (ordered by `orderIndex`), showing sets/reps/intensity/rest/notes. `404 TRAINING_PLAN_NOT_FOUND` until the coach creates it.

## Progress check-ins
- Submit: `POST /api/bookings/{bookingId}/progress-checkins` (`checkInDate`, optional metrics + `learnerNote`).
- History: `GET /api/bookings/{bookingId}/progress-checkins` (paged) — show coach feedback alongside each entry when present.

## Chat
- `GET /api/chat/rooms` → open a room → `GET /api/chat/rooms/{roomId}/messages` (poll), `POST` to send.
- Available only with an active/completed booking with that coach (`CHAT_NOT_ALLOWED` otherwise).

## Notifications
- `GET /api/notifications/me`, `GET /api/notifications/me/unread-count` (badge), `PUT /api/notifications/{id}/read`, `PUT /api/notifications/me/read-all`.

## Payments
- For PayOS purchases, handle the `/payment/success` and `/payment/cancel` return pages and poll the booking until `active` (see [booking-flow-ui.md](booking-flow-ui.md)).
