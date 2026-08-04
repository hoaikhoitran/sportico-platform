# API — Bookings

Controller: `BookingsController` (`/api/bookings`). All endpoints require authentication; purchase and learner reads require role `learner`, coach reads require role `coach`.

Purpose: purchase a training package (manual or PayOS) and view bookings.

`Status`: `pending_payment | active | completed | cancelled | refunded`.

> **No manual session booking.** Purchasing reserves a seat on every package session slot and the
> system auto-creates one `scheduled` `TrainingSession` per slot. The learner does **not** call
> `POST /api/bookings/{bookingId}/sessions` for packages with a fixed schedule.

> **Vouchers**: both purchase endpoints accept an optional `voucherCode` — see
> [`docs/api/vouchers.md`](vouchers.md) for the full discount/eligibility rules, the 100%-off
> no-PayOS path, and the extra `originalAmount`/`discountAmount`/`voucherCode` response fields.

## POST /api/bookings/purchase/manual
- **Role**: `learner`.
- **Body**: `{ "trainingPackageId": "guid", "voucherCode": "WELCOME10" }` (`voucherCode` optional).
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
- **Body**: `{ "trainingPackageId": "guid", "voucherCode": "WELCOME10" }` (`voucherCode` optional).
- **Response** (`Result<PurchaseTrainingPackagePayOsResponse>`):
```json
{
  "bookingId": "guid", "paymentId": "guid", "orderCode": 1716800000000,
  "checkoutUrl": "https://pay.payos.vn/...", "status": "pending", "paymentStatus": "pending",
  "paymentRequired": true, "bookingStatus": "pending_payment", "expiredAt": "date"
}
```
  A voucher covering 100% of the price returns `orderCode: null`, `checkoutUrl: null`,
  `paymentRequired: false`, `bookingStatus: "active"` — no PayOS redirect needed.
- **Effects**: reserves a seat on every package session slot AND (if `voucherCode` given) a voucher
  use, up-front — commits booking+payment+reservations to the database **first**, then calls PayOS.
  On `paid` the booking is activated, sessions auto-created, and the voucher redemption becomes
  permanent (idempotent across webhook + reconcile). On `cancelled`/`failed`/`expired` the reserved
  seats AND the voucher use are released. If PayOS itself fails after the DB commit, the booking is
  immediately cancelled and everything released — never left dangling.
- **Errors**: as manual, plus `PAYOS_CREATE_PAYMENT_FAILED` if PayOS config/call fails, plus the
  voucher error codes in [`vouchers.md`](vouchers.md) if `voucherCode` is invalid/ineligible.

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
  "totalAmount": 900000, "originalAmount": 1000000, "discountAmount": 100000,
  "voucherCampaignId": "guid|null", "voucherCode": "WELCOME10|null",
  "platformFeeRate": 0.15, "platformFeeAmount": 150000,
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
