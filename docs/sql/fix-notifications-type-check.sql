-- ============================================================================
-- Fix: widen notifications.type CHECK constraint to all current backend types
-- ============================================================================
-- Run against the configured PostgreSQL database (the one referenced by
-- ConnectionStrings:Default / ConnectionStrings__Default) using any SQL console
-- for that provider: Supabase SQL Editor, pgAdmin, psql, etc.
--
-- Root cause: the existing constraint chk_notifications_type only allowed the
-- legacy types (message, review, follow, payment, package, system, report).
-- The current backend also inserts: post, booking, training_package,
-- training_session, training_plan, wallet. Those inserts are rejected by the
-- CHECK constraint, which (because notifications are created AFTER the main
-- mutation commits) surfaces as HTTP 500 / COMMON_INTERNAL_SERVER_ERROR even
-- though the booking/session change was already saved.
--
-- This is equivalent to the EF migration
-- 20260601162903_UpdateNotificationTypeCheckConstraint. Apply EITHER the
-- migration OR this script — not both is required, but both are safe (idempotent).
-- ============================================================================

BEGIN;

ALTER TABLE notifications DROP CONSTRAINT IF EXISTS chk_notifications_type;
ALTER TABLE notifications DROP CONSTRAINT IF EXISTS notifications_type_check;

ALTER TABLE notifications ADD CONSTRAINT chk_notifications_type
CHECK (
  type IN (
    'message',
    'review',
    'follow',
    'payment',
    'package',
    'post',
    'system',
    'report',
    'booking',
    'training_package',
    'training_session',
    'training_plan',
    'wallet'
  )
);

COMMIT;

-- ----------------------------------------------------------------------------
-- Verification: confirm the new definition is in place.
-- ----------------------------------------------------------------------------
SELECT conname, pg_get_constraintdef(c.oid)
FROM pg_constraint c
JOIN pg_class t ON c.conrelid = t.oid
WHERE t.relname = 'notifications';
