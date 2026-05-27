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

### POST /api/bookings/{bookingId}/training-plan  — coach
- **Body** (`CreateTrainingPlanRequest`):
```json
{ "title": "Strength Block 1", "goalType": "muscle_gain", "overview": "...",
  "startDate": "2026-06-01T00:00:00Z", "endDate": "2026-07-27T00:00:00Z", "totalWeeks": 8 }
```
- **Response** (`Result<TrainingPlanResponse>`), status `draft`.

### GET /api/bookings/{bookingId}/training-plan  — participant
Returns the plan (with nested weeks/days/exercises per `TrainingPlanResponse`). `404 TRAINING_PLAN_NOT_FOUND`.

### PUT /api/training-plans/{id}  — coach
Update plan header (`UpdateTrainingPlanRequest`). `403 TRAINING_PLAN_NOT_OWNED`.

### POST /api/training-plans/{id}/weeks  — coach
- **Body**: `{ "weekNumber": 1, "focus": "Lower body", "notes": "RPE 6-7" }`.

### POST /api/training-plan-weeks/{weekId}/days  — coach
- **Body**: `{ "dayNumber": 1, "title": "Squat day", "notes": "Warm up 10 min" }`.

### POST /api/training-plan-days/{dayId}/exercises  — coach
- **Body** (`CreateTrainingPlanExerciseRequest`):
```json
{ "exerciseName": "Barbell Back Squat", "orderIndex": 1, "sets": 4,
  "reps": "8", "intensity": "RPE 7", "restSeconds": 120, "notes": "Keep brace" }
```

### PUT /api/training-plan-exercises/{id}  — coach
Update an exercise (`UpdateTrainingPlanExerciseRequest`).

### DELETE /api/training-plan-exercises/{id}  — coach
Remove an exercise. Returns `Result<object>`.

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
