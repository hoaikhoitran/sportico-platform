# Frontend — Routes and Pages

Recommended route map (Next.js-style paths). These are suggestions for the frontend; the backend does not dictate routes.

| Route | Role | Backend endpoints |
|---|---|---|
| `/login` | Public | `POST /api/auth/login` |
| `/register` | Public | `POST /api/auth/register`, `GET /api/auth/verify-email` |
| `/training-packages` | Public | `GET /api/public/training-packages` |
| `/training-packages/[id]` | Public | `GET /api/public/training-packages/{id}` |
| `/coach/dashboard` | Coach | `GET /api/bookings/coach`, `GET /api/training-packages/me`, `GET /api/coaches/me/wallet` |
| `/coach/training-packages` | Coach | `GET/POST/PUT /api/training-packages`, archive |
| `/coach/bookings` | Coach | `GET /api/bookings/coach`, `GET /api/bookings/coach/{id}` |
| `/coach/wallet` | Coach | `GET /api/coaches/me/wallet`, `.../transactions` |
| `/coach/withdrawals` | Coach | `GET/POST /api/coaches/me/withdrawal-requests`, `PUT /api/coaches/me/payout-account` |
| `/learner/dashboard` | Learner | `GET /api/bookings/me`, `GET /api/notifications/me/unread-count` |
| `/learner/bookings` | Learner | `GET /api/bookings/me`, `GET /api/bookings/{id}` |
| `/learner/training-plan/[bookingId]` | Learner | `GET /api/bookings/{bookingId}/training-plan`, `.../assessment` |
| `/learner/progress` | Learner | `GET/POST /api/bookings/{bookingId}/progress-checkins` |
| `/admin/training-packages` | Admin | `GET /api/admin/training-packages/pending`, approve/reject |
| `/admin/withdrawals` | Admin | `GET /api/admin/withdrawal-requests/pending`, approve/mark-paid/reject; payout-account verify |
| `/chat` | Coach/Learner | `GET /api/chat/rooms`, `.../messages` |
| `/notifications` | All | `GET /api/notifications/me`, unread-count, read, read-all |

## Session scheduling

Session actions are typically embedded in the booking detail page rather than a standalone route:
- Learner requests: `POST /api/bookings/{bookingId}/sessions`.
- List: `GET /api/bookings/{bookingId}/sessions`.
- Coach confirm/complete, either party cancel: `PUT /api/training-sessions/{id}/confirm|cancel|complete`.

## Payment return pages (PayOS)

Configure these to match `PayOs:ReturnUrl` / `PayOs:CancelUrl`:
- `/payment/success`
- `/payment/cancel`

After returning, the frontend should poll `GET /api/bookings/{id}` until the status flips to `active` (the webhook drives the actual state change server-side).

## Route guarding

- Public routes: no guard.
- Role routes: guard by the roles present in the JWT. A user may have multiple roles; show the dashboards they qualify for.
- See [auth-integration.md](auth-integration.md).
