# Deployment — Migration Strategy

## Principles

- Migrations live in `src/SporticoApp.Infrastructure/Migrations` and **are committed** to git (the `.gitignore` deliberately keeps them). Schema changes are shared through source control, not regenerated per environment.
- Every environment is brought to the same schema by applying the same committed migrations.

## Creating a migration

```bash
dotnet ef migrations add <DescriptiveName> \
  --project src/SporticoApp.Infrastructure \
  --startup-project src/SporticoApp.Api
```

Review the generated `Up`/`Down` and the snapshot diff before committing. Commit the migration with the code that needs it.

## Applying migrations

```bash
dotnet ef database update \
  --project src/SporticoApp.Infrastructure \
  --startup-project src/SporticoApp.Api
```

- Run against the **direct** Postgres endpoint (not a transaction pooler) — see [supabase-postgres.md](supabase-postgres.md).
- The design-time `AppDbContextFactory` loads `src/SporticoApp.Api/.env` and `appsettings.json` for the connection string.

## Deployment ordering

The CI pipeline ([ci-cd.md](ci-cd.md)) does **not** run migrations. Apply them deliberately:

- **Additive changes** (new tables/columns/indexes): apply migrations **before** deploying code that reads them, so the new code never hits a missing object.
- **Destructive changes** (drops/renames): coordinate carefully. Prefer the expand/contract pattern — deploy code that tolerates both shapes, migrate, then remove the old shape in a later migration.
- Always back up production before a destructive migration.

## Current migration history

| Migration | Purpose |
|---|---|
| `20260522175843_Baseline` | Initial baseline |
| `20260522180016_AddEmailVerificationTokenToUsers` | Email verification token |
| `20260522183932_RenameEmailVerificationTokenColumn` | Rename |
| `20260522185801_AddRefreshTokenFields` | Refresh token fields |
| `20260522190500_FixEmailVerificationTokenColumn` | Column fix |
| `20260526055807_AddPayOsFieldsToPayment` | PayOS fields on payments |
| `20260526092825_UpdatePaymentMethodConstraint` | Payment check constraints |
| `20260527034926_AddBookingTrainingFlow` | Booking + training/session/wallet/payout/personalization tables |

## When to reset migrations to a clean baseline

Only when the project has **no production data to preserve** (e.g. pre-launch). Squashing the history into a single clean baseline is useful after retiring the legacy modules ([legacy cleanup](#legacy-cleanup)) to leave a tidy final schema.

Procedure (no production data):
1. Drop the dev/staging database.
2. Delete the `Migrations` folder contents.
3. `dotnet ef migrations add InitialClean`.
4. `dotnet ef database update` to recreate.
5. Commit the new baseline; have every environment re-baseline together.

Do **not** do this once real data exists — you would lose the ability to migrate existing databases.

## Legacy cleanup
<a id="legacy-cleanup"></a>
Retiring `Package`/`CoachPackage`/`Post` (and the `v_published_post` / `v_coach` views) is a deliberate migration:
1. Confirm nothing live depends on them.
2. Remove the entities/controllers/services and navigation references.
3. Add a migration that drops the tables/views (after a backup).
4. Optionally squash to a clean baseline if there is no production data.

See [17 — Legacy Modules](../17-legacy-modules.md).
