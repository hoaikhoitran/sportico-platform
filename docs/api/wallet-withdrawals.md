# API — Coach Wallet & Withdrawals

Controllers: `CoachWalletsController`, `CoachPayoutAccountsController`, `AdminCoachPayoutAccountsController`, `WithdrawalRequestsController`.

See [08 — Payment and Wallet](../08-payment-and-wallet.md) for balance semantics.

---

## Money flow summary

```
Learner purchase
  └► BookingService snapshots 15% platform fee
      CoachReceiveAmount     = Price × 0.85
      PerSessionCoachAmount  = CoachReceiveAmount / TotalSessions

Session completed (coach marks complete)
  └► CoachWallet.AvailableBalance += PerSessionCoachAmount  (already net of 15%)

Coach requests withdrawal
  └► WithdrawalService.CreateAsync:
      AvailableBalance -= amount   ← reservation, NOT a second commission
      PendingBalance   += amount
      └► If AutoPayoutEnabled = true:
             PayOS payout created automatically
         Else:
             Admin approves → marks paid manually

PayOS payout SUCCESS
  └► PendingBalance -= amount
      TotalWithdrawn  += amount
      CoachWalletTransaction (debit, type=withdrawal) created once

PayOS payout FAILED
  └► PendingBalance  -= amount
      AvailableBalance += amount   (funds returned, no ledger entry)
```

> **The 15% platform commission is only deducted at purchase time.
> It is NEVER deducted again during withdrawal.**

---

## Auto-payout configuration

Set in `appsettings.json` → `PayOs` section:

```json
"PayOs": {
  "AutoPayoutEnabled": false,
  "PayoutCategory": "salary"
}
```

- `AutoPayoutEnabled = true` — PayOS Chi API is called immediately when the coach submits a withdrawal request.
- `AutoPayoutEnabled = false` — Admin manually approves and marks paid (original flow, safe for development).

---

## Payout account (prerequisite)

Coach must have a **verified** `CoachPayoutAccount` with all fields set:
- `BankName`
- `BankBin` — 6-digit bank BIN required by PayOS `toBin` field (e.g. `970415` for VietinBank)
- `BankAccountNumber`
- `BankAccountHolder`

The admin verifies the account before it can be used for payouts.
Withdrawal is rejected if no verified account exists.

---

## Coach endpoints

### POST /api/coaches/me/withdrawal-requests
Creates a withdrawal request. If `AutoPayoutEnabled`, the PayOS payout is triggered automatically.

**Body**: `{ "amount": 212500 }`

**Errors**:
| Code | Meaning |
|------|---------|
| `400 COMMON_VALIDATION_ERROR` | Invalid amount |
| `403 COACH_PROFILE_REQUIRED` | No coach profile |
| `409 PAYOUT_ACCOUNT_REQUIRED` | No verified payout account |
| `409 INSUFFICIENT_WALLET_BALANCE` | Amount exceeds AvailableBalance |

**Response** (`Result<WithdrawalRequestResponse>`): includes `payOsPayoutId`, `payOsPayoutStatus`, `processingAt`, `paidAt`, `failureReason`.

### GET /api/coaches/me/withdrawal-requests
List own withdrawal requests. Query: `?status=...&pageNumber=1&pageSize=10`.

### GET /api/coaches/me/withdrawal-requests/{id}
Get a single own withdrawal request (ownership enforced → `403 COMMON_FORBIDDEN` otherwise,
`404 WITHDRAWAL_REQUEST_NOT_FOUND` if missing). Returns `WithdrawalRequestResponse`.

### GET /api/coaches/me/withdrawal-requests/{id}/receipt
Withdrawal receipt for the authenticated coach.

**Response** (`Result<WithdrawalReceiptResponse>`): receipt number, masked bank account (e.g. `******1234`), payout status, timestamps, and the statement that no additional commission is deducted.

---

## Admin endpoints

| Method | Route | Purpose |
|--------|-------|---------|
| `GET` | `/api/admin/withdrawal-requests` | List **all** withdrawals; `?status=` filters any status |
| `GET` | `/api/admin/withdrawal-requests/pending` | List **pending only** (back-compat; = `?status=pending`) |
| `GET` | `/api/admin/withdrawal-requests/{id}` | Single withdrawal detail (review modals) |
| `PUT` | `/api/admin/withdrawal-requests/{id}/approve` | Approve (manual flow) |
| `PUT` | `/api/admin/withdrawal-requests/{id}/reject` | Reject + return balance |
| `PUT` | `/api/admin/withdrawal-requests/{id}/mark-paid` | Manual mark-paid fallback |
| `PUT` | `/api/admin/withdrawal-requests/{id}/refresh-payout-status` | Query PayOS for latest state |
| `POST` | `/api/admin/withdrawal-requests/{id}/retry-payout` | Retry failed payout |
| `GET` | `/api/admin/withdrawal-requests/{id}/receipt` | View receipt |

### GET /api/admin/withdrawal-requests (full list)
Query: `?status=...&pageNumber=1&pageSize=10`. `status` accepts any of
`pending | approved | processing | paid | rejected | failed | cancelled`.
An unknown status returns `400 COMMON_VALIDATION_ERROR`. Omit `status` to list every withdrawal.
The older `/pending` route is retained for backward compatibility.

### GET /api/admin/withdrawal-requests/{id} (detail)
Returns `WithdrawalRequestResponse` for review modals (distinct from `/receipt`).
`404 WITHDRAWAL_REQUEST_NOT_FOUND` if missing.

### mark-paid safety
Blocked if `status = processing` AND `payOsPayoutId` is set — prevents double payment.
Use `refresh-payout-status` first to confirm the PayOS result before overriding manually.

### reject safety
Blocked if `status = processing` — prevents rejecting while a PayOS payout is in flight.
Use `refresh-payout-status` to get the current state first.

---

## Withdrawal status lifecycle

```
pending
  ├─ (AutoPayoutEnabled) ──► processing ──► paid
  │                      │            └──► failed
  ├─ (admin approve)     ──► approved ──► paid (manual mark-paid)
  └─ (admin reject)      ──► rejected
```

| Status | Meaning |
|--------|---------|
| `pending` | Created; payout not yet initiated |
| `processing` | PayOS accepted the payout; awaiting confirmation |
| `paid` | Payout succeeded; funds delivered |
| `rejected` | Admin rejected; funds returned to AvailableBalance |
| `failed` | PayOS payout failed; funds returned to AvailableBalance |
| `approved` | Admin approved (manual-flow only) |

---

## Withdrawal receipt

Fields include: `receiptNumber`, `amount`, `currency`, `status`, `payOsPayoutId`, `payOsPayoutStatus`,
`maskedAccountNumber`, `bankName`, `bankBin`, `accountHolderName`, `createdAt`, `processingAt`, `paidAt`.

The receipt always contains:
> "Platform commission was already deducted during booking purchase. No additional commission is deducted from this withdrawal."

---

## Idempotency & safety

- `PayOsReferenceId` = `WithdrawalRequest.Id` (first attempt).
- `x-idempotency-key` header = same as referenceId.
- Retry uses a unique key: `{id}-retry-{timestamp}`.
- Wallet is locked with `GetByCoachIdForUpdateAsync` to prevent concurrent overspend.
- If PayOS call times out (no response), withdrawal stays `processing` and the failure reason is recorded. Use `refresh-payout-status` to reconcile.

## Balance invariants

Funds reserved at creation (`Available → Pending`) should always cover the amount when later
released or returned. Every operation that subtracts from `PendingBalance` — **reject**, **mark-paid /
finalize**, and **rollback on failure** — first asserts `PendingBalance >= amount`. If old or
concurrently-mutated data would otherwise push the balance negative, the operation fails with
`409 INSUFFICIENT_WALLET_BALANCE` instead of writing a negative `PendingBalance`.

| Operation | Wallet effect |
|-----------|---------------|
| Create | `Available -= amount`, `Pending += amount` |
| Reject / Fail | `Pending -= amount`, `Available += amount` (guarded) |
| Mark paid / payout success | `Pending -= amount`, `TotalWithdrawn += amount` (guarded), one debit ledger row |
| Retry (failed → processing) | `Available -= amount`, `Pending += amount`, new idempotency key |
