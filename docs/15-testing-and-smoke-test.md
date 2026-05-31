# 15 — Testing and Smoke Test

This is the end-to-end manual smoke test covering the current business flow. Run it against a fresh database (migrations applied) using Swagger or any HTTP client. Each authenticated step needs `Authorization: Bearer <accessToken>` for the appropriate user.

> NOTE: There is no automated test project in the repository. This is a manual verification sequence. Sample bodies show the minimum required fields; `camelCase` is the wire format.

## Preconditions

- `roles` table has `learner`, `coach`, `admin`.
- At least one active `sports` row (admin creates one: `POST /api/sports`).
- Three users: a coach, a learner, and an admin (admin role granted via DB). All `active`.

## Ordered Steps

### 1. Login as coach
```
POST /api/auth/login
{ "email": "coach@example.com", "password": "Passw0rd!" }
```
Use `data.accessToken` as the coach token.

### 2. Coach creates a TrainingPackage
```
POST /api/training-packages          (coach)
{
  "sportId": 1,
  "title": "8-Week Strength Program",
  "description": "Progressive strength training",
  "price": 1000000,
  "sessionCount": 8,
  "durationDays": 56,
  "isOnline": true,
  "level": "beginner",
  "goalType": "muscle_gain"
}
```
Expect `status: "pending"`.

### 3. Admin approves the package
```
PUT /api/admin/training-packages/{packageId}/approve   (admin)
```
Expect `status: "published"`. (Reject path: `PUT .../reject` with `{ "reason": "..." }`.)

### 4. Learner purchases (manual)
```
POST /api/bookings/purchase/manual   (learner)
{ "trainingPackageId": "{packageId}" }
```
Expect a booking with `status: "active"`, `paidAt` set.

### 5. Check commission fields on the booking
From step 4's response (or `GET /api/bookings/{id}`), verify for price 1,000,000 / 8 sessions:
```
totalAmount = 1000000
platformFeeRate = 0.15
platformFeeAmount = 150000
coachReceiveAmount = 850000
totalSessions = 8
perSessionCoachAmount = 106250
completedSessions = 0
```

### 6. Learner requests a session
```
POST /api/bookings/{bookingId}/sessions   (learner)
{
  "startTime": "2026-06-01T09:00:00Z",
  "endTime":   "2026-06-01T10:00:00Z",
  "location": "Gym A",
  "learnerNote": "Morning preferred"
}
```
Expect `status: "requested"`. `startTime` must be in the future.

### 7. Coach confirms the session
```
PUT /api/training-sessions/{sessionId}/confirm   (coach)
{ "location": "Gym A", "meetingUrl": null, "coachNote": "Confirmed" }
```
Expect `status: "scheduled"`.

### 8. Coach completes the session
```
PUT /api/training-sessions/{sessionId}/complete   (coach)
```
Expect `status: "completed"`.

### 9. Check wallet credit
```
GET /api/coaches/me/wallet   (coach)
```
Expect `availableBalance = 106250`, `totalEarned = 106250`. Confirm a ledger entry:
```
GET /api/coaches/me/wallet/transactions   (coach)
→ one credit, type "session_release", amount 106250
```

### 10. Learner creates an assessment
```
POST /api/bookings/{bookingId}/assessment   (learner)
{ "goalType": "muscle_gain", "heightCm": 175, "weightKg": 70, "currentLevel": "beginner",
  "injuryNotes": "none", "availableDaysPerWeek": "3", "equipmentAvailable": "dumbbells" }
```

### 11. Coach creates plan / week / day / exercise
```
POST /api/bookings/{bookingId}/training-plan   (coach)
{ "title": "Strength Block 1", "goalType": "muscle_gain",
  "startDate": "2026-06-01T00:00:00Z", "endDate": "2026-07-27T00:00:00Z", "totalWeeks": 8 }

POST /api/training-plans/{planId}/weeks
{ "weekNumber": 1, "focus": "Lower body", "notes": "RPE 6-7" }

POST /api/training-plan-weeks/{weekId}/days
{ "dayNumber": 1, "title": "Squat day", "notes": "Warm up 10 min" }

POST /api/training-plan-days/{dayId}/exercises
{ "exerciseName": "Barbell Back Squat", "orderIndex": 1, "sets": 4, "reps": "8",
  "intensity": "RPE 7", "restSeconds": 120 }
```

### 12. Learner creates a progress check-in
```
POST /api/bookings/{bookingId}/progress-checkins   (learner)
{ "checkInDate": "2026-06-08T00:00:00Z", "weightKg": 70.5, "energyLevel": "good",
  "sleepQuality": "ok", "learnerNote": "Felt strong" }
```

### 13. Coach gives feedback
```
PUT /api/progress-checkins/{checkInId}/coach-feedback   (coach)
{ "coachFeedback": "Great progress, add 2.5kg next week." }
```

### 14. Chat test
```
GET  /api/chat/rooms                              (coach or learner)
POST /api/chat/rooms/{roomId}/messages
{ "content": "Hi, ready for your first session?" }
GET  /api/chat/rooms/{roomId}/messages
```
A room exists because the booking is active. Both participants can read/send.

### 15. Withdrawal test
```
PUT  /api/coaches/me/payout-account   (coach)
{ "payoutMethod": "bank", "bankName": "Bank X", "bankAccountNumber": "0123456789",
  "bankAccountHolder": "Coach Name" }
→ status "pending"

PUT  /api/admin/coach-payout-accounts/{accountId}/verify   (admin)
→ status "verified"

POST /api/coaches/me/withdrawal-requests   (coach)
{ "amount": 106250 }
→ status "pending"; wallet Available → Pending

GET  /api/admin/withdrawal-requests                  (admin)  → all withdrawals
GET  /api/admin/withdrawal-requests?status=pending   (admin)  → filter to pending
GET  /api/admin/withdrawal-requests?status=paid       (admin)  → filter to paid (after mark-paid below)
GET  /api/admin/withdrawal-requests?status=bogus     (admin)  → 400 COMMON_VALIDATION_ERROR
GET  /api/admin/withdrawal-requests/{id}             (admin)  → single detail (review modal)
GET  /api/coaches/me/withdrawal-requests/{id}        (coach)  → own detail (403 for another coach's id)

PUT  /api/admin/withdrawal-requests/{id}/approve     (admin)  → "approved"
PUT  /api/admin/withdrawal-requests/{id}/mark-paid   (admin)  → "paid"; ledger debit, totalWithdrawn += 106250
```
- Filtering by every status (`pending|approved|processing|paid|rejected|failed|cancelled`) returns only that status; unknown status → `400`.
- Rejecting (instead of mark-paid) returns Pending → Available; a `processing` withdrawal cannot be rejected or marked paid manually.

### 16. Notification test
```
GET /api/notifications/me               (coach)  → booking/session/wallet notifications
GET /api/notifications/me/unread-count
PUT /api/notifications/{id}/read
PUT /api/notifications/me/read-all
```

### 17. PayOS webhook signature test
Create a PayOS purchase to get an `orderCode`:
```
POST /api/bookings/purchase/payos   (learner)
{ "trainingPackageId": "{packageId}" }
→ { bookingId, paymentId, orderCode, checkoutUrl, ... }; booking status "pending_payment"
```
Then simulate the webhook. The signature must be a valid HMAC-SHA256 of the canonical `data` (keys sorted ascending, `key=value` joined by `&`, excluding `signature`) using `PayOs:ChecksumKey`:
```
POST /api/payments/payos/webhook   (anonymous)
{
  "data": { "orderCode": <orderCode>, "status": "paid", ... },
  "signature": "<hex hmac-sha256 of canonical data>"
}
```
- Valid signature + `status: "paid"` → booking becomes `active`.
- Invalid/missing signature → `400` (`COMMON_VALIDATION_ERROR`, "Invalid webhook signature"). The verifier is fail-closed.

### 18. PayOS reconcile test (webhook fallback)
Simulates the success page settling a payment when the webhook never ran.
```
POST /api/bookings/purchase/payos   (learner)
→ { bookingId, paymentId, orderCode, ... }; booking "pending_payment"

# Without the webhook firing, the coach's create-plan is correctly blocked:
POST /api/bookings/{bookingId}/training-plan   (coach)  → 409 BOOKING_NOT_ACTIVE

# Learner success page reconciles (backend re-checks PayOS, never trusts ?status=PAID):
POST /api/payments/payos/{orderCode}/reconcile   (learner)
→ if PayOS reports PAID: { activated: true, bookingStatus: "active", paymentStatus: "paid" }
→ if still pending:       { activated: false, bookingStatus: "pending_payment" }  (retry later)

# After activation, the coach can create the plan:
POST /api/bookings/{bookingId}/training-plan   (coach)  → 200
```
- Reconcile is idempotent: calling it again after the webhook already activated returns
  `activated: true` and does **not** duplicate notifications/wallet (no PayOS call is made).
- Ownership: reconciling another learner's `orderCode` → `403 COMMON_FORBIDDEN`.

### 19. Coach review + moderation test
```
# Learner has an active/completed PAID booking with the coach (steps 3-4 / 17-18).
POST /api/coaches/{coachId}/reviews        (learner)  { "rating": 5, "comment": "Great coach" } → 200
  → CoachProfile.Rating / TotalReviews recalculated
POST /api/coaches/{coachId}/reviews        (learner, again)            → 409 REVIEW_ALREADY_EXISTS
GET  /api/coaches/{coachId}/reviews                  (public)         → shows the active review
GET  /api/coaches/{coachId}/reviews/summary          (public)         → averageRating + 1★–5★ breakdown
PUT  /api/reviews/{id}                      (learner) { "rating": 4 } → 200 (booking not expired)

# Ineligible cases:
POST /api/coaches/{coachId}/reviews        (learner w/ pending_payment booking) → 403 REVIEW_NOT_ALLOWED
POST /api/coaches/{otherCoachId}/reviews   (learner who never bought)           → 403 REVIEW_NOT_ALLOWED

# Expiry: set the booking ExpiresAt in the past, then:
PUT  /api/reviews/{id}                      (learner)                 → 409 REVIEW_EDIT_EXPIRED

# Moderation:
POST /api/reviews/{id}/report               (coach, own review)      → 200 (report pending)
POST /api/reviews/{id}/report               (other coach)            → 403 REVIEW_REPORT_NOT_ALLOWED
GET  /api/admin/review-reports?status=pending  (admin)               → lists the report + review snapshot
PUT  /api/admin/review-reports/{reportId}/resolve (admin)
      { "isValid": true, "hideOrDeleteReview": true }                → review hidden, stats recalculated
GET  /api/coaches/{coachId}/reviews                  (public)         → hidden review no longer listed
```

## Pass Criteria

- Commission fields exactly match the formula (step 5).
- Wallet credited exactly `perSessionCoachAmount` per completion (step 9).
- After all 8 completions the booking becomes `completed`.
- Chat blocked without an active/completed booking; allowed with one.
- Withdrawal moves money Available → Pending → TotalWithdrawn correctly.
- Admin withdrawal list returns every status and filters correctly; unknown status → `400`.
- Reconcile activates a paid-but-unactivated booking, is idempotent with the webhook, and enforces ownership.
- Coach create-plan stays `409 BOOKING_NOT_ACTIVE` while `pending_payment`, and succeeds once `active` (the rule is never relaxed).
- Reviews: only learners with a successful paid booking can review; one per coach; edit blocked after expiry; public list/summary count active reviews only; coach can report; admin hide recalculates `CoachProfile.Rating`/`TotalReviews`.
