# 07 — Business Flows

End-to-end flows with status transitions. Status values are the string constants in `SporticoApp.Shared/Constants`.

## Coach Training Package Flow

```
Coach creates package        → status: pending
Admin approves               → status: published   (publicly listable & purchasable)
        or rejects           → status: rejected    (RejectionReason set)
Coach archives               → status: archived
```

Steps:
1. Coach: `POST /api/training-packages` → package created `pending`.
2. Admin: `GET /api/admin/training-packages/pending` to review.
3. Admin: `PUT /api/admin/training-packages/{id}/approve` → `published`, notification to coach.
   - Or `PUT /api/admin/training-packages/{id}/reject` with a reason → `rejected`, notification to coach.
4. Public/learners now see it via `GET /api/public/training-packages`.

## Booking Purchase Flow

Two paths produce a `Booking`. Both snapshot commission fields at purchase time (see [08 — Payment and Wallet](08-payment-and-wallet.md)).

### Manual (no gateway)
```
Learner POST /api/bookings/purchase/manual
  → Booking status: active   (PaidAt set)
  → Payment: manual / paid
  → Coach wallet ensured, chat room ensured
  → Notifications: coach ("new booking"), learner ("booking active")
```

### PayOS
```
Learner POST /api/bookings/purchase/payos
  → Booking status: pending_payment
  → Payment: payos / pending  (+ checkoutUrl, orderCode)
Learner pays at checkoutUrl
PayOS → POST /api/payments/payos/webhook
  → signature verified
  → Payment status: paid
  → Booking status: active (PaidAt set)
  → Coach wallet ensured, chat room ensured
  → Notifications: coach + learner
```

Preconditions for both: package must be `published`, and a learner cannot purchase their own package.

Cancellation/failure (PayOS webhook reports `cancelled`/`failed`): Payment → `cancelled`/`failed`, Booking → `cancelled`.

## Session Flow

```
Learner requests   → session status: requested   (booking must be active)
Coach confirms     → session status: scheduled    (ConfirmedAt set)
Coach completes    → session status: completed     (CompletedAt set)
                      → Booking.CompletedSessions += 1
                      → Coach wallet credited PerSessionCoachAmount
                      → if CompletedSessions >= TotalSessions: Booking → completed
Either party cancels (from requested/scheduled) → session status: cancelled
```

Rules enforced on request:
- Booking must be `active`.
- Count of sessions in `requested + scheduled + completed` must be `< Booking.TotalSessions` (else `SESSION_LIMIT_EXCEEDED`).
- `StartTime` must be in the future.
- No time overlap with the coach's or learner's other `requested`/`scheduled` sessions (else `SCHEDULE_CONFLICT`).

Only the coach who owns the session may confirm/complete; completion requires the session to be `scheduled`.

## Personalized Training Flow

```
Learner fills assessment   POST /api/bookings/{id}/assessment
Coach builds plan          POST /api/bookings/{id}/training-plan       (draft)
  → add weeks              POST /api/training-plans/{id}/weeks
  → add days               POST /api/training-plan-weeks/{weekId}/days
  → add exercises          POST /api/training-plan-days/{dayId}/exercises
Learner logs progress      POST /api/bookings/{id}/progress-checkins
Coach gives feedback       PUT  /api/progress-checkins/{id}/coach-feedback
```

See [09 — Personalized Training](09-personalized-training.md). All structure-editing endpoints require the coach who owns the booking; read endpoints are open to either participant.

## Wallet and Withdrawal Flow

```
Session completed       → wallet.AvailableBalance += PerSessionCoachAmount
                          + ledger credit (session_release)
Coach sets payout acct   PUT /api/coaches/me/payout-account   → status: pending
Admin verifies           PUT /api/admin/coach-payout-accounts/{id}/verify → verified
Coach requests withdraw  POST /api/coaches/me/withdrawal-requests
                          → requires verified account + sufficient AvailableBalance
                          → moves amount Available → Pending; request status: pending
Admin approves           PUT .../{id}/approve  → status: approved
Admin marks paid         PUT .../{id}/mark-paid → status: paid
                          → moves Pending → TotalWithdrawn; ledger debit (withdrawal)
   or Admin rejects      PUT .../{id}/reject   → status: rejected
                          → returns Pending → Available
```

> NOTE: There is no real automated bank transfer. "Mark paid" is an administrative action recording that the payout was made externally. See [08 — Payment and Wallet](08-payment-and-wallet.md).

## PayOS Flow (detailed)

```
1. Learner requests PayOS purchase.
2. Backend creates Booking (pending_payment) + Payment (pending) with a unique orderCode.
3. Backend calls PayOS /v2/payment-requests (HMAC-SHA256 signed) → checkoutUrl.
4. Backend returns { bookingId, paymentId, orderCode, checkoutUrl, expiredAt }.
5. Frontend redirects the learner to checkoutUrl.
6. PayOS calls POST /api/payments/payos/webhook with { data, signature }.
7. Backend verifies the signature (fail-closed), logs a PaymentTransaction,
   and reads the status:
     - paid      → Payment paid, Booking active (idempotent), activation side effects
     - cancelled → Payment cancelled, Booking cancelled
     - failed    → Payment failed, Booking cancelled
     - other     → ignored (no state change)
8. Learner is returned to PayOs ReturnUrl / CancelUrl (frontend pages).
```

The webhook is idempotent for the `paid` case: it only re-runs activation side effects (notifications, wallet, chat) when the booking was not already `active`.
