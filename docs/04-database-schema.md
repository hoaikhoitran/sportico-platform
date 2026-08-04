# 04 — Database Schema

## Engine

PostgreSQL, accessed through **EF Core 8** with the **Npgsql** provider. The `DbContext` is [AppDbContext](../src/SporticoApp.Infrastructure/Persistence/AppDbContext.cs); entity mappings live in `SporticoApp.Infrastructure/Persistence/Configurations` (one `IEntityTypeConfiguration<T>` per entity).

## Naming Convention — snake_case

C# entities use `PascalCase`; the database uses `snake_case`. `AppDbContext.OnModelCreating` applies configurations and then runs `ApplySnakeCaseNames`, which converts every table, view, and column name automatically:

- `TrainingPackage` → table `training_packages`
- `Booking.PlatformFeeAmount` → column `platform_fee_amount`

Most configurations also set an explicit `ToTable("...")` name; the snake_case pass normalizes the result consistently.

## PostgreSQL Extensions

Enabled in `OnModelCreating`:

```csharp
modelBuilder.HasPostgresExtension("citext");    // case-insensitive text
modelBuilder.HasPostgresExtension("pg_trgm");   // trigram fuzzy search
modelBuilder.HasPostgresExtension("pgcrypto");  // gen_random_uuid(), crypto
```

GUID primary keys default to `gen_random_uuid()`; timestamp columns default to `now()`.

## Money Precision

Monetary and rate columns use fixed precision (no floating point):

| Column kind | Precision | Example columns |
|---|---|---|
| Amounts | `numeric(12,2)` | `bookings.total_amount`, `platform_fee_amount`, `coach_receive_amount`, `per_session_coach_amount`, `training_packages.price`, `payments.amount` |
| Fee rate | `numeric(5,4)` | `bookings.platform_fee_rate` (e.g. `0.1500`) |

Wallet balances (`coach_wallets`) and transaction amounts are likewise decimal. Always treat money as `decimal` on the client; never parse into a float.

## Representative Indexes & Constraints

### bookings
- Indexes: `idx_bookings_learner`, `idx_bookings_coach`, `idx_bookings_training_package`, `idx_bookings_status`, `idx_bookings_created_at` (descending).
- FKs: learner → users (Restrict), coach → coach_profiles, training_package → training_packages (Restrict).
- `status` defaults to `'pending_payment'`.

### training_packages
- Indexes: coach, sport, status, created_at (desc), plus a **filtered index** `idx_training_packages_published` on `status` where `status = 'published'` (fast public listing).
- `status` defaults to `'pending'`.
- `start_date` / `end_date` (added by migration `AddTrainingPackageScheduledSessions`, default `now()` to backfill legacy rows). `duration_days` is now derived from these.

### training_package_session_slots (added by `AddTrainingPackageScheduledSessions`)
- The fixed schedule of a package: `training_package_id` (FK → training_packages, **Cascade**), `session_number`, `start_time`, `end_time`, `level`, `location`, `is_online`, `meeting_url`, `note`, `max_participants` (default 1), `booked_participants` (default 0), `status` (`open|full|cancelled`, default `'open'`), `version` (optimistic-concurrency token, default 0).
- Indexes: `idx_training_package_session_slots_package`, `idx_training_package_session_slots_status`, and a **unique** index `uq_training_package_session_slots_package_number` on `(training_package_id, session_number)`.

### training_sessions
- `training_package_session_slot_id` (added by `AddTrainingPackageScheduledSessions`, nullable; FK → training_package_session_slots, **SetNull**) links a purchase-generated session to its package slot.
- Index `ix_training_sessions_package_session_slot_id`, plus a **unique filtered** index `uq_training_sessions_booking_package_slot` on `(booking_id, training_package_session_slot_id)` where `training_package_session_slot_id IS NOT NULL` — the idempotency backstop preventing duplicate generated sessions.

### payments
- Unique index on `transaction_code`.
- Unique **filtered** index `idx_payments_order_code` on `order_code` where `order_code IS NOT NULL` (PayOS order codes).
- Indexes on `(reference_type, reference_id)`, `status`, `user_id`, `created_at` (desc).
- Check constraints: `method IN ('manual','payos')`, `status IN ('pending','paid','failed','cancelled')`.

### reviews
- One review per `(coach_id, learner_id)` — unique index `uq_reviews_pair`.
- Columns added by migration `AddCoachReviewsFlow`: `booking_id` (FK → bookings, SetNull), `status`
  (`active|hidden|deleted`, default `'active'`), `deleted_at`, `deleted_by_user_id`, `moderation_reason`.
- Indexes: `idx_reviews_coach`, `idx_reviews_learner`, `idx_reviews_created_at` (desc),
  `idx_reviews_booking` (filtered, `booking_id IS NOT NULL`), and `idx_reviews_coach_status_created`
  on `(coach_id, status, created_at)` for the public active-review listing.
- Only `active` reviews count toward `coach_profiles.rating` / `total_reviews` (caches recalculated on
  every create/update/delete/moderation).

### reports
- Extended to target either a user or a review. `target_user_id` is now **nullable**; new columns:
  `target_type` (`user|review`, default `'user'`), `target_id`, `description`, `handled_by_user_id`,
  `handled_at`, `resolution_note`, `action_taken` (`none|review_hidden|review_deleted`).
- Index `idx_reports_target_entity` on `(target_type, target_id)` for the review-moderation queue.

Other tables follow the same pattern: indexes on FK columns and `status`, descending index on `created_at`, named primary keys (`<table>_pkey`) and named foreign keys (`fk_<table>_<relation>`).

> NOTE: Index/constraint details above are taken from the configuration classes that were reviewed (`BookingConfiguration`, `TrainingPackageConfiguration`, `PaymentConfiguration`). Other entities have analogous configurations in the same folder; consult them for exact index names.

## Migrations

Migrations live in `src/SporticoApp.Infrastructure/Migrations` and **are committed to the repository** (the `.gitignore` intentionally does not ignore them — schema changes are shared through git).

Current migration history (chronological):

| Migration | Purpose |
|---|---|
| `20260522175843_Baseline` | Initial schema baseline |
| `20260522180016_AddEmailVerificationTokenToUsers` | Email verification token |
| `20260522183932_RenameEmailVerificationTokenColumn` | Rename of the above column |
| `20260522185801_AddRefreshTokenFields` | Refresh token fields on users |
| `20260522190500_FixEmailVerificationTokenColumn` | Column fix |
| `20260526055807_AddPayOsFieldsToPayment` | PayOS fields on `payments` |
| `20260526092825_UpdatePaymentMethodConstraint` | Payment method/status check constraints |
| **`20260527034926_AddBookingTrainingFlow`** | **Booking + training/session/wallet/payout/personalization tables** |
| `20260620163848_InitSupabaseSchema` | Baseline snapshot for the production Supabase database |
| `20260623174211_AddTrainingPackageScheduledSessions` | Fixed-schedule `training_package_session_slots` |
| `20260711050207_AddConfigurablePlatformCommission` | `platform_settings` singleton (admin-editable commission, default 0%) |
| `20260721062200_AddVisitorAndApiRequestAnalytics` | Self-hosted visitor/page-view/API-request analytics tables |
| **`20260803160234_AddVoucherCommunityAndChatModules`** | **Voucher campaigns/redemptions + booking discount snapshot, community forum module, `chat_rooms` request/accept/reject extension, `user_blocks`** |

The booking-based marketplace schema is introduced by `20260527034926_AddBookingTrainingFlow`.

### New tables (`20260803160234_AddVoucherCommunityAndChatModules`)

| Table | Purpose |
|---|---|
| `voucher_campaigns` | Admin-managed, platform-funded discount campaigns. `code` is `citext` (case-insensitive unique). |
| `voucher_redemptions` | One learner's use of one campaign against exactly one booking (`UNIQUE(booking_id)`). Lifecycle: `reserved → applied \| released`. |
| `community_posts` | Forum / player-recruitment posts, independent of the legacy `posts` table. |
| `community_post_media` | Image/video attachments (max 8 per post, max 1 video — enforced in the service layer). |
| `community_comments` | Comments + one-level replies (`parent_comment_id`, self-referencing FK, `Restrict` delete). |
| `community_post_reactions` | Likes, `PK(post_id, user_id)`. |
| `community_post_applications` | "Xin tham gia" requests for recruitment posts, `UNIQUE(post_id, applicant_id)`. |
| `user_blocks` | One user blocking another, `PK(blocker_id, blocked_user_id)`. |

`bookings` gained `original_amount`, `discount_amount`, `voucher_campaign_id`, `voucher_code_snapshot`, `voucher_discount_type_snapshot`, `voucher_discount_value_snapshot`, `voucher_max_discount_amount_snapshot` — existing rows were safely backfilled (`original_amount = total_amount`, `discount_amount = 0`, no voucher) by the migration itself, verified against production with zero data loss. `chat_rooms` gained `status` (backfilled to `'active'` for all existing rows — no existing conversation was interrupted), `requested_by_user_id`, `requested_at`, `accepted_at`, `rejected_at`, `last_message_at`, `source_type`, `source_id`. `reports.target_type` gained three new allowed values: `community_post`, `community_comment`, `chat_message` (no schema change — `Report` already supported polymorphic targets for reviews).

See [`docs/api/vouchers.md`](api/vouchers.md) and [`docs/api/community.md`](api/community.md) for the full API surface.

### Apply migrations

```bash
dotnet ef database update --project src/SporticoApp.Infrastructure --startup-project src/SporticoApp.Api
```

### Add a migration

```bash
dotnet ef migrations add <Name> --project src/SporticoApp.Infrastructure --startup-project src/SporticoApp.Api
```

The design-time factory is [AppDbContextFactory](../src/SporticoApp.Infrastructure/Persistence/Context/AppDbContextFactory.cs); it loads `.env` from `../SporticoApp.Api/.env` and reads `appsettings.json` to find `ConnectionStrings:Default`. Ensure that connection string is set before running EF commands.

## Supabase Compatibility

Supabase is managed PostgreSQL, so the schema is compatible.

- Use a **direct connection string** (not the pooled PgBouncer endpoint) when running `dotnet ef database update`, because migrations use prepared statements / DDL that the transaction pooler can reject.
- Set `SslMode=Require` (and `Trust Server Certificate=true` if needed) in the connection string.
- **Never commit database secrets.** Provide the connection string via environment variable `ConnectionStrings__Default` or a local `.env` (git-ignored). See [13 — Environment Variables](13-environment-variables.md) and [deployment/supabase-postgres.md](deployment/supabase-postgres.md).
