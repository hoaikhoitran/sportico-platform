# 01 — Project Overview

## What Sportico Is

Sportico is a coaching marketplace that connects sports/fitness **coaches** with **learners**. A coach publishes paid training packages; a learner buys one, which creates a **booking**. Inside an active booking the two parties schedule training sessions, exchange messages, and the coach builds a personalized training plan and tracks the learner's progress.

The platform earns revenue by taking a fixed **15% commission** on each purchase. The coach's share is paid out **progressively**, one slice per completed session, into an internal **coach wallet** that the coach can later withdraw.

## Main Actors

| Actor | Role string | Description |
|---|---|---|
| **Admin** | `admin` | Moderates training packages, verifies coach payout accounts, approves/marks-paid withdrawal requests, manages sports. |
| **Coach** | `coach` | Creates training packages, manages bookings, confirms/completes sessions, builds training plans, manages wallet and withdrawals. |
| **Learner** | `learner` | Default role on registration. Browses packages, purchases bookings, requests sessions, fills assessments, logs progress check-ins. |

A user may hold more than one role (roles are a many-to-many via `user_roles`). Registering through `/api/auth/register` grants the `learner` role. Calling `/api/coaches/register` additionally grants the `coach` role and creates a coach profile.

## Business Model (Current)

1. A coach creates a **TrainingPackage** (`pending`).
2. An admin **approves** it (→ `published`) or **rejects** it (→ `rejected`).
3. A learner **purchases** a published package. This creates a **Booking** that snapshots price and commission.
4. The platform keeps **15%**; the remaining 85% is the coach's earnable amount, divided evenly across the package's sessions.
5. The learner **requests** training sessions against the active booking; the coach **confirms** them.
6. When the coach **completes** a session, the coach wallet is credited the per-session amount.
7. The coach sets a **payout account** (admin verifies it) and submits **withdrawal requests**; the admin approves and marks them paid.
8. Throughout the booking, the learner can fill a **LearnerAssessment**, the coach builds a **TrainingPlan** (weeks → days → exercises), the learner submits **ProgressCheckIns**, and the coach replies with feedback.
9. **Chat** is enabled only between a learner and coach who share an active or completed booking.
10. **Notifications** are created for the key events above.

### Worked Commission Example

For a package priced at 1,000,000 with 8 sessions:

| Field | Value |
|---|---|
| `TotalAmount` | 1,000,000 |
| `PlatformFeeRate` | 0.15 |
| `PlatformFeeAmount` | 150,000 |
| `CoachReceiveAmount` | 850,000 |
| `TotalSessions` | 8 |
| `PerSessionCoachAmount` | 106,250 |

The coach receives 106,250 each time a session is completed; after 8 completions the booking is marked `completed` and the coach has earned the full 850,000.

## Main Modules

| Module | Status | Key entities |
|---|---|---|
| Auth & users | Current | `User`, `Role`, `UserRole`, `RefreshToken` |
| Coach / learner profiles | Current | `CoachProfile`, `LearnerProfile`, `CoachSport`, `Sport` |
| Training packages | Current | `TrainingPackage` |
| Bookings | Current | `Booking` |
| Training sessions | Current | `TrainingSession` |
| Personalized training | Current | `LearnerAssessment`, `TrainingPlan`, `TrainingPlanWeek`, `TrainingPlanDay`, `TrainingPlanExercise`, `ProgressCheckIn` |
| Payments | Current | `Payment`, `PaymentTransaction` (PayOS + manual) |
| Coach wallet & payouts | Current | `CoachWallet`, `CoachWalletTransaction`, `CoachPayoutAccount`, `WithdrawalRequest` |
| Chat | Current | `ChatRoom`, `Message`, `MessageAttachment` |
| Notifications | Current | `Notification` |
| Coach posting / subscriptions | **Legacy** | `Package`, `CoachPackage`, `Post`, `PostImage`, `VPublishedPost` |

## Legacy vs Current

The platform originally used a **coach-posting subscription** model: coaches bought a `Package` (subscription with a post quota), which created a `CoachPackage`, and then published `Post`s advertising their services.

That model is **legacy**. The code, entities, controllers, and tables still exist, but the live business model is the **TrainingPackage + Booking** marketplace described above. Do not build new features on the legacy modules. See [17 — Legacy Modules](17-legacy-modules.md).
