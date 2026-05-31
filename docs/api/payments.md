# API — Payments / PayOS

Controllers: `PaymentsController` (`/api/payments`) for the webhook; PayOS purchase link creation lives on `BookingsController` (`/api/bookings/purchase/payos`, see [api/bookings.md](bookings.md)). Implementation in [PayOsService](../../src/SporticoApp.Infrastructure/Services/Payments/PayOsService.cs).

See [08 — Payment and Wallet](../08-payment-and-wallet.md#payos-payment-flow) for the full flow.

## Create payment (purchase a booking via PayOS)
`POST /api/bookings/purchase/payos` (role `learner`). Returns `checkoutUrl` + `orderCode`. The backend:
- creates a `Booking` (`pending_payment`) and a `Payment` (`payos`/`pending`) with a unique `orderCode`,
- calls PayOS `POST {BaseUrl}/v2/payment-requests` with headers `x-client-id`, `x-api-key`, and an HMAC-SHA256 `signature` over:
  `amount={amount}&cancelUrl={cancelUrl}&description={description}&orderCode={orderCode}&returnUrl={returnUrl}` keyed by `PayOs:ChecksumKey`.

## POST /api/payments/payos/webhook
- **Role**: Public (`AllowAnonymous`) — secured by signature verification, not auth.
- **Body** (`PayOsWebhookRequest`):
```json
{ "data": { "orderCode": 1716800000000, "status": "paid" }, "signature": "<hex>" }
```
`data` is the raw PayOS data object (JSON). `signature` is the gateway's HMAC.
- **Response**: `Result<object>` — `{ "status": "ok" }` for handled paid/cancelled/failed, `{ "status": "ignored" }` for other statuses.
- **Behaviour**:
  - Verifies the signature (fail-closed). Logs the raw payload as a `PaymentTransaction`.
  - Reads status from `data.status` (or `data.code == "00"` ⇒ `paid`):
    - `paid` → Payment `paid`, Booking `active` (idempotent — activation side effects only run if it was not already active).
    - `cancelled` → Payment `cancelled`, Booking `cancelled`.
    - `failed` → Payment `failed`, Booking `cancelled`.
    - anything else → ignored.
- **Errors**: `400 COMMON_VALIDATION_ERROR` (invalid signature, or missing `orderCode`); `404 PAYMENT_NOT_FOUND`; `404 BOOKING_NOT_FOUND`.

## POST /api/payments/payos/reconcile  *(and `POST /api/payments/payos/{orderCode}/reconcile`)*
Learner-initiated reconciliation after returning from the PayOS checkout. The webhook is the
primary activation path; reconcile is the **fallback** for when the webhook is delayed or never
arrives (so a learner who has paid is not stuck on a `pending_payment` booking).

- **Role**: `learner` (authenticated). The service verifies the payment belongs to the caller.
- **Body** (`ReconcilePayOsRequest`) — supply either field (the `{orderCode}` route variant fills `orderCode`):
```json
{ "orderCode": 1716800000000, "paymentId": null }
```
- **Behaviour**:
  1. Loads the payment by `orderCode` or `paymentId`; `404 PAYMENT_NOT_FOUND` if missing.
  2. Ownership guard: `403 COMMON_FORBIDDEN` if the payment is not the caller's.
  3. If already `paid` + booking `active` → returns idempotent success **without** calling PayOS.
  4. If still `pending` → calls PayOS `GET {BaseUrl}/v2/payment-requests/{orderCode}` and acts on the **real** state:
     - `PAID` → activates the booking (same idempotent path as the webhook).
     - `CANCELLED` / `EXPIRED` → Payment `cancelled`/`failed`, pending Booking `cancelled`.
     - `PENDING` / `PROCESSING` → no activation; returns the current status so the client can retry.
- **Never** trusts a frontend `status=PAID` / `code=00` query string — it only triggers this backend verification.
- **Response** (`Result<ReconcilePayOsResponse>`):
```json
{
  "isSuccess": true,
  "data": {
    "paymentId": "…", "orderCode": 1716800000000,
    "paymentStatus": "paid", "bookingId": "…", "bookingStatus": "active",
    "activated": true, "payOsStatus": "PAID",
    "message": "Payment confirmed by PayOS. Booking is now active."
  },
  "error": null
}
```
- **Errors**: `400 COMMON_VALIDATION_ERROR` (neither `orderCode` nor `paymentId`; or payment has no `orderCode`); `403 COMMON_FORBIDDEN`; `404 PAYMENT_NOT_FOUND` / `BOOKING_NOT_FOUND`; `409` when the payment is not a PayOS payment.

### Frontend integration (success / fail pages)
The repository is backend-only; the frontend should implement:
- **`payment/success`**: read `orderCode` (or `paymentId`) from the query string, show `Đang xác nhận thanh toán...`, then `POST /api/payments/payos/{orderCode}/reconcile`.
  - If `data.activated == true` (booking `active`) → redirect to the booking detail / learner dashboard.
  - If still pending → show `Thanh toán đang được xác nhận, vui lòng thử đồng bộ lại sau` and a `Đồng bộ lại thanh toán` button that re-calls reconcile.
  - Do **not** treat a `status=PAID` query string as final success.
- **`payment/fail`**: call reconcile once to settle the record; show the cancelled/failed state.

## Signature Verification
`VerifyWebhookSignature` recomputes HMAC-SHA256 over the **canonical** `data`:
- object keys sorted ascending (ordinal),
- `key=value` pairs joined by `&`,
- the `signature` field (if echoed inside `data`) excluded,
- compared in constant time against the provided signature (lower-cased hex).

It returns false (reject) when the signature is missing, the checksum key is missing, or `data` is not a JSON object.

## Payment record fields (reference)
`Payment`: `userId`, `amount`, `method` (`manual|payos`), `referenceType` (`booking|coach_package`), `referenceId`, `status` (`pending|paid|failed|cancelled`), `transactionCode`, `orderCode` (unique when set), `paymentLinkId`, `checkoutUrl`, `expiredAt`, `paidAt`.

> NOTE: The legacy `CoachPackagesController` also creates PayOS payments with `referenceType = coach_package`. That path is legacy ([17 — Legacy Modules](../17-legacy-modules.md)); the current flow uses `referenceType = booking`.
