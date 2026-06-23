# API — Bookings

Controller: `BookingsController` (`/api/bookings`). All endpoints require authentication; purchase and learner reads require role `learner`, coach reads require role `coach`.

Purpose: purchase a training package (manual or PayOS) and view bookings.

`Status`: `pending_payment | active | completed | cancelled | refunded`.

> **No manual session booking.** Purchasing reserves a seat on every package session slot and the
> system auto-creates one `scheduled` `TrainingSession` per slot. The learner does **not** call
> `POST /api/bookings/{bookingId}/sessions` for packages with a fixed schedule.

## POST /api/bookings/purchase/manual
- **Role**: `learner`.
- **Body**: `{ "trainingPackageId": "guid" }`.
- **Response** (`Result<BookingResponse>`): a booking with `status: "active"`, `paidAt` set.
- **Effects**: validates capacity and reserves one seat on every package session slot; snapshots
  commission; creates a `manual`/`paid` `Payment`; **auto-creates one `scheduled` `TrainingSession`
  per slot**; ensures the coach wallet; notifies both parties. The coach wallet is **not** credited here.
- **Errors**: `404 TRAINING_PACKAGE_NOT_FOUND`; `409 TRAINING_PACKAGE_NOT_PUBLISHED`;
  `409 TRAINING_PACKAGE_HAS_NO_SCHEDULE`; `409 TRAINING_PACKAGE_SESSION_SLOT_FULL`;
  `409 CONCURRENCY_CONFLICT` (lost the race for the last seat); `403 COMMON_FORBIDDEN` (buying your own
  package); `400 COMMON_VALIDATION_ERROR`.

## POST /api/bookings/purchase/payos
- **Role**: `learner`.
- **Body**: `{ "trainingPackageId": "guid" }`.
- **Response** (`Result<PurchaseTrainingPackagePayOsResponse>`):
```json
{
  "bookingId": "guid", "paymentId": "guid", "orderCode": 1716800000000,
  "checkoutUrl": "https://pay.payos.vn/...", "status": "pending", "expiredAt": "date"
}
```
- **Effects**: reserves a seat on every package session slot up-front (prevents overselling while
  pending), creates booking `pending_payment` and a `payos`/`pending` payment, then calls PayOS to
  create the link. On `paid` the booking is activated and the sessions are auto-created (idempotent);
  on `cancelled`/`failed`/`expired` the reserved seats are released.
- **Errors**: as manual, plus `PAYOS_CREATE_PAYMENT_FAILED` if PayOS config/call fails.

## GET /api/bookings/me
- **Role**: `learner`. Paged list of the learner's bookings.
- **Query** (`BookingFilterRequest`): `status`, `pageNumber`, `pageSize` (confirm exact fields in `BookingFilterRequest`).

## GET /api/bookings/{id}
- **Role**: `learner`. Get one of the learner's bookings.
- **Errors**: `404 BOOKING_NOT_FOUND`; `403 BOOKING_NOT_OWNED`.

## GET /api/bookings/coach
- **Role**: `coach`. Paged list of bookings for the coach's packages.

## GET /api/bookings/coach/{id}
- **Role**: `coach`. Get one booking the coach owns. `404 BOOKING_NOT_FOUND` / `403 BOOKING_NOT_OWNED`.

## BookingResponse shape
```json
{
  "id": "guid", "learnerId": "guid", "coachId": "guid",
  "trainingPackageId": "guid", "trainingPackageTitle": "...",
  "totalAmount": 1000000, "platformFeeRate": 0.15, "platformFeeAmount": 150000,
  "coachReceiveAmount": 850000, "perSessionCoachAmount": 106250,
  "totalSessions": 8, "completedSessions": 0,
  "status": "active",
  "paidAt": "date|null", "completedAt": "date|null", "cancelledAt": "date|null",
  "createdAt": "date", "updatedAt": "date"
}
```

## Business rules
- Package must be `published` and have a schedule; learners cannot purchase their own package.
- Commission fields are snapshotted at purchase (see [08 — Payment and Wallet](../08-payment-and-wallet.md)).
- Manual → immediately `active` (sessions auto-created); PayOS → `pending_payment` (seats reserved)
  until the webhook/reconcile reports `paid`, then `active` with sessions auto-created.
- Learners are **not** blocked by their own schedule overlap (an account may buy for a child).
- Per-session slot capacity is enforced with an optimistic-concurrency `Version` token — no overselling
  under concurrent purchases.
- The generated sessions are listed via `GET /api/bookings/{bookingId}/sessions` (see [training-sessions](training-sessions.md)).
- Booking becomes `completed` when all sessions complete. The coach is paid **per completed session**, not at purchase.
