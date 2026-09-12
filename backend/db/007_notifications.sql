-- Healingram notifications outbox (idempotent). Safe to re-run.
-- No cross-schema foreign keys. Payload holds public ids only (no email/phone/name).

CREATE SCHEMA IF NOT EXISTS notifications;

CREATE TABLE IF NOT EXISTS notifications.outbox (
    id               uuid PRIMARY KEY,
    kind             text NOT NULL,
    idempotency_key  text NOT NULL UNIQUE,
    payload          jsonb NOT NULL,
    status           text NOT NULL CHECK (status IN ('pending', 'sent', 'failed')),
    created_at       timestamptz NOT NULL DEFAULT now(),
    sent_at          timestamptz
);

CREATE INDEX IF NOT EXISTS outbox_pending_created_idx
    ON notifications.outbox (status, created_at)
    WHERE status = 'pending';
