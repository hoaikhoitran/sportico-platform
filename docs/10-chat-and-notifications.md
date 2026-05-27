# 10 — Chat and Notifications

## Chat

### Model
- `ChatRoom` — a 1:1 conversation between `User1Id` and `User2Id`. Participants are stored **ordered by GUID** (the lower GUID is `User1Id`), not as (learner, coach).
- `Message` — `RoomId`, `SenderId`, `Content`, `IsRead`, `SentAt`.
- `MessageAttachment` — optional attachments (entity present; no upload endpoint reviewed).

### When a room exists
A chat room is created automatically when a booking is **activated** (manual purchase, or PayOS webhook success). The same room is reused for subsequent bookings between the same two users.

### Access control — chat only after an active booking
Both reading messages and sending messages enforce two checks:
1. The caller must be a participant of the room (`User1Id` or `User2Id`), else `403 CHAT_NOT_ALLOWED`.
2. The two participants must share a booking in `active` or `completed` status, else `403 CHAT_NOT_ALLOWED` ("Chat is not allowed without an active booking"). Because room participants are ordered by GUID, the service checks both `(user1,user2)` and `(user2,user1)` orderings against bookings' `(LearnerId, CoachId)`.

### Endpoints
| Method | Route | Purpose |
|---|---|---|
| GET | `/api/chat/rooms` | Rooms for the current user |
| GET | `/api/chat/rooms/{roomId}/messages` | Messages (paged, newest-first conventions per repository) |
| POST | `/api/chat/rooms/{roomId}/messages` | Send a message |

### No SignalR in MVP
Chat is **plain REST**; there is no SignalR/WebSocket hub. Clients should **poll** `GET messages` (and optionally the unread notification count) on an interval for near-real-time behaviour. Real-time push is a future enhancement, not a current requirement.

> NOTE: `Message.IsRead` exists but no endpoint to mark messages read was found in the reviewed `ChatController`. Treat message read-state management as not yet exposed.

## Notifications

### Model
`Notification` — `UserId`, `Title`, `Content?`, `Type`, `IsRead`, `CreatedAt`. Types are string constants (`NotificationTypeConstants`): `booking`, `training_session`, `wallet`, `training_package`, `payment`, `message`, `review`, `follow`, `package`, `post`, `system`, `report`.

### Triggers (observed in services)

| Event | Recipient | Type | Title |
|---|---|---|---|
| Booking activated | Coach | `booking` | "You have a new booking" |
| Booking activated | Learner | `booking` | "Your booking is active" |
| Session requested | Coach | `training_session` | "New training session request" |
| Session confirmed | Learner | `training_session` | "Training session confirmed" |
| Session cancelled | Other party | `training_session` | "Training session cancelled" |
| Session completed | Learner | `training_session` | "Training session completed" |
| Session completed (wallet credited) | Coach | `wallet` | "Wallet credited" |
| Withdrawal approved | Coach | `wallet` | "Withdrawal approved" |
| Withdrawal rejected | Coach | `wallet` | "Withdrawal rejected" |
| Withdrawal paid | Coach | `wallet` | "Withdrawal paid" |

> NOTE: Training-package approve/reject also notify the coach (per the package services). The exact title/type for those notifications should be confirmed in `AdminTrainingPackageService` if the frontend needs to special-case them; this doc lists what was directly observed in the booking/session/withdrawal services.

### API behaviour

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/notifications/me` | Paged list for the current user |
| GET | `/api/notifications/me/unread-count` | Count of unread (returns `Result<int>`) |
| PUT | `/api/notifications/{id}/read` | Mark a single notification read |
| PUT | `/api/notifications/me/read-all` | Mark all the user's notifications read |

### Read/unread state
- New notifications are created with `IsRead = false`.
- `unread-count` powers a badge.
- `mark read` / `read-all` flip the state; the frontend should refresh the count after either call.
- Notifications are scoped to the authenticated user; one user cannot read or mutate another user's notifications.
