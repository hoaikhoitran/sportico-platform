# Frontend — Admin Dashboard UI

Role: `admin` (granted out of band; no self-service endpoint).

## Approve training packages
- Queue: `GET /api/admin/training-packages/pending` (paged).
- Approve: `PUT /api/admin/training-packages/{id}/approve` → `published`.
- Reject: `PUT /api/admin/training-packages/{id}/reject` with `{ "reason": "..." }` → `rejected`.
- UI: list pending packages with coach, sport, price, sessionCount; provide approve/reject actions. Require a reason for rejection. The coach is notified automatically.

## Verify payout accounts
- Queue: `GET /api/admin/coach-payout-accounts/pending` (paged; `pageNumber`, `pageSize`).
- Verify: `PUT /api/admin/coach-payout-accounts/{id}/verify` → `verified`.
- Reject: `PUT /api/admin/coach-payout-accounts/{id}/reject` with `{ "reason": "..." }` → `rejected`.
- UI: show bank details (`bankName`, masked `bankAccountNumber`, `bankAccountHolder`, `payoutMethod`). A coach cannot withdraw until their account is `verified`.

## Manage withdrawals
- Queue: `GET /api/admin/withdrawal-requests` (paged; `?status=`) — also `/pending` for back-compat.
- Approve: `PUT /api/admin/withdrawal-requests/{id}/approve`.
  - **Manual mode** (`PayOs__AutoPayoutEnabled=false`): → `approved` (no balance change). Admin then transfers externally and calls mark-paid.
  - **Auto mode** (`PayOs__AutoPayoutEnabled=true`): triggers the PayOS payout → response is `processing`, `paid`, or `failed`. The UI must **read the returned status** and refresh the list/detail — do **not** show "money transferred" on approve unless the response is `paid`.
- Mark paid: `PUT /api/admin/withdrawal-requests/{id}/mark-paid` → `paid` (manual mode, after the external transfer; writes the ledger debit). Blocked while `processing`.
- Refresh payout status: `PUT /api/admin/withdrawal-requests/{id}/refresh-payout-status` (auto mode) — reconcile a `processing` payout to `paid`/`failed`.
- Retry payout: `POST /api/admin/withdrawal-requests/{id}/retry-payout` — retry a `failed` payout.
- Reject: `PUT /api/admin/withdrawal-requests/{id}/reject` with `{ "adminNote": "..." }` → `rejected` (funds returned). Blocked while `processing`/`paid`.

Status labels (never claim payment until the backend returns `paid`):

| Status | Label (vi) |
|---|---|
| `pending` | Chờ duyệt |
| `approved` | Đã duyệt, cần xác nhận đã chuyển tiền (manual mode) |
| `processing` | PayOS đang xử lý chuyển khoản |
| `paid` | Đã thanh toán |
| `failed` | Chuyển khoản thất bại |
| `rejected` | Đã từ chối |

## Sports
- Create: `POST /api/sports` (`name`, optional `slug`, `description`, `iconUrl`). Used to seed the catalog so coaches can register sports and create packages.

## Notes
- All admin endpoints require the `admin` role; expect `403` otherwise.
- There is no admin user-management UI in the current API (no user list/ban endpoints were found). Treat user administration as out of scope for now.
