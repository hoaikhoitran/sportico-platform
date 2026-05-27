# API — Wallet & Withdrawals

Controllers: `CoachWalletsController`, `CoachPayoutAccountsController`, `AdminCoachPayoutAccountsController`, `WithdrawalRequestsController`.

See [08 — Payment and Wallet](../08-payment-and-wallet.md) for balance semantics.

## Wallet — `/api/coaches/me/wallet` (role `coach`)

### GET /api/coaches/me/wallet
- **Response** (`Result<CoachWalletResponse>`):
```json
{ "id": "guid", "coachId": "guid", "availableBalance": 106250, "pendingBalance": 0,
  "totalEarned": 106250, "totalWithdrawn": 0, "createdAt": "date", "updatedAt": "date" }
```
- **Errors**: `404 COACH_WALLET_NOT_FOUND` (no wallet yet — created on first booking activation/session completion).

### GET /api/coaches/me/wallet/transactions
- **Query** (`CoachWalletTransactionFilterRequest`): `pageNumber`, `pageSize` (plus any type/direction filters defined there).
- **Response** (`Result<PagedResult<CoachWalletTransactionResponse>>`). Each entry has `type` (`session_release|withdrawal|adjustment`), `direction` (`credit|debit`), `amount`, `referenceType`, `referenceId`, `createdAt`.

## Payout account — `/api/coaches/me/payout-account` (role `coach`)

### GET /api/coaches/me/payout-account
- **Response** (`Result<CoachPayoutAccountResponse>`). `404 COACH_PAYOUT_ACCOUNT_NOT_FOUND` if none.

### PUT /api/coaches/me/payout-account  — upsert
- **Body** (`UpsertCoachPayoutAccountRequest`):
```json
{ "payoutMethod": "bank", "bankName": "Bank X",
  "bankAccountNumber": "0123456789", "bankAccountHolder": "Coach Name" }
```
- **Effect**: creates or replaces the account; status reset to `pending` (must be re-verified by an admin).

## Admin payout verification — `/api/admin/coach-payout-accounts` (role `admin`)

### GET /api/admin/coach-payout-accounts/pending
- **Query**: `pageNumber` (default 1), `pageSize` (default 10).
- **Response** (`Result<PagedResult<CoachPayoutAccountResponse>>`).

### PUT /api/admin/coach-payout-accounts/{id}/verify
Set status `verified`.

### PUT /api/admin/coach-payout-accounts/{id}/reject
- **Body** (`RejectCoachPayoutAccountRequest`): `{ "reason": "..." }`. Set status `rejected`.

## Withdrawals — `WithdrawalRequestsController`

### POST /api/coaches/me/withdrawal-requests  — coach
- **Body**: `{ "amount": 106250 }`.
- **Effect**: requires a **verified** payout account and `amount <= availableBalance`. Moves `amount` from `available` to `pending`; creates a `pending` request.
- **Response** (`Result<WithdrawalRequestResponse>`).
- **Errors**: `403 COACH_PROFILE_REQUIRED`; `409 PAYOUT_ACCOUNT_REQUIRED` (no verified account); `404 COACH_WALLET_NOT_FOUND`; `409 INSUFFICIENT_WALLET_BALANCE`; `400 COMMON_VALIDATION_ERROR`.

### GET /api/coaches/me/withdrawal-requests  — coach
Paged list of the coach's requests (`WithdrawalRequestFilterRequest`: `status`, `pageNumber`, `pageSize`).

### GET /api/admin/withdrawal-requests/pending  — admin
Paged list of pending requests.

### PUT /api/admin/withdrawal-requests/{id}/approve  — admin
Status `pending → approved`. Notifies the coach. (No balance change.)

### PUT /api/admin/withdrawal-requests/{id}/mark-paid  — admin
Status `→ paid`. Moves `amount` from `pending` to `totalWithdrawn`; writes a `withdrawal`/`debit` ledger entry. Notifies the coach.

### PUT /api/admin/withdrawal-requests/{id}/reject  — admin
- **Body** (`RejectWithdrawalRequest`): `{ "adminNote": "..." }`.
- Status `→ rejected`. Returns `amount` from `pending` to `available`. Notifies the coach.

**Common errors**: `404 WITHDRAWAL_REQUEST_NOT_FOUND`; `404 COACH_WALLET_NOT_FOUND`.

> NOTE: There is no real automated bank transfer; "mark paid" records that an external payout occurred.
