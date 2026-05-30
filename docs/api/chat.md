# API — Chat

Controller: `ChatController` (`/api/chat`). All endpoints require authentication.

## Business rules
- Chat is **independent from package purchase**. Any authenticated user may open a chat room with a coach without buying a package.
- A chat room is created on demand (when the user clicks "Chat") — never automatically.
- There is exactly **one ChatRoom per user-coach pair**. Calling `POST /api/chat/rooms` multiple times returns the same room.
- Chat access is gated on **room participation only** — not on booking status, package expiry, or anything else.
- Chat remains accessible after a package expires or a booking is completed/cancelled.

---

## POST /api/chat/rooms
Opens or retrieves the chat room between the current user and a coach.

**Request body** (`CreateChatRoomRequest`):
```json
{ "coachId": "..." }
```

**Response** (`Result<ChatRoomResponse>`): the room (`id`, `user1Id`, `user2Id`, `createdAt`).

**Errors**:
| Code | Meaning |
|------|---------|
| `400 COMMON_VALIDATION_ERROR` | `coachId` is empty |
| `403 COMMON_FORBIDDEN` | Caller and coach are the same user (self-chat) |
| `404 COACH_PROFILE_NOT_FOUND` | No coach profile for the given `coachId` |

**Idempotent**: calling it multiple times with the same `coachId` always returns the same room. Race conditions between concurrent requests are handled safely — a unique index on `(user1Id, user2Id)` prevents duplicates at the DB level; the service re-queries and returns the existing room if a concurrent insert wins.

---

## GET /api/chat/rooms
Returns all chat rooms the current user participates in (ordered by newest first).

**Response** (`Result<List<ChatRoomResponse>>`): empty list if no rooms exist yet.

---

## GET /api/chat/rooms/{roomId}/messages
Retrieves paginated messages for a room.

**Query** (`ChatMessageFilterRequest`): `pageNumber`, `pageSize`.

**Response** (`Result<PagedResult<ChatMessageResponse>>`).

**Errors**:
- `404 CHAT_NOT_ALLOWED` — room not found
- `403 CHAT_NOT_ALLOWED` — caller is not a participant of the room

---

## POST /api/chat/rooms/{roomId}/messages
Sends a message to a room.

**Body** (`SendMessageRequest`): `{ "content": "Hello" }`.

**Response** (`Result<ChatMessageResponse>`): the created message.

**Errors**: same participant check as above; `400 COMMON_VALIDATION_ERROR` for empty/too-long content.

---

## Permissions summary
| Check | Failure |
|-------|---------|
| `coachId` refers to an existing coach profile | `404 COACH_PROFILE_NOT_FOUND` |
| Caller is not the same user as the coach | `403 COMMON_FORBIDDEN` |
| Caller is `User1Id` or `User2Id` of the room | `403 CHAT_NOT_ALLOWED` |

No booking or package checks are performed on message read/send.

---

## Notes
- Rooms are **never** created automatically (not on purchase, not on booking activation, not on session creation).
- No SignalR — poll `GET messages` for near-real-time UX.
- `Message.IsRead` exists but no mark-read endpoint is currently exposed.
