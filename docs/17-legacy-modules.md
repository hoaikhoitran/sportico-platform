# 17 — Legacy Modules

These modules belong to the **original** business model: a coach-posting subscription system. They are superseded by the **TrainingPackage + Booking** marketplace ([01 — Project Overview](01-project-overview.md), [07 — Business Flows](07-business-flows.md)). They remain in the codebase but should be treated as legacy.

## What Is Legacy

| Type | Kind | Original purpose |
|---|---|---|
| `Package` (`int` key) | Entity / table `packages` | Coach subscription tier; defines `DurationDays`, `MaxPosts` quota, `Price`. |
| `CoachPackage` | Entity / table `coach_packages` | A coach's purchased subscription instance with `RemainingPosts`, `StartDate`/`EndDate`, status `pending\|active\|expired\|cancelled`. |
| `Post` / `PostImage` | Entities / tables | Coach service advertisement and its images; status `draft\|pending\|published\|archived\|rejected`. |
| `VPublishedPost` | DB view | Read model of published posts joined with coach + sport. |
| `VCoach` | DB view | Read model of coaches. |

### Related legacy API surface
- `PackagesController` (`/api/packages`) — public list/detail; admin create/update/status.
- `CoachPackagesController` (`/api/coach-packages`) — coach current/history, purchase (manual + PayOS).
- `PostsController` (`/api/posts`) — coach CRUD/archive of posts.
- `AdminPostsController` (`/api/admin/posts`) — admin pending list, approve, reject.

### Related legacy services / constants
- `PackageService`, `CoachPackageService`, `PostService`, `AdminPostService`.
- `PostStatusConstants`, `CoachPackageStatusConstants`, and in `PaymentConstants` the `CoachPackageStatuses` and `PaymentReferenceTypes.CoachPackage`.
- `ErrorCodes` entries: `ACTIVE_PACKAGE_REQUIRED`, `POST_QUOTA_EXCEEDED`, `COACH_PACKAGE_*`, `PACKAGE_*`, `POST_*`.

## Why It's Still Here

- The entities, controllers, services, and tables remain wired and compile.
- Some are still referenced by navigation properties (`CoachProfile.CoachPackages`, `CoachProfile.Posts`) and the payment `ReferenceType` polymorphism.
- Removing them is a deliberate cleanup, not an incidental change — do it as its own task with team sign-off.

## Rules Going Forward

1. **Do not build new features** on `Package`, `CoachPackage`, or `Post`.
2. New monetization and coach-offering work goes through `TrainingPackage` + `Booking`.
3. If you must touch legacy code (e.g. a security fix), keep changes minimal and note them as legacy.

## Future Cleanup Plan

When the team decides to retire the legacy model:

1. **Confirm no production dependency** — verify no live data or frontend depends on packages/posts.
2. **Remove unused APIs** — delete the legacy controllers and their service registrations.
3. **Remove navigation references** — drop `CoachPackages`/`Posts` collections from `CoachProfile` and any mappings.
4. **Back up, then drop tables** — after a database backup, drop `packages`, `coach_packages`, `posts`, `post_images`, and the `v_published_post` / `v_coach` views via a migration.
5. **Reset migrations for a clean schema (optional)** — if the project has **no production data**, consider squashing migrations into a single clean baseline reflecting the final marketplace schema. Only do this when there is nothing to preserve, and coordinate so every environment re-baselines together.

> NOTE: Until the cleanup is approved, keep the legacy modules compiling and migrated so existing environments continue to work.
