# API — Personalized Training

Controllers: `LearnerAssessmentsController`, `TrainingPlansController`, `ProgressCheckInsController`. All require authentication; structure-editing is coach-only, reads are open to either booking participant.

See [09 — Personalized Training](../09-personalized-training.md) for the conceptual model.

## Assessment

### POST /api/bookings/{bookingId}/assessment  — learner
- **Body** (`CreateLearnerAssessmentRequest`):
```json
{
  "goalType": "muscle_gain", "goalDescription": "Add 5kg lean mass",
  "heightCm": 175, "weightKg": 70, "bodyFatPercent": 18,
  "currentLevel": "beginner", "healthNotes": null, "injuryNotes": "knee discomfort",
  "trainingHistory": "1 year casual", "availableDaysPerWeek": "3",
  "preferredSessionDurationMinutes": 60, "equipmentAvailable": "dumbbells, bench"
}
```
Only `goalType` is required.
- **Response** (`Result<LearnerAssessmentResponse>`).

### GET /api/bookings/{bookingId}/assessment  — participant
Returns the assessment. `404 LEARNER_ASSESSMENT_NOT_FOUND` if none.

### PUT /api/bookings/{bookingId}/assessment  — learner
Updates the assessment (`UpdateLearnerAssessmentRequest`).

## Training Plan

**Domain model**: `Booking` = purchased package · `TrainingSession` = one booked time slot · `TrainingPlan` = the coach-authored workout plan for a booking. There is **one TrainingPlan per Booking** (unique index on `BookingId`). A plan is structured as `TrainingPlan → Weeks → Days → Exercises`.

**Status**: `draft | active | completed | cancelled`. Allowed transitions:
- `draft → active`, `draft → cancelled`
- `active → completed`, `active → cancelled`
- `completed` and `cancelled` are **terminal** — no further status change or content edits.

Any other transition (e.g. `completed → active`, `done`, `finish`) is rejected with `409 INVALID_TRAINING_PLAN_STATUS` (transition) or `400 COMMON_VALIDATION_ERROR` (unknown value).

**Expiration & read-only**: when the underlying package expires (`Booking.ExpiresAt < now`) **or** the plan reaches a terminal status, the plan becomes **read-only**:
- `GET` is **always allowed** — learner and coach can keep viewing the plan after expiry.
- `POST`/`PUT`/`DELETE` (plan, weeks, days, exercises) are blocked with `409`:
  - Expired package → `"Training package has expired. Training plan is now read-only."`
  - Terminal status → `"Training plan is {status} and can no longer be modified"`.

`TrainingPlanResponse` exposes computed flags so the UI can disable edit controls:
- `bookingExpiresAt` — package expiry (nullable)
- `isReadOnly` — `true` when terminal or expired
- `readOnlyReason` — `"Training package expired"` / `"Training plan completed"` / `"Training plan cancelled"` / `null`

### POST /api/bookings/{bookingId}/training-plan  — coach
Creates the plan. Booking must be **active and not expired**, owned by the coach, and must not already have a plan.
- **Body** (`CreateTrainingPlanRequest`):
```json
{ "title": "Strength Block 1", "goalType": "muscle_gain", "overview": "...",
  "startDate": "2026-06-01T00:00:00Z", "endDate": "2026-07-27T00:00:00Z", "totalWeeks": 8 }
```
- **Response** (`Result<TrainingPlanResponse>`), status `draft`.
- **Errors**: `403 BOOKING_NOT_OWNED`; `404 BOOKING_NOT_FOUND`; `409 BOOKING_NOT_ACTIVE` (inactive or expired); `409` if a plan already exists.
- **Notification**: learner receives a `training_plan` notification ("Training plan created").

### GET /api/bookings/{bookingId}/training-plan  — participant
Returns the plan (nested weeks/days/exercises). **Allowed even after expiry / for terminal plans.** `404 TRAINING_PLAN_NOT_FOUND` if none.

### PUT /api/training-plans/{id}  — coach
Update plan header + status (`UpdateTrainingPlanRequest`). Blocked when read-only.
- **Errors**: `403 TRAINING_PLAN_NOT_OWNED`; `409 INVALID_TRAINING_PLAN_STATUS` (illegal transition); `409 BOOKING_NOT_ACTIVE` (expired); `400` for unknown status value.
- **Notification**: learner receives a `training_plan` notification ("Training plan updated").

### POST /api/training-plans/{id}/weeks  — coach
- **Body**: `{ "weekNumber": 1, "focus": "Lower body", "notes": "RPE 6-7" }`. Blocked when read-only. Bumps `TrainingPlan.UpdatedAt`.

### POST /api/training-plan-weeks/{weekId}/days  — coach
- **Body**: `{ "dayNumber": 1, "title": "Squat day", "notes": "Warm up 10 min" }`. Blocked when read-only.

### POST /api/training-plan-days/{dayId}/exercises  — coach
- **Body** (`CreateTrainingPlanExerciseRequest`):
```json
{ "exerciseName": "Barbell Back Squat", "orderIndex": 1, "sets": 4,
  "reps": "8", "intensity": "RPE 7", "restSeconds": 120, "notes": "Keep brace" }
```
Blocked when read-only.

### PUT /api/training-plan-exercises/{id}  — coach
Update an exercise (`UpdateTrainingPlanExerciseRequest`). Blocked when read-only.

### DELETE /api/training-plan-exercises/{id}  — coach
Remove an exercise. Returns `Result<object>`. Blocked when read-only.

> Nested mutations (week/day/exercise) do **not** emit per-change notifications — only plan creation and plan-header/status updates notify the learner, to avoid notification spam.

## Progress Check-ins

### POST /api/bookings/{bookingId}/progress-checkins  — learner
- **Body** (`CreateProgressCheckInRequest`):
```json
{ "checkInDate": "2026-06-08T00:00:00Z", "weightKg": 70.5, "bodyFatPercent": 17.5,
  "waistCm": 80, "energyLevel": "good", "sleepQuality": "ok", "learnerNote": "Felt strong" }
```
- **Response** (`Result<ProgressCheckInResponse>`).

### GET /api/bookings/{bookingId}/progress-checkins  — participant
Paged list (`ProgressCheckInFilterRequest`: `pageNumber`, `pageSize`).

### PUT /api/progress-checkins/{id}/coach-feedback  — coach
- **Body**: `{ "coachFeedback": "Great progress, add 2.5kg next week." }`.
- **Errors**: `404 PROGRESS_CHECKIN_NOT_FOUND`; `403` if not the owning coach.

## Common errors
- `404 BOOKING_NOT_FOUND` / `403 BOOKING_NOT_OWNED` when the booking is missing or the caller is not a participant.
- `400 COMMON_VALIDATION_ERROR` for invalid bodies.
- Coach-only endpoints return `403` for non-owning coaches and for non-coach roles.
