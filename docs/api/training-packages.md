# API — Training Packages

Controllers: `TrainingPackagesController` (coach), `AdminTrainingPackagesController` (admin), `PublicTrainingPackagesController` (public).
Purpose: create and moderate coach offerings; expose the public catalog.

`Status`: `pending | published | rejected | archived`.

## Coach — `/api/training-packages` (role `coach`)

### POST /api/training-packages
Create a package **with its full fixed schedule** (status `pending`). The model is start/end-date based;
`sessions` must contain exactly `sessionCount` items.
- **Body** (`CreateTrainingPackageRequest`):
```json
{
  "sportId": 1,
  "title": "8-Week Strength Program",
  "description": "Progressive strength training",
  "price": 1000000,
  "sessionCount": 2,
  "startDate": "2026-07-01T00:00:00Z",
  "endDate": "2026-08-31T00:00:00Z",
  "location": "Gym A",
  "isOnline": false,
  "level": "beginner",
  "goalType": "muscle_gain",
  "sessions": [
    {
      "sessionNumber": 1,
      "startTime": "2026-07-02T09:00:00Z",
      "endTime": "2026-07-02T10:00:00Z",
      "level": "beginner",
      "maxParticipants": 4,
      "location": "Gym A",
      "isOnline": false,
      "meetingUrl": null,
      "note": "Intro session"
    },
    {
      "sessionNumber": 2,
      "startTime": "2026-07-09T09:00:00Z",
      "endTime": "2026-07-09T10:00:00Z",
      "level": "beginner",
      "maxParticipants": 4,
      "location": "Gym A",
      "isOnline": false
    }
  ]
}
```
- **Validation** (`400 COMMON_VALIDATION_ERROR` otherwise): `sessions.Count == sessionCount`;
  `sessionNumber` unique covering `1..sessionCount`; each session within `[startDate, endDate]`;
  `startTime < endTime`; `maxParticipants > 0`; offline sessions require a `location`; no two sessions
  may overlap.
- `durationDays` is **no longer an input** — it is derived from `startDate`..`endDate`.
- **Response** (`Result<TrainingPackageResponse>`).

### GET /api/training-packages/me
List the coach's own packages (paged).
- **Query** (`TrainingPackageFilterRequest`): `keyword`, `sportId`, `status`, `pageNumber` (default 1), `pageSize` (default 10).
- **Response** (`Result<PagedResult<TrainingPackageResponse>>`).

### GET /api/training-packages/me/{id}
Get one of the coach's own packages. `404 TRAINING_PACKAGE_NOT_FOUND` / `403 TRAINING_PACKAGE_NOT_OWNED`.

### PUT /api/training-packages/{id}
Update a package (`UpdateTrainingPackageRequest`; same fields as create, including the full `sessions`
schedule, which **replaces** the existing schedule). Allowed only while the package is not published.
- **Errors**: `404 TRAINING_PACKAGE_NOT_FOUND`; `403 TRAINING_PACKAGE_NOT_OWNED`; `409 INVALID_TRAINING_PACKAGE_STATUS` if the package is `published`.

### PUT /api/training-packages/{id}/archive
Set status to `archived`.

## Admin — `/api/admin/training-packages` (role `admin`)

### GET /api/admin/training-packages/pending
List packages awaiting review (paged). Same filter shape.

### PUT /api/admin/training-packages/{id}/approve
Approve → `published`. Sets `ReviewedByUserId`/`ReviewedAt`. Notifies the coach.

### PUT /api/admin/training-packages/{id}/reject
Reject → `rejected` with a reason.
- **Body**: `{ "reason": "Insufficient detail" }`.
- Notifies the coach.

## Public — `/api/public/training-packages` (anonymous)

### GET /api/public/training-packages
List published packages (paged). Filter: `keyword`, `sportId`, `coachId`, `pageNumber`, `pageSize`.

### GET /api/public/training-packages/{id}
Get a package by id. `404 TRAINING_PACKAGE_NOT_FOUND`.

## TrainingPackageResponse shape
```json
{
  "id": "guid", "coachId": "guid", "sportId": 1, "sportName": "Gym",
  "title": "...", "description": "...", "price": 1000000,
  "sessionCount": 2, "durationDays": 62,
  "startDate": "2026-07-01T00:00:00Z", "endDate": "2026-08-31T00:00:00Z",
  "location": "Gym A", "isOnline": false,
  "level": "beginner", "goalType": "muscle_gain",
  "status": "published", "rejectionReason": null,
  "reviewedByUserId": "guid|null", "reviewedAt": "date|null",
  "createdAt": "date", "updatedAt": "date",
  "sessions": [
    {
      "id": "guid", "sessionNumber": 1,
      "startTime": "2026-07-02T09:00:00Z", "endTime": "2026-07-02T10:00:00Z",
      "level": "beginner", "location": "Gym A", "isOnline": false,
      "meetingUrl": null, "note": "Intro session",
      "maxParticipants": 4, "bookedParticipants": 0, "remainingParticipants": 4,
      "status": "open"
    }
  ]
}
```
`PublicTrainingPackageResponse` adds the same `sessions` array (with live `bookedParticipants` /
`remainingParticipants`) plus the embedded `coach` summary. Session slot `status`: `open | full | cancelled`.

## Business rules
- Created `pending`; only `published` packages are publicly listable and purchasable.
- Coaches see only their own packages on the `/me` routes (ownership enforced).
- Only admins approve/reject.
- The coach defines the entire schedule at creation; purchasing a package auto-creates the training
  sessions (see [bookings](bookings.md) and [training-sessions](training-sessions.md)).
