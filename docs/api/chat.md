# API — Chat

Controllers: `ChatController` (`/api/chat`), `UserBlocksController` (`/api/users/{userId}/block`). All endpoints require authentication.

> **Updated**: chat is no longer coach-only. Any two **active** users can open a room. Existing
> coach↔learner chat behavior (booking-based rooms, message send/list) is unchanged and still works.

## Business rules
- Chat is **independent from package purchase**. Any authenticated, active user may open a chat room with any other active user.
- A chat room is created on demand (when the user clicks "Chat") — never automatically.
- There is exactly **one ChatRoom per user pair**. Calling `POST /api/chat/rooms` multiple times returns the same room.
- **Spam-reduction gate**: a new room starts `pending`. The requester can keep sending messages while pending; the recipient must `accept` before they can reply. A room starts `active` immediately (skipping `pending`) when the two users already have an active/completed `Booking` between them — this preserves the pre-existing coach↔learner UX.
- A `rejected` room is read-only — no new messages from either side.
- Blocking a user prevents new chat requests and new messages in both directions; existing message history is never deleted.
- Messages may carry `content`, `attachments`, or both (never neither).

---

## POST /api/chat/rooms
Opens or retrieves the chat room between the current user and a target user.

**Request body** (`CreateChatRoomRequest`):
```json
{ "targetUserId": "b2b1...", "sourceType": "community_post", "sourceId": "a1c4..." }
```
- `targetUserId` — required (or the legacy `coachId` field, still accepted for backward compatibility).
- `sourceType` / `sourceId` — optional context (e.g. opened from a community post).

**Response** (`Result<ChatRoomResponse>`):
```json
{
  "isSuccess": true,
  "data": {
    "id": "...", "user1Id": "...", "user2Id": "...", "otherUserId": "b2b1...",
    "status": "pending", "requestedByUserId": "...", "requestedAt": "...",
    "acceptedAt": null, "rejectedAt": null, "lastMessageAt": null,
    "sourceType": "community_post", "sourceId": "a1c4...", "createdAt": "..."
  }
}
```

**Errors**:
| Code | Meaning |
|------|---------|
| `400 COMMON_VALIDATION_ERROR` | `targetUserId`/`coachId` missing |
| `403 CHAT_CANNOT_MESSAGE_SELF` | Caller and target are the same user |
| `404 CHAT_TARGET_USER_NOT_FOUND` | No such user |
| `409 CHAT_TARGET_USER_INACTIVE` | Target user is not `active` |
| `403 CHAT_USER_BLOCKED` | Either user has blocked the other |

**Idempotent**: calling it multiple times with the same target always returns the same room.

---

## PUT /api/chat/rooms/{roomId}/accept
Recipient accepts a pending chat request → room becomes `active`. `403 CHAT_NOT_ALLOWED` if the caller is the requester (not the recipient); `409 CHAT_ROOM_NOT_PENDING` if the room isn't pending.

## PUT /api/chat/rooms/{roomId}/reject
Recipient rejects a pending chat request → room becomes `rejected` (read-only).

## GET /api/chat/rooms
Returns all chat rooms the current user participates in (ordered by most recent activity), each including its `status`.

## GET /api/chat/rooms/{roomId}/messages
Retrieves paginated messages for a room. **Query** (`ChatMessageFilterRequest`): `pageNumber`, `pageSize`.

## POST /api/chat/rooms/{roomId}/messages
Sends a message to a room.

**Body** (`SendMessageRequest`):
```json
{ "content": "Hello!", "attachments": [ { "fileUrl": "https://cdn.example.com/a.png", "fileType": "image" } ] }
```
`content` is optional when at least one attachment is present (max 5 attachments, each an absolute http(s) URL).

**Response** (`Result<ChatMessageResponse>`): the created message, including `attachments`.

**Errors**: participant check; `409 CHAT_ROOM_REJECTED`; `409 CHAT_ROOM_NOT_PENDING` (recipient replying before accepting); `403 CHAT_USER_BLOCKED`; `400 COMMON_VALIDATION_ERROR` (no content and no attachment, or >5 attachments).

---

## User blocking

| Endpoint | Effect |
|---|---|
| `PUT /api/users/{userId}/block` | Blocks the user (idempotent). Body: `{ "reason": "..." }` (optional). |
| `DELETE /api/users/{userId}/block` | Unblocks (idempotent). |
| `GET /api/users/me/blocked` | Lists users the caller has blocked. |

A block never deletes chat history — existing rooms simply become unable to receive new messages/requests between the two users.

---

## Notes
- No SignalR — poll `GET messages` for near-real-time UX.
- `Message.IsRead` exists but no mark-read endpoint is currently exposed.
- Migration backfill: every pre-existing `ChatRoom` row is set to `status = 'active'` — no existing coach↔learner conversation is interrupted.
