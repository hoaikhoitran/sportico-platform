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
  - `availabilitySlotId` must exist and be `available` → else `404` / `409 SCHEDULE_CONFLICT`.
  - Slot `CoachId` must match `booking.CoachId` → else `403 COMMON_FORBIDDEN`.
  - Slot `StartTime` must be in the future → else `409 SCHEDULE_CONFLICT`.
  - Slot `StartTime` must be ≤ `booking.ExpiresAt` → else `409 BOOKING_NOT_ACTIVE`.
  - No overlap with coach's or learner's `requested`/`scheduled` sessions → else `409 SCHEDULE_CONFLICT`.
  - Ownership: `403 BOOKING_NOT_OWNED`; missing: `404 BOOKING_NOT_FOUND`.
  - On success: slot status → `booked`; coach is notified.
  - On session cancellation: slot status reverts to `available`.

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
  "startTime": "2026-06-01T09:00:00Z",
  "endTime":   "2026-06-01T10:00:00Z",
  "location":  "Gym A",
  "meetingUrl": null,
  "isOnline":  false,
  "note":      "Bring your own equipment"
}
```
- **Rules**: `startTime` must be in the future; `endTime > startTime`; no overlapping slot for the same coach.
- **Response** (`Result<CoachAvailabilitySlotResponse>`): `status: "available"`.

### GET /api/coaches/me/availability-slots  — coach views own slots
- **Role**: `coach`.
- **Query** (`CoachAvailabilitySlotFilterRequest`): `status`, `startFrom`, `startTo`, `pageNumber`, `pageSize`.
- **Response** (`Result<PagedResult<CoachAvailabilitySlotResponse>>`): all slots for the current coach (any status).

### GET /api/coaches/{coachId}/availability-slots  — public view
- **Role**: any authenticated user.
- **Query** (`CoachAvailabilitySlotFilterRequest`): `startFrom`, `startTo`, `pageNumber`, `pageSize`.
- **Response** (`Result<PagedResult<CoachAvailabilitySlotResponse>>`): only `available` future slots.

### PUT /api/coaches/me/availability-slots/{id}/cancel
- **Role**: `coach` (must own the slot).
- **Effect**: `available → cancelled`. Cannot cancel a `booked` slot.
- **Response** (`Result<CoachAvailabilitySlotResponse>`).

---

## Slot status lifecycle
```
available ──(learner books)──► booked ──(session cancelled)──► available
available ──(coach cancels)──► cancelled
```

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
