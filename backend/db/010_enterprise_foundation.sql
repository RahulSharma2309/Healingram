-- Enterprise foundation: multi-role, OTP challenges, audit, payment ownership.
-- Idempotent. Safe to re-run. No cross-schema foreign keys.

CREATE SCHEMA IF NOT EXISTS audit;

CREATE TABLE IF NOT EXISTS identity.user_roles (
    user_id     uuid NOT NULL,
    role        text NOT NULL CHECK (role IN ('customer', 'partner', 'admin')),
    created_at  timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (user_id, role)
);

INSERT INTO identity.user_roles (user_id, role)
SELECT id, role FROM identity.users
ON CONFLICT DO NOTHING;

CREATE TABLE IF NOT EXISTS identity.admin_permissions (
    user_id     uuid NOT NULL,
    permission  text NOT NULL,
    created_at  timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (user_id, permission)
);

INSERT INTO identity.admin_permissions (user_id, permission)
SELECT u.id, p.permission
FROM identity.users u
CROSS JOIN (VALUES
    ('requests.read'),
    ('requests.manage'),
    ('vendors.read'),
    ('vendors.manage'),
    ('catalog.read'),
    ('catalog.manage'),
    ('bookings.read'),
    ('bookings.manage'),
    ('payments.read'),
    ('refunds.manage'),
    ('users.read'),
    ('users.manage'),
    ('audit.read')
) AS p(permission)
WHERE u.role = 'admin'
ON CONFLICT DO NOTHING;

CREATE TABLE IF NOT EXISTS identity.otp_challenges (
    id                  uuid PRIMARY KEY,
    user_id             uuid,
    public_id           text,
    destination         text NOT NULL,
    channel             text NOT NULL,
    purpose             text NOT NULL,
    code_hash           text NOT NULL,
    expires_at          timestamptz NOT NULL,
    attempts            int NOT NULL DEFAULT 0,
    max_attempts        int NOT NULL DEFAULT 5,
    consumed_at         timestamptz,
    provider            text NOT NULL,
    provider_reference  text,
    created_at          timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS otp_challenges_lookup_idx
    ON identity.otp_challenges (destination, purpose, created_at DESC);

CREATE TABLE IF NOT EXISTS audit.events (
    id              uuid PRIMARY KEY,
    actor_id        uuid,
    actor_role      text,
    action          text NOT NULL,
    entity_type     text NOT NULL,
    entity_id       text,
    correlation_id  text,
    metadata        jsonb NOT NULL DEFAULT '{}',
    occurred_at     timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS audit_events_entity_idx
    ON audit.events (entity_type, entity_id, occurred_at DESC);

ALTER TABLE payment.intents
    ADD COLUMN IF NOT EXISTS customer_user_id uuid;

ALTER TABLE partners.partner_users
    ADD COLUMN IF NOT EXISTS membership_role text NOT NULL DEFAULT 'member';
