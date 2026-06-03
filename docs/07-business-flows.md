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
PayOS → POST /api/payments/payos/webhook        (primary activation path)
  → signature verified
  → ActivatePaidBookingAsync(payment, booking, "webhook")
       Payment → paid, Booking → active (PaidAt, ExpiresAt set)
       Coach wallet ensured; notifications: coach + learner (once)

Fallback if the webhook never arrives:
Learner success page → POST /api/payments/payos/{orderCode}/reconcile
  → backend queries PayOS GET /v2/payment-requests/{orderCode}
  → if PAID → ActivatePaidBookingAsync(payment, booking, "reconcile")  (same idempotent path)
  → if PENDING/PROCESSING → no activation; client retries ("Đồng bộ lại thanh toán")
```

Preconditions for both: package must be `published`, and a learner cannot purchase their own package.

Cancellation/failure (PayOS webhook **or** reconcile reports `cancelled`/`failed`/`expired`): Payment → `cancelled`/`failed`, Booking → `cancelled`.

> **Why the coach's `POST /api/bookings/{id}/training-plan` returns 409:** a training plan can only be
> created for an `active` booking. If a learner paid but the booking is still `pending_payment`, the
> webhook did not run — the learner reconciles (above) to activate it. Do **not** relax the
> `active`-booking rule in `TrainingPlanService`. The coach UI should hide/disable the create-plan
> form unless the booking is `active`, show "Học viên chưa được hệ thống xác nhận thanh toán" for a
> `pending_payment` booking, and surface a friendly message on a `409 BOOKING_NOT_ACTIVE` response.

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
                          → NO payout is sent here (even if AutoPayoutEnabled = true)

Admin approves           PUT .../{id}/approve   ← the only step that can send money
  • Manual mode  (PayOs:AutoPayoutEnabled = false):
        → status: approved
        Admin marks paid   PUT .../{id}/mark-paid → status: paid
                           → moves Pending → TotalWithdrawn; ledger debit (withdrawal)
  • Auto mode    (PayOs:AutoPayoutEnabled = true):
        → status: processing; PayOS Chi payout initiated (idempotency key = withdrawal.Id)
        PayOS SUCCESS  → status: paid; Pending → TotalWithdrawn; ledger debit (withdrawal)
        PayOS PROCESSING → status: processing (funds stay in Pending; refresh-payout-status later)
        PayOS FAILED/CANCELLED/REJECTED → status: failed; Pending → Available (funds returned)

   or Admin rejects      PUT .../{id}/reject   → status: rejected; returns Pending → Available
```

Admin tools: `refresh-payout-status` reconciles a `processing` payout against PayOS; `retry-payout`
re-reserves funds and retries a `failed` payout with a NEW idempotency key.

> NOTE: In manual mode there is no automated bank transfer — "mark paid" records an external transfer.
> In auto mode the transfer is performed by PayOS at **admin approval** time. See [08 — Payment and Wallet](08-payment-and-wallet.md).

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
9. (Fallback) The success page calls POST /api/payments/payos/{orderCode}/reconcile.
   Backend verifies the real state with PayOS GET /v2/payment-requests/{orderCode}:
     - PAID              → activate booking (same idempotent path as the webhook)
     - CANCELLED/EXPIRED → Payment cancelled/failed, Booking cancelled
     - PENDING/PROCESSING→ no change; client retries
```

The webhook and reconcile both call `ActivatePaidBookingAsync` and are idempotent for the `paid`
case: activation side effects (notifications, wallet) run only on the first transition to `active`,
so the two paths can race or repeat without duplicating effects. Reconcile additionally enforces an
**ownership guard** — a learner may only reconcile their own payment.

## Coach Reviews & Moderation

```
Eligibility (backend-enforced, never trust frontend):
  learner may review a coach  ⇔  a Booking exists where
      LearnerId = learner, CoachId = coach,
      Status ∈ { active, completed }, PaidAt ≠ null
  pending_payment | cancelled | refunded  → not allowed
  one review per (coach, learner)          → uq_reviews_pair + duplicate check

Create (learner)  POST /api/coaches/{coachId}/reviews
  → review status: active; recalc CoachProfile.Rating + TotalReviews
Edit (learner)    PUT /api/reviews/{id}
  → allowed only while a NON-expired successful booking exists (Booking.ExpiresAt)
  → expired → 409 REVIEW_EDIT_EXPIRED (view stays allowed)
Delete own (learner) DELETE /api/reviews/{id}
  → soft delete (status=deleted); recalc stats

Public  GET /api/coaches/{coachId}/reviews            → active only, newest first
        GET /api/coaches/{coachId}/reviews/summary    → average + 1★–5★ breakdown

Moderation:
  Coach   POST /api/reviews/{id}/report               → only about own reviews
  Admin   GET  /api/admin/review-reports?status=...
          PUT  /api/admin/review-reports/{id}/resolve
            isValid=false              → report rejected, review stays active
            isValid=true + hide=true   → review hidden (auditable), recalc stats
```

The cached `CoachProfile.Rating` / `TotalReviews` are recomputed from **active** reviews after every
create, update, self-delete, and moderation hide. Coaches can never edit/delete learner reviews —
they can only report; admins moderate.
