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
- Queue: `GET /api/admin/withdrawal-requests/pending` (paged; filter by `status`).
- Approve: `PUT /api/admin/withdrawal-requests/{id}/approve` → `approved` (no balance change).
- Mark paid: `PUT /api/admin/withdrawal-requests/{id}/mark-paid` → `paid` (after the external bank transfer is done; writes the ledger debit).
- Reject: `PUT /api/admin/withdrawal-requests/{id}/reject` with `{ "adminNote": "..." }` → `rejected` (funds returned to the coach's available balance).
- UI: show coach, amount, requested date, and the linked payout account. Make the "mark paid" action explicit (it represents a real-world transfer that must already have happened).

## Sports
- Create: `POST /api/sports` (`name`, optional `slug`, `description`, `iconUrl`). Used to seed the catalog so coaches can register sports and create packages.

## Notes
- All admin endpoints require the `admin` role; expect `403` otherwise.
- There is no admin user-management UI in the current API (no user list/ban endpoints were found). Treat user administration as out of scope for now.
