# 08 — Payment and Wallet

## Commission Model

The platform takes a **fixed 15% commission**. The rate is defined in `BookingService` as:

```csharp
private const decimal PlatformFeeRate = 0.15m;
```

When a booking is created (manual or PayOS), the commission math is **snapshotted onto the booking** so that later edits to the training package price do not affect a booking that has already been purchased.

### Booking snapshot fields

| Field | Meaning | Formula |
|---|---|---|
| `TotalAmount` | Price charged to the learner | `TrainingPackage.Price` |
| `PlatformFeeRate` | Commission rate captured at purchase | `0.15` |
| `PlatformFeeAmount` | Platform's cut | `TotalAmount * PlatformFeeRate` |
| `CoachReceiveAmount` | Coach's total earnable amount | `TotalAmount - PlatformFeeAmount` |
| `TotalSessions` | Sessions in the package | `TrainingPackage.SessionCount` |
| `PerSessionCoachAmount` | Coach earning per completed session | `CoachReceiveAmount / TotalSessions` (0 if no sessions) |
| `CompletedSessions` | Running count, starts at 0 | incremented on each completion |

### Worked example

Package price = 1,000,000, 8 sessions:

```
TotalAmount          = 1,000,000
PlatformFeeRate      = 0.15
PlatformFeeAmount    = 150,000
CoachReceiveAmount   = 850,000
TotalSessions        = 8
PerSessionCoachAmount= 106,250    (850,000 / 8)
```

The coach earns **106,250 per completed session**, and the full 850,000 only after all 8 are completed.

> NOTE: `PerSessionCoachAmount` is stored at `numeric(12,2)`. When `CoachReceiveAmount` does not divide evenly by `TotalSessions`, rounding can leave a small residual versus `CoachReceiveAmount`. There is no explicit reconciliation of the rounding remainder in the reviewed code.

## Coaches Are Paid Progressively

The coach is **not** paid the whole amount at purchase. Money is released **per completed session**:

- On `PUT /api/training-sessions/{id}/complete` (coach only, session must be `scheduled`):
  - `Booking.CompletedSessions += 1`.
  - `CoachWallet.AvailableBalance += PerSessionCoachAmount`.
  - `CoachWallet.TotalEarned += PerSessionCoachAmount`.
  - A ledger entry is written: `type = session_release`, `direction = credit`, `referenceType = training_session`.
  - If all sessions are complete, the booking is marked `completed`.

> NOTE: Released funds go directly to `AvailableBalance` (not held in `PendingBalance`). `PendingBalance` is used only to hold amounts that are tied up in an open withdrawal request.

## Wallet Balances

`CoachWallet` tracks four decimals:

| Field | Meaning |
|---|---|
| `AvailableBalance` | Withdrawable now |
| `PendingBalance` | Reserved against open withdrawal requests |
| `TotalEarned` | Lifetime credited from completed sessions |
| `TotalWithdrawn` | Lifetime paid out |

A wallet is created when a booking activates, or lazily on the first session completion if it does not exist.

## Wallet Transaction Ledger

`CoachWalletTransaction` is an append-only ledger:

| Type | Direction | Written when |
|---|---|---|
| `session_release` | `credit` | A session is completed |
| `withdrawal` | `debit` | A withdrawal request is marked paid |
| `adjustment` | (either) | Reserved for manual corrections (no automated writer in reviewed code) |

Query via `GET /api/coaches/me/wallet/transactions` (paged).

## Withdrawal Flow

Two modes, selected by `PayOs:AutoPayoutEnabled`. **Creation never sends money in either mode** — it
only reserves funds. The PayOS payout (auto mode) is triggered by **admin approval**.

State machine for `WithdrawalRequest.Status`:
- Manual mode (`AutoPayoutEnabled=false`): `pending → approved → paid`, or `pending → rejected`.
- Auto mode (`AutoPayoutEnabled=true`): `pending → processing → paid`, or `processing → failed → (retry) → processing`, or `pending → rejected`.

| Action | Endpoint | Wallet effect |
|---|---|---|
| Create | `POST /api/coaches/me/withdrawal-requests` | `Available -= amount`, `Pending += amount`; status `pending`; **no PayOS call** |
| Approve (manual) | `PUT /api/admin/withdrawal-requests/{id}/approve` | none (status → `approved`) |
| Approve (auto) | `PUT /api/admin/withdrawal-requests/{id}/approve` | status → `processing`; PayOS payout initiated (key = `withdrawal.Id`). SUCCESS → paid (`Pending -= amount`, `TotalWithdrawn += amount`, ledger debit); PROCESSING → stays `processing`; FAILED/CANCELLED/REJECTED → `failed` (`Pending -= amount`, `Available += amount`) |
| Mark paid (manual) | `PUT /api/admin/withdrawal-requests/{id}/mark-paid` | `Pending -= amount`, `TotalWithdrawn += amount`, ledger debit. Blocked while `processing`. |
| Refresh payout status | `PUT /api/admin/withdrawal-requests/{id}/refresh-payout-status` | Reconciles a `processing` payout: finalizes `paid` or rolls back to `failed`/`Available` |
| Retry payout | `POST /api/admin/withdrawal-requests/{id}/retry-payout` | `failed` only → re-reserve `Available -= amount`/`Pending += amount`, new idempotency key, status `processing` |
| Reject | `PUT /api/admin/withdrawal-requests/{id}/reject` | `Pending -= amount`, `Available += amount`. Blocked while `processing`/`paid`. |

Create preconditions:
- Caller has a coach profile.
- A **verified** payout account exists (`PayoutAccountStatuses.Verified`), else `PAYOUT_ACCOUNT_REQUIRED`.
- `amount <= AvailableBalance`, else `INSUFFICIENT_WALLET_BALANCE`.

## Payout Account Verification

- Coach upserts bank details via `PUT /api/coaches/me/payout-account`. An upsert sets status to `pending`.
- Admin lists pending accounts (`GET /api/admin/coach-payout-accounts/pending`) and either verifies or rejects them.
- Only a `verified` account allows withdrawals.

## PayOS Payment Flow

Implemented in [PayOsService](../src/SporticoApp.Infrastructure/Services/Payments/PayOsService.cs) and orchestrated by `BookingService`.

### Create payment link
- Endpoint: PayOS `POST {BaseUrl}/v2/payment-requests`.
- Headers: `x-client-id`, `x-api-key`.
- The request is signed with **HMAC-SHA256** over a canonical string:
  `amount=...&cancelUrl=...&description=...&orderCode=...&returnUrl=...` keyed by `PayOs:ChecksumKey`.
- On success (`code == "00"`) the service returns `paymentLinkId`, `checkoutUrl`, and the computed `expiredAt`.
- Missing PayOS configuration throws `PAYOS_CREATE_PAYMENT_FAILED` listing the missing keys.

### Webhook verification
- Endpoint: `POST /api/payments/payos/webhook` (`AllowAnonymous`).
- The body is `{ data, signature }`. Verification (`VerifyWebhookSignature`) is **fail-closed**: it rejects when the signature is missing, the checksum key is missing, or `data` is not a JSON object.
- The expected signature is HMAC-SHA256 over the canonical form of `data` (keys sorted ascending, `key=value` joined by `&`, the `signature` field excluded), compared in constant time.
- Status is read from `data.status` (or `code == "00"` ⇒ `paid`). The handler maps `paid`/`cancelled`/`failed` and ignores anything else.
- On `paid`, the webhook calls the shared **`ActivatePaidBookingAsync(payment, booking, source)`** method.

### Activation is shared and idempotent
Both the webhook and the reconcile endpoint route through `ActivatePaidBookingAsync`, so activation
side effects happen **exactly once** no matter how many times (or from which path) activation is
triggered:
- Payment → `paid` (`PaidAt` stamped if not already set).
- Booking → `active`, `PaidAt`, `ExpiresAt = PaidAt + TrainingPackage.DurationDays`.
- Coach wallet is ensured to exist.
- "New booking" / "booking active" notifications are sent **only on the first transition** to active.

If the booking is already `active`, the method is a no-op for side effects — re-running webhook +
reconcile cannot double-notify, double-create the wallet, or re-stamp timestamps.

### Reconcile (webhook fallback)
- Endpoint: `POST /api/payments/payos/reconcile` (also `POST /api/payments/payos/{orderCode}/reconcile`), role `learner`.
- Root cause it fixes: if the PayOS webhook never reaches the backend, the booking stays
  `pending_payment` even though the learner paid — which then makes the coach's
  `POST /api/bookings/{id}/training-plan` return `409 BOOKING_NOT_ACTIVE`. The 409 is correct;
  the bug is the un-activated booking. Reconcile lets the learner's success page actively settle it.
- The endpoint verifies the **real** state with PayOS (`GET /v2/payment-requests/{orderCode}`) and only
  activates when PayOS confirms `PAID`. A frontend `status=PAID` / `code=00` query string is never
  trusted as final — it only triggers this backend verification.
- See [api/payments.md](api/payments.md) for the request/response contract and the frontend success/fail flow.

### Disbursement modes
- **Manual (`PayOs:AutoPayoutEnabled=false`, default):** no automated transfer. The admin performs the bank
  transfer through an external channel and records it via "mark paid". PayOS is used only for **inbound** learner payments.
- **Auto (`PayOs:AutoPayoutEnabled=true`):** admin **approval** initiates a real PayOS Chi (payout) transfer to the
  coach's verified bank account, then the withdrawal moves through `processing → paid/failed` based on the PayOS result.

In both modes the money is only ever sent **after admin approval** — never at coach request time.
