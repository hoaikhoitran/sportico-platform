# API — Training Packages

Controllers: `TrainingPackagesController` (coach), `AdminTrainingPackagesController` (admin), `PublicTrainingPackagesController` (public).
Purpose: create and moderate coach offerings; expose the public catalog.

`Status`: `pending | published | rejected | archived`.

## Coach — `/api/training-packages` (role `coach`)

### POST /api/training-packages
Create a package (status `pending`).
- **Body** (`CreateTrainingPackageRequest`):
```json
{
  "sportId": 1,
  "title": "8-Week Strength Program",
  "description": "Progressive strength training",
  "price": 1000000,
  "sessionCount": 8,
  "durationDays": 56,
  "location": "Gym A",
  "isOnline": true,
  "level": "beginner",
  "goalType": "muscle_gain"
}
```
- **Response** (`Result<TrainingPackageResponse>`).

### GET /api/training-packages/me
List the coach's own packages (paged).
- **Query** (`TrainingPackageFilterRequest`): `keyword`, `sportId`, `status`, `pageNumber` (default 1), `pageSize` (default 10).
- **Response** (`Result<PagedResult<TrainingPackageResponse>>`).

### GET /api/training-packages/me/{id}
Get one of the coach's own packages. `404 TRAINING_PACKAGE_NOT_FOUND` / `403 TRAINING_PACKAGE_NOT_OWNED`.

### PUT /api/training-packages/{id}
Update a package (`UpdateTrainingPackageRequest`; same fields as create).
- **Errors**: `404 TRAINING_PACKAGE_NOT_FOUND`; `403 TRAINING_PACKAGE_NOT_OWNED`; possibly `409 INVALID_TRAINING_PACKAGE_STATUS` depending on current status.

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
  "sessionCount": 8, "durationDays": 56, "location": "Gym A", "isOnline": true,
  "level": "beginner", "goalType": "muscle_gain",
  "status": "published", "rejectionReason": null,
  "reviewedByUserId": "guid|null", "reviewedAt": "date|null",
  "createdAt": "date", "updatedAt": "date"
}
```

## Business rules
- Created `pending`; only `published` packages are publicly listable and purchasable.
- Coaches see only their own packages on the `/me` routes (ownership enforced).
- Only admins approve/reject.
