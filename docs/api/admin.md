# API — Admin

Consolidated list of admin-only (`[Authorize(Roles = "admin")]`) capabilities. Detailed bodies are in the linked module docs.

## Sports
| Method | Route | Purpose |
|---|---|---|
| POST | `/api/sports` | Create a sport (`CreateSportRequest`: `name`, `slug?`, `description?`, `iconUrl?`). Errors: `SPORT_NAME_ALREADY_EXISTS`, `SPORT_SLUG_ALREADY_EXISTS`. |

## Training Package Moderation
See [training-packages.md](training-packages.md).
| Method | Route | Purpose |
|---|---|---|
| GET | `/api/admin/training-packages/pending` | List pending packages (paged) |
| PUT | `/api/admin/training-packages/{id}/approve` | Approve → `published` |
| PUT | `/api/admin/training-packages/{id}/reject` | Reject → `rejected` (`{ "reason": "..." }`) |

## Coach Payout Account Verification
See [wallet-withdrawals.md](wallet-withdrawals.md).
| Method | Route | Purpose |
|---|---|---|
| GET | `/api/admin/coach-payout-accounts/pending` | List pending payout accounts |
| PUT | `/api/admin/coach-payout-accounts/{id}/verify` | Verify |
| PUT | `/api/admin/coach-payout-accounts/{id}/reject` | Reject (`{ "reason": "..." }`) |

## Withdrawal Requests
See [wallet-withdrawals.md](wallet-withdrawals.md).
| Method | Route | Purpose |
|---|---|---|
| GET | `/api/admin/withdrawal-requests/pending` | List pending withdrawals |
| PUT | `/api/admin/withdrawal-requests/{id}/approve` | Approve |
| PUT | `/api/admin/withdrawal-requests/{id}/mark-paid` | Mark paid (writes ledger debit) |
| PUT | `/api/admin/withdrawal-requests/{id}/reject` | Reject (`{ "adminNote": "..." }`, returns funds) |

## Legacy admin (do not extend)
| Method | Route | Purpose |
|---|---|---|
| POST/PUT | `/api/packages`, `/api/packages/{id}`, `/api/packages/{id}/status` | Manage subscription tiers |
| GET/PUT | `/api/admin/posts/pending`, `/api/admin/posts/{id}/approve`, `/api/admin/posts/{id}/reject` | Moderate coach posts |

See [17 — Legacy Modules](../17-legacy-modules.md).

## Granting the admin role
There is **no** admin self-service endpoint. Assign the `admin` role out of band by inserting a `user_roles` row (`role_id` of the `admin` role) for the target user.
