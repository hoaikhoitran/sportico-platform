# API — Notifications

Controller: `NotificationsController` (`/api/notifications`). All endpoints require authentication and are scoped to the current user. See [10 — Chat and Notifications](../10-chat-and-notifications.md).

## GET /api/notifications/me
- **Query** (`NotificationFilterRequest`): `pageNumber`, `pageSize` (plus any unread/type filters defined there).
- **Response** (`Result<PagedResult<NotificationResponse>>`):
```json
{
  "id": "guid", "title": "Your booking is active", "content": "...",
  "type": "booking", "isRead": false, "createdAt": "date"
}
```

## GET /api/notifications/me/unread-count
- **Response** (`Result<int>`): the number of unread notifications. Use for a badge.

## PUT /api/notifications/{id}/read
- Marks a single notification read. **Response** `Result<object>`.
- **Errors**: `404 NOTIFICATION_NOT_FOUND` (or if it belongs to another user).

## PUT /api/notifications/me/read-all
- Marks all of the user's notifications read. **Response** `Result<object>` (a count is returned via `MarkAllNotificationsReadResponse`).

## Types
`type` values come from `NotificationTypeConstants`: `booking`, `training_session`, `wallet`, `training_package`, `payment`, `message`, `review`, `follow`, `package`, `post`, `system`, `report`.

## Read/unread behaviour
- Created with `isRead: false`.
- After `read` / `read-all`, refresh `unread-count`.
- A user can only read/mutate their own notifications.
