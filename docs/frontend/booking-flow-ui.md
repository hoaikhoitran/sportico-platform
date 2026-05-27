# Frontend — Booking Flow UI

The learner-facing purchase-to-sessions journey.

## 1. Public listing → detail

- `/training-packages` → `GET /api/public/training-packages` (paged; filter by `keyword`, `sportId`).
  - Card shows title, sport, price, sessionCount, durationDays, online/location, level, goalType, coach.
- `/training-packages/[id]` → `GET /api/public/training-packages/{id}`.
  - Show full description and a **Buy** CTA (requires login as a learner).

## 2. Purchase

Offer the two purchase methods:

### Manual
```
POST /api/bookings/purchase/manual   { "trainingPackageId": id }
```
Booking returns `active` immediately. Route the user to the booking detail.

### PayOS
```
POST /api/bookings/purchase/payos    { "trainingPackageId": id }
→ { bookingId, orderCode, checkoutUrl, expiredAt, status: "pending" }
```
1. Redirect the browser to `checkoutUrl`.
2. PayOS returns the user to `/payment/success` or `/payment/cancel` (configured server-side).
3. On `/payment/success`, **poll** `GET /api/bookings/{bookingId}` until `status === "active"` (the webhook activates it server-side; there may be a short delay). Show a "confirming payment…" state until then.

Disable the Buy button for a learner viewing their own package (the server returns `403 COMMON_FORBIDDEN`).

## 3. Booking detail → sessions

Once a booking is `active`, the detail page surfaces:

- **Commission summary** (read-only): totalAmount, platformFeeAmount, coachReceiveAmount, perSessionCoachAmount, totalSessions, completedSessions.
- **Session list**: `GET /api/bookings/{bookingId}/sessions`.
- **Request a session** (learner):
  ```
  POST /api/bookings/{bookingId}/sessions
  { "startTime": "...", "endTime": "...", "location": "...", "learnerNote": "..." }
  ```
  Validate client-side that `startTime` is in the future and `endTime > startTime`. Surface server errors:
  - `409 SESSION_LIMIT_EXCEEDED` — show "all sessions scheduled".
  - `409 SCHEDULE_CONFLICT` — prompt for a different slot.
  - `409 BOOKING_NOT_ACTIVE` — booking not payable/active.

## 4. Session lifecycle (status badges)

```
requested → (coach confirms) → scheduled → (coach completes) → completed
        ↘ (either cancels) → cancelled
```

- Learner can cancel a `requested`/`scheduled` session.
- Coach actions (confirm/complete) appear on the coach's view of the same booking.
- After a completion, refresh the booking (to update `completedSessions`) and, on the coach side, the wallet.

## Chat entry point

Show a "Message coach/learner" button once a booking is active — it opens the chat room (`GET /api/chat/rooms`, then `.../messages`). See [learner-dashboard-ui.md](learner-dashboard-ui.md) and [coach-dashboard-ui.md](coach-dashboard-ui.md).
