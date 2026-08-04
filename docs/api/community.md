# API — Community

New module, fully independent of the legacy `Post`/`CoachPackage`/`VPublishedPost` tables. Entities: `CommunityPost`, `CommunityPostMedia`, `CommunityComment`, `CommunityPostReaction`, `CommunityPostApplication` (tables `community_posts`, `community_post_media`, `community_comments`, `community_post_reactions`, `community_post_applications`). Reports reuse the existing shared `reports` table (`Report.TargetType` = `community_post` / `community_comment`).

Controllers: `CommunityPostsController` (`/api/community/posts`), `CommunityCommentsController` (`/api/community`), `CommunityApplicationsController` (`/api/community/applications`), `ReportsController` (`/api/reports`), `AdminCommunityController` (`/api/admin/community`, role `admin`).

## Post types
`looking_for_players`, `looking_for_team`, `training_partner`, `friendly_match` (all "recruitment" — require `sportId`+`startAt`+`maxParticipants`, and support applications) · `event`, `discussion`, `question` (no recruitment fields required).

## Post lifecycle
`draft → published → closed (full, or manually closed) → expired (past its time)`, or `hidden`/`deleted` by an admin at any point. The public feed only ever returns `published` / `closed` / `expired`.

---

## POST /api/community/posts  (authenticated, active user)
```json
{
  "postType": "looking_for_players", "title": "Cần 3 người đá sân 7",
  "content": "Chủ nhật 17h, sân ABC, trình độ trung bình", "sportId": 1,
  "startAt": "2026-08-10T10:00:00Z", "maxParticipants": 8, "level": "intermediate",
  "feePerPerson": 50000, "locationName": "Sân ABC", "publish": true,
  "media": [ { "mediaType": "image", "url": "https://cdn.example.com/field.jpg" } ]
}
```
`maxParticipants` **includes the author** — `acceptedParticipants` starts at 1 for recruitment posts. Media: max 8 items, max 1 video, each an absolute `https://` URL.

## GET /api/community/posts  (public — works signed-out, enriched when signed in)
Filters: `postType`, `sportId`, `keyword`, `city`, `fromDate`/`toDate`, `level`, `hasAvailableSlots`, `authorId`, `followingOnly`, `sortBy` (`latest` \| `upcoming` \| `most_discussed`), `pageNumber`, `pageSize`.

## GET /api/community/posts/{id}
Response includes viewer-relative fields: `currentUserReaction`, `currentUserApplicationStatus`, `canApply`, `canEdit`, `slotsRemaining`. A `hidden` or `draft` post 404s (`COMMUNITY_POST_NOT_FOUND`) for everyone except its author.

## GET /api/community/posts/me · PUT /{id} · PUT /{id}/close · DELETE /{id}
Author-only. `DELETE` is a soft delete (`status: deleted`, record kept). Editable only while `draft`/`published`; `maxParticipants` can never be lowered below `acceptedParticipants`.

## PUT/DELETE /api/community/posts/{id}/like
Idempotent like/unlike; `reactionCount` never goes negative.

## Comments — `/api/community/posts/{postId}/comments`, `/api/community/comments/{commentId}`
- `GET`/`POST` comments on a post; `POST /api/community/comments/{commentId}/replies` — **one level of nesting only**: replying to a reply is rejected with `409 COMMUNITY_COMMENT_NESTING_NOT_ALLOWED` (not silently reparented).
- `PUT`/`DELETE` — owner only. Delete is a soft delete; the UI should render deleted content as "Bình luận đã bị xóa" while keeping any replies intact.

## Applications (recruitment posts) — `/api/community/posts/{postId}/applications`, `/api/community/applications/{id}`
```json
// POST .../applications
{ "message": "Mình chơi được, rảnh cuối tuần!" }
```
- Cannot apply to your own post, a non-`published` post, an already-started activity, or a full post.
- `DELETE .../applications/me` — cancel your own application (works while `pending` or `accepted`; cancelling an accepted spot frees the seat and re-opens a `closed` post if it hasn't started yet).
- `GET .../applications` — owner-only list.
- `PUT /api/community/applications/{id}/accept` \| `/reject` — owner-only. Accepting is guarded by the post's optimistic-concurrency `version` token, so two accepts racing for the last seat cannot both succeed; the post auto-`closed`s the moment it's full.

## Reporting — `POST /api/reports`
```json
{ "targetType": "community_post", "targetId": "...", "reason": "spam", "description": "..." }
```
`targetType`: `community_post` \| `community_comment` \| `chat_message`. Reuses the same `reports` table reviews already use — no second report system. A duplicate open report from the same reporter for the same target is idempotent (returns the existing one).

---

## Admin moderation — `/api/admin/community` (role `admin`)

| Endpoint | Effect |
|---|---|
| `GET /posts` | Filters: `status`, `postType`, `sportId`, `authorId`, `keyword`, `reportedOnly`, dates, `sortBy`, paging. Sees every status, incl. `hidden`/`deleted`. |
| `GET /posts/{id}` | Full detail, `canModerate: true`. |
| `PUT /posts/{id}/hide` | Body `{ "reason": "..." }`. Post disappears from the public feed/detail; data kept. Notifies the author. |
| `PUT /posts/{id}/restore` | Back to `published` (or `draft` if it was never published). |
| `DELETE /posts/{id}` | Soft delete. |
| `GET /posts/{id}/comments` | All comments regardless of status. |
| `PUT /comments/{id}/hide` \| `/restore` \| `DELETE /comments/{id}` | Same semantics as post moderation. |
| `GET /reports` | Filters: `targetType`, `status`, paging. |
| `PUT /reports/{id}/resolve` | Body `{ "status": "resolved", "actionTaken": "post_hidden", "resolutionNote": "..." }`. `actionTaken` (`post_hidden`/`post_deleted`/`comment_hidden`/`comment_deleted`/`none`) is applied automatically through the exact same hide/delete paths above. |

---

## Known limitations (honest, not swept under the rug)
- **Media storage**: no dedicated upload/storage-provider abstraction exists in this repo (checked: none of `CoachProfileMedia`/`Post` had one either). `CommunityPostMedia.Url` accepts a client-supplied `https://` URL as-is (validated for scheme only) — same trust model the repo already uses elsewhere for image URLs. Wiring a real object-storage provider (S3/Cloudinary/etc.) was out of scope for this change.
- **Search**: keyword search uses `ILIKE`, not a `pg_trgm` GIN index — correct results, but no index-accelerated fuzzy search yet at scale.
- **Chat-message reports**: `POST /api/reports` accepts `targetType: "chat_message"` but there is no message-lookup validation (no `GetById` on messages yet) — the `targetId` is trusted as given.
