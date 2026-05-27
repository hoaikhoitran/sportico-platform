# API — Training Sessions

Controller: `TrainingSessionsController`. Mixed routes (booking-scoped and session-scoped). All require authentication.

Purpose: request, confirm, cancel, and complete training sessions inside an active booking.

`Status`: `requested | scheduled | completed | cancelled | missed`.

## POST /api/bookings/{bookingId}/sessions  — request
- **Role**: `learner` (must own the booking).
- **Body** (`CreateTrainingSessionRequest`):
```json
{
  "startTime": "2026-06-01T09:00:00Z",
  "endTime":   "2026-06-01T10:00:00Z",
  "location": "Gym A",
  "meetingUrl": null,
  "learnerNote": "Morning preferred"
}
```
(`bookingId` comes from the route.)
- **Response** (`Result<TrainingSessionResponse>`): `status: "requested"`.
- **Rules / errors**:
  - Booking must be `active` → else `409 BOOKING_NOT_ACTIVE`.
  - `startTime` must be in the future → else `400 COMMON_VALIDATION_ERROR`.
  - `requested+scheduled+completed` count `< TotalSessions` → else `409 SESSION_LIMIT_EXCEEDED`.
  - No overlap with coach's or learner's `requested`/`scheduled` sessions → else `409 SCHEDULE_CONFLICT`.
  - Ownership: `403 BOOKING_NOT_OWNED`; missing: `404 BOOKING_NOT_FOUND`.
  - Notifies the coach.

## GET /api/bookings/{bookingId}/sessions
- **Role**: any authenticated participant (learner or coach on the booking).
- **Query** (`TrainingSessionFilterRequest`): `status`, `pageNumber`, `pageSize`.
- **Response** (`Result<PagedResult<TrainingSessionResponse>>`).
- **Errors**: `404 BOOKING_NOT_FOUND`; `403 BOOKING_NOT_OWNED` (not a participant).

## PUT /api/training-sessions/{id}/confirm  — coach confirms
- **Role**: `coach` (must own the session).
- **Body** (`ConfirmTrainingSessionRequest`): `{ "location": "...", "meetingUrl": "...", "coachNote": "..." }` (all optional).
- **Effect**: `requested → scheduled`, sets `confirmedAt`. Notifies the learner.
- **Errors**: `404 TRAINING_SESSION_NOT_FOUND`; `403 TRAINING_SESSION_NOT_OWNED`; `409 INVALID_TRAINING_SESSION_STATUS` (not `requested`).

## PUT /api/training-sessions/{id}/cancel
- **Role**: any participant (coach or learner on the session).
- **Body** (`CancelTrainingSessionRequest`): `{ "reason": "..." }` (optional; stored as coach or learner note depending on caller).
- **Effect**: `requested|scheduled → cancelled`, sets `cancelledAt`. Notifies the other party.
- **Errors**: `404 TRAINING_SESSION_NOT_FOUND`; `403 TRAINING_SESSION_NOT_OWNED`; `409 INVALID_TRAINING_SESSION_STATUS` (already completed/cancelled).

## PUT /api/training-sessions/{id}/complete  — coach completes
- **Role**: `coach` (must own the session).
- **Body**: none.
- **Effect**: `scheduled → completed`, sets `completedAt`. Increments `Booking.CompletedSessions`, credits the coach wallet `perSessionCoachAmount` (ledger `session_release` credit), and marks the booking `completed` when all sessions are done. Notifies the learner ("completed") and the coach ("wallet credited").
- **Errors**: `404 TRAINING_SESSION_NOT_FOUND`; `403 TRAINING_SESSION_NOT_OWNED`; `409 INVALID_TRAINING_SESSION_STATUS` (not `scheduled`); `404 BOOKING_NOT_FOUND`.

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
(Confirm exact response fields in `TrainingSessionResponse`.)
