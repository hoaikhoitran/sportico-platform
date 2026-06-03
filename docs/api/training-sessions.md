# API — Training Sessions

Controller: `TrainingSessionsController`. Mixed routes (booking-scoped, session-scoped, and schedule-scoped). All require authentication.

**Domain clarification**: `Booking` = a purchased training package. `TrainingSession` = one actual booked time slot.

`Status`: `requested | scheduled | completed | cancelled | missed`.

---

## Coach availability slots (prerequisite)
Before a learner can book a session, the coach must publish availability slots.
See [Coach Availability Slots](#coach-availability-slots) section below.

---

## POST /api/bookings/{bookingId}/sessions  — learner requests a session
- **Role**: `learner` (must own the booking).
- **Body** (`CreateTrainingSessionRequest`):
```json
{
  "availabilitySlotId": "guid"
  "learnerNote": "Morning preferred"
}
```
(`bookingId` is taken from the route; `startTime`/`endTime` come from the selected slot.)
- **Response** (`Result<TrainingSessionResponse>`): `status: "requested"`.
- **Rules / errors**:
  - Booking must be `active` → else `409 BOOKING_NOT_ACTIVE`.
  - Booking must not be expired (`ExpiresAt` not in the past) → else `409 BOOKING_NOT_ACTIVE`.
  - `requested+scheduled+completed` count `< TotalSessions` → else `409 SESSION_LIMIT_EXCEEDED`.
  - `availabilitySlotId` must exist and not be cancelled/expired → else `404` / `409 SCHEDULE_CONFLICT`.
  - Slot `CoachId` must match `booking.CoachId` → else `403 COMMON_FORBIDDEN`.
  - Slot `StartTime` must be in the future → else `409 SCHEDULE_CONFLICT`.
  - Slot `StartTime` must be ≤ `booking.ExpiresAt` → else `409 BOOKING_NOT_ACTIVE`.
  - No overlap with coach's or learner's `requested`/`scheduled` sessions → else `409 SCHEDULE_CONFLICT`.
  - **Capacity (group slots):** the slot accepts up to `maxParticipants` active sessions. If it is already full → `409 SCHEDULE_CONFLICT "Availability slot is full"`.
  - Ownership: `403 BOOKING_NOT_OWNED`; missing: `404 BOOKING_NOT_FOUND`.
  - On success: the slot stays `available` while seats remain (`remainingParticipants > 0`) and flips to `booked` only when the **last** seat is taken. Coach is notified.
  - On session cancellation: a seat is released; if the slot is still in the future and not cancelled, it reverts to `available`.
  - **Concurrency:** the slot carries an optimistic-concurrency `version`; two learners booking the last seat at the same time will not both succeed — the loser gets `409 SCHEDULE_CONFLICT` and can retry.

## GET /api/bookings/{bookingId}/sessions
- **Role**: any authenticated participant (learner or coach on the booking).
- **Query** (`TrainingSessionFilterRequest`): `status`, `startFrom`, `startTo`, `pageNumber`, `pageSize`.
- **Response** (`Result<PagedResult<TrainingSessionResponse>>`).

## GET /api/learners/me/training-sessions  — learner schedule
- **Role**: `learner`.
- **Query** (`TrainingSessionFilterRequest`): `status`, `startFrom`, `startTo`, `pageNumber`, `pageSize`.
- **Response** (`Result<PagedResult<TrainingSessionResponse>>`): all sessions for the current learner across all bookings.

## GET /api/coaches/me/training-sessions  — coach schedule
- **Role**: `coach`.
- **Query** (`TrainingSessionFilterRequest`): `status`, `startFrom`, `startTo`, `pageNumber`, `pageSize`.
- **Response** (`Result<PagedResult<TrainingSessionResponse>>`): all sessions for the current coach across all bookings.

## PUT /api/training-sessions/{id}/confirm  — coach confirms
- **Role**: `coach` (must own the session).
- **Body** (`ConfirmTrainingSessionRequest`): `{ "location": "...", "meetingUrl": "...", "coachNote": "..." }` (all optional).
- **Effect**: `requested → scheduled`, sets `confirmedAt`. Notifies the learner.
- **Errors**: `404`, `403 TRAINING_SESSION_NOT_OWNED`, `409 INVALID_TRAINING_SESSION_STATUS` (not `requested`).

## PUT /api/training-sessions/{id}/cancel
- **Role**: any participant (coach or learner).
- **Body** (`CancelTrainingSessionRequest`): `{ "reason": "..." }` (optional).
- **Effect**: `requested|scheduled → cancelled`, sets `cancelledAt`. If the session was booked from an availability slot, that slot reverts to `available`. Notifies the other party.
- **Errors**: `404`, `403`, `409 INVALID_TRAINING_SESSION_STATUS`.

## PUT /api/training-sessions/{id}/complete  — coach completes
- **Role**: `coach` (must own the session).
- **Body**: none.
- **Effect**: `scheduled → completed`. Increments `Booking.CompletedSessions`. Credits coach wallet `perSessionCoachAmount` (ledger `session_release` credit). Marks the booking `completed` when all sessions finish. Notifies learner and coach.
- **Errors**: `404`, `403`, `409 INVALID_TRAINING_SESSION_STATUS` (not `scheduled`).

---

## Coach availability slots

Controller: `CoachAvailabilitySlotsController`.

### POST /api/coaches/me/availability-slots  — coach publishes a slot
- **Role**: `coach`.
- **Body** (`CreateCoachAvailabilitySlotRequest`):
```json
{
  "startTime": "2026-06-10T08:00:00Z",
  "endTime":   "2026-06-10T09:00:00Z",
  "isOnline":  true,
  "meetingUrl": "https://meet.example.com/abc",
  "location":  null,
  "note":      "Group badminton session",
  "maxParticipants": 5
}
```
- **Rules**: `startTime` must be in the future; `endTime > startTime`; no overlapping slot for the same coach; `maxParticipants` (optional) must be `1..50`. **Omitting `maxParticipants` defaults to 1** (a private slot — backward compatible).
- **Response** (`Result<CoachAvailabilitySlotResponse>`) — now includes capacity fields:
```json
{
  "status": "available",
  "maxParticipants": 5,
  "bookedParticipants": 0,
  "remainingParticipants": 5,
  "isFull": false
}
```

### GET /api/coaches/me/availability-slots  — coach views own slots
- **Role**: `coach`.
- **Query** (`CoachAvailabilitySlotFilterRequest`): `status`, `startFrom`, `startTo`, `pageNumber`, `pageSize`.
- **Response** (`Result<PagedResult<CoachAvailabilitySlotResponse>>`): all slots for the current coach (any status), each with `maxParticipants`/`bookedParticipants`/`remainingParticipants`/`isFull`.

### GET /api/coaches/{coachId}/availability-slots  — public view
- **Role**: any authenticated user.
- **Query** (`CoachAvailabilitySlotFilterRequest`): `startFrom`, `startTo`, `pageNumber`, `pageSize`.
- **Response** (`Result<PagedResult<CoachAvailabilitySlotResponse>>`): only **bookable** slots — `available` status, future `startTime`, `remainingParticipants > 0` (a slot is flipped to `booked` once full, so full slots are excluded). Capacity fields included.

### PUT /api/coaches/me/availability-slots/{id}/cancel
- **Role**: `coach` (must own the slot).
- **Effect**: `available → cancelled`. **Blocked if the slot has any active sessions** → `409 INVALID_TRAINING_SESSION_STATUS "Cannot cancel a slot that has active sessions"` (cancel/move those sessions first).
- **Response** (`Result<CoachAvailabilitySlotResponse>`).

---

## Slot status lifecycle (capacity-aware)
```
available ──(book a seat, seats remain)──► available
available ──(book the LAST seat)─────────► booked  (full)
booked    ──(a session is cancelled)─────► available  (if future & not cancelled)
available ──(coach cancels, no sessions)─► cancelled
```
`bookedParticipants` = active sessions (`requested|scheduled|completed`) on the slot;
`remainingParticipants` = `maxParticipants − bookedParticipants`; `isFull` = no seats left.

---

## Package expiration rules
- `Booking.ExpiresAt` is set when the booking becomes active: `PaidAt + TrainingPackage.DurationDays`.
- After `ExpiresAt`, the learner **cannot book new sessions** from this package.
- Existing chat and already-booked sessions are unaffected by expiration.
- If `ExpiresAt` is null (legacy data), the expiration check is skipped.

---

## TrainingSessionResponse shape
```json
{
  "id": "guid", "bookingId": "guid", "learnerId": "guid", "coachId": "guid",
  "startTime": "date", "endTime": "date", "status": "scheduled",
  "meetingUrl": "...|null", "location": "...|null",
  "learnerNote": "...|null", "coachNote": "...|null",
  "confirmedAt": "date|null", "completedAt": "date|null", "cancelledAt": "date|null",
  "createdAt": "date", "updatedAt": "date"
}
```
