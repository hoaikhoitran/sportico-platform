# API — Chat

Controller: `ChatController` (`/api/chat`). All endpoints require authentication. See [10 — Chat and Notifications](../10-chat-and-notifications.md).

Chat is gated: the caller must be a room participant **and** share an `active`/`completed` booking with the other participant.

## GET /api/chat/rooms
- **Response** (`Result<List<ChatRoomResponse>>`): the rooms the current user belongs to.

## GET /api/chat/rooms/{roomId}/messages
- **Query** (`ChatMessageFilterRequest`): `pageNumber`, `pageSize`.
- **Response** (`Result<PagedResult<ChatMessageResponse>>`).
- **Errors**: `404 CHAT_NOT_ALLOWED` (room not found); `403 CHAT_NOT_ALLOWED` (not a participant, or no shared active/completed booking).

## POST /api/chat/rooms/{roomId}/messages
- **Body** (`SendMessageRequest`): `{ "content": "Hello" }`.
- **Response** (`Result<ChatMessageResponse>`): the created message (`id`, `roomId`, `senderId`, `content`, `isRead`, `sentAt`).
- **Errors**: same gating as above; `400 COMMON_VALIDATION_ERROR` for empty content.

## Permissions summary
| Check | Failure |
|---|---|
| Caller is `User1Id` or `User2Id` of the room | `403 CHAT_NOT_ALLOWED` |
| Caller shares an `active`/`completed` booking with the other party | `403 CHAT_NOT_ALLOWED` |

## Notes
- Rooms are created automatically when a booking is activated.
- No SignalR — poll `GET messages` for near-real-time UX.
- `Message.IsRead` exists but no mark-read endpoint is exposed for messages (notifications have their own read endpoints).
