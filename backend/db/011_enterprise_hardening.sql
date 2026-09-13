-- Enterprise hardening: migration ledger, concurrency, payment lifecycle,
-- webhook processing, scoped refresh tokens. Idempotent. Safe to re-run.

CREATE TABLE IF NOT EXISTS public.schema_migrations (
    id          text PRIMARY KEY,
    version     text NOT NULL,
    applied_at  timestamptz NOT NULL DEFAULT now()
);

ALTER TABLE identity.refresh_tokens
    ADD COLUMN IF NOT EXISTS auth_kind text,
    ADD COLUMN IF NOT EXISTS purpose text,
    ADD COLUMN IF NOT EXISTS request_id text;

ALTER TABLE payment.webhook_events
    ADD COLUMN IF NOT EXISTS processing_status text NOT NULL DEFAULT 'received',
    ADD COLUMN IF NOT EXISTS processed_at timestamptz,
    ADD COLUMN IF NOT EXISTS last_error text;

CREATE UNIQUE INDEX IF NOT EXISTS payment_intents_one_open_per_booking_uidx
    ON payment.intents (booking_id)
    WHERE status IN ('creating', 'ready');

INSERT INTO identity.admin_permissions (user_id, permission)
SELECT u.id, 'payments.simulate'
FROM identity.users u
WHERE u.role = 'admin'
ON CONFLICT DO NOTHING;
