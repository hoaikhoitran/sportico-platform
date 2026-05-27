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
