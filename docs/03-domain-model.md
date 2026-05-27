# 03 — Domain Model

Entities live in `SporticoApp.Core/Entities`. This document summarizes the entities that matter to the current business model, their key fields, relationships, and rules. Field types reflect the C# entity; storage details (precision, snake_case) are in [04 — Database Schema](04-database-schema.md).

Primary keys are `Guid` unless noted. `Role`, `Sport`, and `Package` use `int` keys.

## Identity & Profiles

### User
Core account record.
- **Key fields**: `Id`, `Email`, `PasswordHash`, `FullName`, `Phone?`, `AvatarUrl?`, `Status` (`active | inactive | banned | pending`), `EmailVerificationToken?`, `RefreshToken?`, `RefreshTokenExpiresAt?`.
- **Relationships**: optional `CoachProfile`, optional `LearnerProfile`, many `UserRoles`; owns bookings as learner, notifications, payments, messages, etc.
- **Rules**: created `inactive`; becomes `active` only after email verification. Login is blocked unless `active`.

### Role / UserRole
- `Role` (`int Id`, `Name`) — values `learner`, `coach`, `admin`.
- `UserRole` — join table (`UserId`, `RoleId`). A user can hold multiple roles.

### CoachProfile
Profile for a user acting as a coach. Primary key is `UserId` (1:1 with `User`).
- **Key fields**: `Bio?`, `ExperienceYears?`, `Headline?`, `Rating` (cached average), `TotalReviews` (cached).
- **Relationships**: many `TrainingPackages`, `Bookings`, `TrainingSessions`, `CoachSports`; one optional `Wallet`, one optional `PayoutAccount`, many `WithdrawalRequests`. Also legacy `CoachPackages`, `Posts`.
- **Rules**: created via `/api/coaches/register`, which also grants the `coach` role. One profile per user.

### LearnerProfile
- **Key fields**: `UserId` (PK, 1:1 with `User`), `Goal?`.
- > NOTE: There is no learner-registration endpoint in the API surface reviewed; the `learner` role is granted at user registration. `LearnerProfile` rows are not created by the auth flow examined.

### Sport
Catalog of sports/disciplines (`int Id`).
- **Key fields**: `Name`, `Slug` (URL-friendly), `Description?`, `IconUrl?`, `IsActive`.
- **Relationships**: `CoachSports`, used by `TrainingPackage.SportId` and legacy `Post.SportId`.
- **Rules**: only `IsActive` sports can be selected when registering a coach.

### CoachSport
Join table linking a `CoachProfile` to the `Sport`s they coach.

## Marketplace Core

### TrainingPackage
A paid offering a coach sells to learners.
- **Key fields**: `Id`, `CoachId`, `SportId`, `Title`, `Description?`, `Price`, `SessionCount`, `DurationDays`, `Location?`, `IsOnline`, `Level?`, `GoalType?`, `Status` (`pending | published | rejected | archived`), `RejectionReason?`, `ReviewedByUserId?`, `ReviewedAt?`.
- **Relationships**: belongs to `CoachProfile` and `Sport`; has many `Bookings`.
- **Rules**: created as `pending`. Admin approves → `published` (only published packages are publicly listable and purchasable) or rejects → `rejected` (with reason). Coach can archive → `archived`.

### Booking
Created when a learner purchases a training package. **Snapshots** the commission math so later package edits don't change a paid booking.
- **Key fields**: `Id`, `LearnerId`, `CoachId`, `TrainingPackageId`, `TotalAmount`, `PlatformFeeRate`, `PlatformFeeAmount`, `CoachReceiveAmount`, `PerSessionCoachAmount`, `TotalSessions`, `CompletedSessions`, `Status` (`pending_payment | active | completed | cancelled | refunded`), `PaidAt?`, `CompletedAt?`, `CancelledAt?`.
- **Relationships**: belongs to `Learner` (User), `Coach` (CoachProfile), `TrainingPackage`; has many `TrainingSessions`; optional `LearnerAssessment` and `TrainingPlan`.
- **Rules**: manual purchase is created `active` immediately; PayOS purchase is created `pending_payment` and activated by the webhook. Activation ensures a coach wallet and a chat room exist. A booking becomes `completed` when `CompletedSessions >= TotalSessions`.

### TrainingSession
A scheduled session inside a booking.
- **Key fields**: `Id`, `BookingId`, `LearnerId`, `CoachId`, `StartTime`, `EndTime`, `Status` (`requested | scheduled | completed | cancelled | missed`), `MeetingUrl?`, `Location?`, `LearnerNote?`, `CoachNote?`, `ConfirmedAt?`, `CompletedAt?`, `CancelledAt?`.
- **Rules**:
  - Only the learner can create (request) a session, and only on an `active` booking with a future `StartTime`.
  - The number of sessions in `requested + scheduled + completed` state cannot exceed `Booking.TotalSessions`.
  - Overlap is rejected for both coach and learner against existing `requested`/`scheduled` sessions (schedule conflict).
  - Coach confirms `requested` → `scheduled`. Either party cancels a `requested`/`scheduled` session. Coach completes a `scheduled` session → `completed`, which credits the wallet.

## Personalized Training

### LearnerAssessment
Intake form per booking, captured for personalization (1:1 with `Booking`).
- **Key fields**: `GoalType`, `GoalDescription?`, `HeightCm?`, `WeightKg?`, `BodyFatPercent?`, `CurrentLevel?`, `HealthNotes?`, `InjuryNotes?`, `TrainingHistory?`, `AvailableDaysPerWeek?`, `PreferredSessionDurationMinutes?`, `EquipmentAvailable?`.
- **Rules**: created/updated by the learner; readable by both parties on the booking.

### TrainingPlan → Week → Day → Exercise
The coach-authored program for a booking. A four-level hierarchy.
- **TrainingPlan**: `BookingId` (1:1), `Title`, `GoalType`, `Overview?`, `StartDate`, `EndDate`, `TotalWeeks`, `Status` (`draft | active | completed | cancelled`).
- **TrainingPlanWeek**: `TrainingPlanId`, `WeekNumber`, `Focus?`, `Notes?`.
- **TrainingPlanDay**: `TrainingPlanWeekId`, `DayNumber`, `Title`, `Notes?`.
- **TrainingPlanExercise**: `TrainingPlanDayId`, `ExerciseName`, `OrderIndex`, `Sets?`, `Reps?`, `Intensity?`, `RestSeconds?`, `Notes?`.
- **Rules**: only the coach owning the booking can create/update plan structure and exercises.

### ProgressCheckIn
Periodic learner-submitted progress entry with optional coach feedback.
- **Key fields**: `BookingId`, `LearnerId`, `CoachId`, `CheckInDate`, `WeightKg?`, `BodyFatPercent?`, `WaistCm?`, `EnergyLevel?`, `SleepQuality?`, `LearnerNote?`, `CoachFeedback?`.
- **Rules**: learner creates; coach adds `CoachFeedback`.

## Money: Wallet & Payouts

### CoachWallet
Internal balance per coach (1:1 with `CoachProfile`).
- **Key fields**: `CoachId`, `AvailableBalance`, `PendingBalance`, `TotalEarned`, `TotalWithdrawn`.
- **Rules**: created when a booking is activated, or lazily when the first session is completed. Session completion credits `AvailableBalance` and `TotalEarned`. A withdrawal moves funds `Available → Pending`; marking paid moves `Pending → TotalWithdrawn`; rejecting returns `Pending → Available`.

### CoachWalletTransaction
Append-only ledger entry.
- **Key fields**: `CoachWalletId`, `CoachId`, `Type` (`session_release | withdrawal | adjustment`), `Direction` (`credit | debit`), `Amount`, `ReferenceType?`, `ReferenceId?`, `Note?`.
- **Rules**: a `credit`/`session_release` is written on session completion (`ReferenceType = training_session`); a `debit`/`withdrawal` is written when a withdrawal is marked paid (`ReferenceType = withdrawal_request`).

### CoachPayoutAccount
Bank details for paying a coach out (1:1 with `CoachProfile`).
- **Key fields**: `PayoutMethod`, `BankName`, `BankAccountNumber`, `BankAccountHolder`, `Status` (`pending | verified | rejected`).
- **Rules**: upserted by the coach (resets to `pending`); verified or rejected by an admin. A **verified** account is required to create a withdrawal.

### WithdrawalRequest
A coach's request to withdraw available balance.
- **Key fields**: `CoachId`, `CoachWalletId`, `CoachPayoutAccountId?`, `Amount`, `Status` (`pending | approved | rejected | paid | cancelled`), `AdminNote?`, `ReviewedByUserId?`, `ReviewedAt?`.
- **Rules**: requires a verified payout account and sufficient `AvailableBalance`. Creation moves the amount to `PendingBalance`. Admin approves, marks paid (writes a debit ledger entry, increases `TotalWithdrawn`), or rejects (returns funds to `AvailableBalance`).

## Payments

### Payment
A payment record for a purchase (manual or PayOS).
- **Key fields**: `UserId`, `Amount`, `Method` (`manual | payos`), `ReferenceType` (`booking | coach_package`), `ReferenceId?`, `Status` (`pending | paid | failed | cancelled`), `TransactionCode?`, `OrderCode?` (unique when present), `PaymentLinkId?`, `CheckoutUrl?`, `ExpiredAt?`, `PaidAt?`.
- **Rules**: PayOS bookings create a `pending` payment with `OrderCode`/`CheckoutUrl`; the webhook flips it to `paid`/`failed`/`cancelled` and drives the booking status.

### PaymentTransaction
Raw gateway callback log linked to a `Payment` (stores the serialized webhook payload).

## Communication

### ChatRoom / Message / MessageAttachment
- `ChatRoom` — 1:1 conversation between `User1Id` and `User2Id` (stored ordered by GUID).
- `Message` — `RoomId`, `SenderId`, `Content`, `IsRead`, `SentAt`.
- `MessageAttachment` — optional attachments on a message.
- **Rules**: a room is created when a booking activates. Reading/sending requires the user to be a participant **and** to share an active or completed booking with the other participant.

### Notification
Per-user notification.
- **Key fields**: `UserId`, `Title`, `Content?`, `Type` (e.g. `booking`, `training_session`, `wallet`, `training_package`, `payment`, …), `IsRead`, `CreatedAt`.

## Legacy Entities (still present)

| Entity | Original purpose |
|---|---|
| `Package` (`int`) | Coach subscription tier with `MaxPosts` quota. |
| `CoachPackage` | A coach's purchased subscription with `RemainingPosts`. |
| `Post` / `PostImage` | Coach service advertisement and its images. |
| `VPublishedPost` | Read-only DB view of published posts joined with coach/sport. |
| `VCoach` | Read-only DB view of coaches. |
| `Follow`, `Review`, `Report` | Social/moderation entities; present but peripheral to the current flow. |

See [17 — Legacy Modules](17-legacy-modules.md). Do not extend the legacy entities for new work.
