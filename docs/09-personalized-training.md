# 09 — Personalized Training

## Why Personalization Exists

Sportico is not a generic "buy sessions and get a note" product. Each booking carries a structured, goal-driven coaching program tailored to one learner. Personalization is **per-booking, per-learner, goal-based, and process-oriented**:

- The learner's situation is captured up front (goals, body metrics, injuries, equipment, availability).
- The coach authors a multi-week plan broken down to individual exercises with sets/reps/intensity.
- The learner logs measurable progress over time.
- The coach reviews progress and replies with feedback, closing the loop.

This is what distinguishes the booking from a flat purchase: the artifacts below are tied to a specific `bookingId`.

## LearnerAssessment

Captured once per booking (1:1). Fields (all optional except `GoalType`):

| Field | Type | Notes |
|---|---|---|
| `GoalType` | string (required) | e.g. fat loss, muscle gain, endurance |
| `GoalDescription` | string? | Free-text goal detail |
| `HeightCm`, `WeightKg`, `BodyFatPercent` | decimal? | Baseline body metrics |
| `CurrentLevel` | string? | Beginner / intermediate / advanced |
| `HealthNotes` | string? | Health conditions |
| `InjuryNotes` | string? | Injuries / limitations (e.g. knee discomfort) |
| `TrainingHistory` | string? | Prior experience |
| `AvailableDaysPerWeek` | string? | Availability |
| `PreferredSessionDurationMinutes` | int? | Preferred session length |
| `EquipmentAvailable` | string? | Home/gym equipment |

Endpoints: learner `POST`/`PUT /api/bookings/{bookingId}/assessment`; either participant `GET`.

## TrainingPlan Structure

A four-level hierarchy authored by the coach.

```
TrainingPlan         (1 per booking)
└── TrainingPlanWeek (many)        WeekNumber, Focus, Notes
    └── TrainingPlanDay (many)     DayNumber, Title, Notes
        └── TrainingPlanExercise (many)  ExerciseName, OrderIndex, Sets, Reps, Intensity, RestSeconds, Notes
```

### TrainingPlan
`Title`, `GoalType`, `Overview?`, `StartDate`, `EndDate`, `TotalWeeks`, `Status` (`draft | active | completed | cancelled`).

### TrainingPlanWeek
`WeekNumber`, `Focus?` (e.g. "Hypertrophy — lower body"), `Notes?`.

### TrainingPlanDay
`DayNumber`, `Title` (e.g. "Leg day"), `Notes?`.

### TrainingPlanExercise
`ExerciseName`, `OrderIndex` (ordering within the day), `Sets?`, `Reps?` (string — supports ranges like "8-12"), `Intensity?` (e.g. "RPE 7" or "%1RM"), `RestSeconds?`, `Notes?`.

Endpoints (coach-only writes; participant reads): see [05 — API Overview](05-api-overview.md#personalized-training) and [api/personalized-training.md](api/personalized-training.md).

## ProgressCheckIn

Periodic learner-submitted entry, optionally answered by the coach.

| Field | Type |
|---|---|
| `CheckInDate` | DateTime |
| `WeightKg`, `BodyFatPercent`, `WaistCm` | decimal? |
| `EnergyLevel`, `SleepQuality` | string? |
| `LearnerNote` | string? |
| `CoachFeedback` | string? (set by coach) |

Endpoints: learner `POST /api/bookings/{bookingId}/progress-checkins`; participant `GET`; coach `PUT /api/progress-checkins/{id}/coach-feedback`.

## Coach Feedback Loop

```
Learner submits check-in (weight, energy, note)
   → notification path (training/wallet events generate notifications;
      check-in feedback is recorded on the entry)
Coach reviews and writes CoachFeedback
   → learner sees feedback on the check-in
Coach adjusts the plan (edit exercises) for the next cycle
```

## Two-Learner Illustration

The same coach, same package, two different programs driven by the assessment:

**Learner A** — goal *fat loss*, injury note *knee discomfort*:
- Plan focus: conditioning + joint-friendly strength.
- Example exercise: **Goblet Box Squat**, 3 sets, reps 10-12, intensity **RPE 5**, notes "limit knee flexion depth".

**Learner B** — goal *muscle gain*, no injuries:
- Plan focus: progressive overload, hypertrophy.
- Example exercise: **Barbell Back Squat**, **4 sets x 8 reps**, intensity **RPE 7**.

Both records live under different `bookingId`s with their own assessment, plan tree, and check-ins. Nothing about the plan is shared or templated across learners by the system — it is authored per booking.
