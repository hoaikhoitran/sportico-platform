# API — Coach Reviews & Moderation

Controllers: `ReviewsController`, `ReviewReportsController`.
Service/Repo: [ReviewService](../../src/SporticoApp.Application/Services/ReviewService.cs),
[ReviewRepository](../../src/SporticoApp.Infrastructure/Persistence/Repositories/ReviewRepository.cs),
[ReviewReportService](../../src/SporticoApp.Application/Services/ReviewReportService.cs).

Coach reviews work like Google Maps reviews: 1–5 stars + optional comment, a public list per coach,
summary stats (average + breakdown), and a learner can leave **one** review per coach.

---

## Who can review (enforced in the backend — never trust frontend flags)

A learner may create a review for a coach only when they have a **successful booking** with that coach:

- `Booking.LearnerId == learner` and `Booking.CoachId == coach`
- `Booking.Status` is `active` **or** `completed`
- `Booking.PaidAt != null`

A learner **cannot** review when the booking is `pending_payment`, `cancelled`, or `refunded`, or
when they never purchased from the coach. One review per `(coach, learner)` — enforced by the unique
index `uq_reviews_pair` and a service-side duplicate check.

**Edit window:** a learner may edit their review only while they still have a **non-expired**
successful booking (`Booking.ExpiresAt == null || ExpiresAt >= now`). After expiry the review is
read-only — editing returns `409 REVIEW_EDIT_EXPIRED` (*"Package has expired. Review can no longer be edited."*).

---

## Public endpoints

### GET /api/coaches/{coachId}/reviews
Public list of **active** reviews. Optional auth — when called with a learner token, the learner's own
review carries `canEdit: true` if still editable.

Query (`ReviewFilterRequest`): `?pageNumber=1&pageSize=10&rating=5&sortBy=latest|highest|lowest`
(`pageSize` ≤ 50; default sort `latest`). Returns `Result<PagedResult<ReviewResponse>>`.

### GET /api/coaches/{coachId}/reviews/summary
`Result<CoachReviewSummaryResponse>` — `averageRating`, `totalReviews`, and `ratingBreakdown`
(1★–5★ counts). Computed from **active** reviews only.

---

## Learner endpoints (role `learner`)

### GET /api/coaches/{coachId}/reviews/me
The caller's own review for the coach (`404 REVIEW_NOT_FOUND` if none / deleted).

### POST /api/coaches/{coachId}/reviews
Body (`CreateReviewRequest`): `{ "bookingId": "…optional…", "rating": 5, "comment": "…" }`
(the route `coachId` is authoritative). Recalculates the coach's cached rating on success.

| Code | When |
|------|------|
| `400 COMMON_VALIDATION_ERROR` | rating not 1–5, comment > 1000 chars |
| `403 REVIEW_NOT_ALLOWED` | no successful paid booking, or reviewing yourself |
| `404 COACH_PROFILE_NOT_FOUND` | coach does not exist |
| `409 REVIEW_ALREADY_EXISTS` | already reviewed → call `PUT /api/reviews/{id}` instead |

> A previously **self-deleted** review is revived (same row) on re-create, keeping the unique constraint intact.

### PUT /api/reviews/{id}
Body (`UpdateReviewRequest`): `{ "rating": 4, "comment": "…" }`. Owner only, active review only,
non-expired booking required. `403 REVIEW_NOT_OWNED`, `409 REVIEW_EDIT_EXPIRED`, `409 REVIEW_NOT_ALLOWED`
(not active). Recalculates the coach rating.

### DELETE /api/reviews/{id}
Owner-only **soft delete** (`status = deleted`, recorded with `deletedAt`/`deletedByUserId`).
Recalculates the coach rating. Coaches cannot edit or delete learner reviews — they can only report.

---

## Report & moderation

Reviews are never hard-deleted; moderation sets `status = hidden` so the action stays auditable.
The shared `Report` entity now carries `targetType` (`user|review`), `targetId`, `description`,
`handledByUserId`, `handledAt`, `resolutionNote`, and `actionTaken` (`none|review_hidden|review_deleted`).

### POST /api/reviews/{id}/report  (role `coach`)
Only the **reviewed** coach may report (a coach cannot report another coach's review →
`403 REVIEW_REPORT_NOT_ALLOWED`). Body (`CreateReviewReportRequest`): `{ "reason": "…", "description": "…" }`.
One open report per coach per review.

### GET /api/admin/review-reports?status=pending|reviewing|resolved|rejected  (role `admin`)
Paged moderation queue (`Result<PagedResult<ReviewReportResponse>>`); each item includes a snapshot of
the reported review. Unknown status → `400`.

### PUT /api/admin/review-reports/{id}/resolve  (role `admin`)
Body (`ResolveReviewReportRequest`): `{ "isValid": true, "hideOrDeleteReview": true, "resolutionNote": "…" }`.
- `isValid: false` → report `rejected`, review stays `active`.
- `isValid: true` + `hideOrDeleteReview: true` → review `hidden` (with `moderationReason`), report `resolved`,
  `actionTaken = review_hidden`, **coach rating recalculated**.
- `isValid: true` + `hideOrDeleteReview: false` → report `resolved`, review kept.
Re-resolving an already handled report → `409`.

---

## Rating cache recalculation

`CoachProfile.Rating` and `CoachProfile.TotalReviews` are caches recomputed from **active** reviews
after every create, update, self-delete, and moderation hide. Hidden/deleted reviews are excluded
from both the average and the count.
