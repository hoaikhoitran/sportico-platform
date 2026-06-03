# API — Admin

Consolidated list of admin-only (`[Authorize(Roles = "admin")]`) capabilities. Detailed bodies are in the linked module docs.

## User Management

`AdminUsersController` — all endpoints `[Authorize(Roles = RoleConstants.Admin)]`.

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/admin/users` | List users with pagination, search, role/status filters |
| GET | `/api/admin/users/{id}` | Get user detail by id |
| POST | `/api/admin/users` | Create user |
| PUT | `/api/admin/users/{id}` | Update user basic information and roles |
| DELETE | `/api/admin/users/{id}` | Deactivate user (status → `inactive`; **not** a physical delete) |

**GET /api/admin/users** — query (`AdminUserFilterRequest`): `?search=&role=&status=&pageNumber=1&pageSize=10`.
- `search` matches email / full name / phone (case-insensitive `ILIKE`); `role` filters by role name; `status` by user status.
- Newest first (`CreatedAt DESC`), paged in the database. Returns `Result<PagedResult<AdminUserResponse>>`.

**POST /api/admin/users** (`AdminCreateUserRequest`): `email, fullName, phone?, avatarUrl?, dateOfBirth?, password, status, roles[]`.
- Email must be unique → `409 USER_EMAIL_ALREADY_EXISTS`. Password is hashed (`PasswordHelper`); never stored or returned in plain text.
- Every role must exist → otherwise `404 COMMON_ROLE_NOT_FOUND`. `user_roles` rows are created accordingly.
- Status must be one of `active | inactive | banned | pending`.

**PUT /api/admin/users/{id}** (`AdminUpdateUserRequest`): `fullName, phone?, avatarUrl?, dateOfBirth?, status, roles?`.
- Email and password are **not** changed here. `roles` null → roles unchanged; provided → replaces roles (all must exist, validated first).
- `404 USER_NOT_FOUND` if the user does not exist.

**DELETE /api/admin/users/{id}** — sets `status = inactive` and updates `UpdatedAt`; preserves bookings/payments/reviews/sessions/chat/wallet FKs. `404 USER_NOT_FOUND` if missing.

`AdminUserResponse` never exposes `PasswordHash`, refresh/verification/reset tokens or any secret. It includes `roles[]` (sorted) plus optional coach/learner profile summaries.

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
