-- Healingram V1 — one Postgres, one schema per module.
-- Safe to re-run (IF NOT EXISTS). No cross-schema foreign keys.

CREATE EXTENSION IF NOT EXISTS citext;

CREATE SCHEMA IF NOT EXISTS identity;
CREATE SCHEMA IF NOT EXISTS catalog;
CREATE SCHEMA IF NOT EXISTS matching;
CREATE SCHEMA IF NOT EXISTS availability;
CREATE SCHEMA IF NOT EXISTS booking;
CREATE SCHEMA IF NOT EXISTS payment;
CREATE SCHEMA IF NOT EXISTS leads;
CREATE SCHEMA IF NOT EXISTS partners;

CREATE TABLE IF NOT EXISTS identity.users (
    id            uuid PRIMARY KEY,
    email         citext,
    full_name     text,
    phone_e164    text,
    role          text NOT NULL CHECK (role IN ('customer', 'partner', 'admin')),
    status        text NOT NULL DEFAULT 'active' CHECK (status IN ('active', 'blocked')),
    created_at    timestamptz NOT NULL DEFAULT now(),
    updated_at    timestamptz NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX IF NOT EXISTS users_email_uidx ON identity.users (email) WHERE email IS NOT NULL;

CREATE TABLE IF NOT EXISTS identity.credentials (
    id              uuid PRIMARY KEY,
    user_id         uuid NOT NULL,
    password_hash   text NOT NULL,
    created_at      timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS identity.refresh_tokens (
    id            uuid PRIMARY KEY,
    user_id       uuid NOT NULL,
    token_hash    text NOT NULL,
    expires_at    timestamptz NOT NULL,
    revoked_at    timestamptz
);

CREATE TABLE IF NOT EXISTS catalog.needs (
    slug        text PRIMARY KEY,
    label       text NOT NULL
);

CREATE TABLE IF NOT EXISTS catalog.destinations (
    slug        text PRIMARY KEY,
    kind        text NOT NULL CHECK (kind IN ('state', 'region', 'locality')),
    parent_slug text,
    label       text NOT NULL
);

CREATE TABLE IF NOT EXISTS catalog.retreats (
    id                      uuid PRIMARY KEY,
    slug                    text NOT NULL UNIQUE,
    name                    text NOT NULL,
    status                  text NOT NULL CHECK (status IN ('draft', 'active', 'archived')),
    state_slug              text NOT NULL,
    region_slug             text,
    locality                text NOT NULL,
    identity_complete       boolean NOT NULL DEFAULT false,
    settlement_mode         text NOT NULL DEFAULT 'PARTNER_DIRECT'
                            CHECK (settlement_mode IN ('MARKETPLACE_SPLIT', 'PARTNER_DIRECT')),
    created_at              timestamptz NOT NULL DEFAULT now(),
    updated_at              timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS catalog.programmes (
    id                      uuid PRIMARY KEY,
    retreat_id              uuid NOT NULL,
    slug                    text NOT NULL,
    name                    text NOT NULL,
    supported_durations     int[] NOT NULL DEFAULT '{}',
    UNIQUE (retreat_id, slug)
);

CREATE TABLE IF NOT EXISTS catalog.programme_prices (
    id              uuid PRIMARY KEY,
    programme_id    uuid NOT NULL,
    occupancy       text NOT NULL,
    duration_nights int NOT NULL,
    amount_inr      numeric(12,2),
    status          text NOT NULL CHECK (status IN ('VERIFIED', 'ESTIMATED', 'ON_REQUEST')),
    valid_from      date,
    valid_to        date,
    version         int NOT NULL DEFAULT 1
);

CREATE TABLE IF NOT EXISTS catalog.rooms (
    id              uuid PRIMARY KEY,
    retreat_id      uuid NOT NULL,
    name            text NOT NULL,
    occupancy_max   int NOT NULL
);

CREATE TABLE IF NOT EXISTS catalog.experts (
    id              uuid PRIMARY KEY,
    retreat_id      uuid NOT NULL,
    name            text NOT NULL,
    verified        boolean NOT NULL DEFAULT false
);

CREATE TABLE IF NOT EXISTS catalog.testimonials (
    id              uuid PRIMARY KEY,
    retreat_id      uuid NOT NULL,
    consented       boolean NOT NULL DEFAULT false,
    verified        boolean NOT NULL DEFAULT false,
    body            text NOT NULL
);

CREATE TABLE IF NOT EXISTS catalog.inclusions (
    id              uuid PRIMARY KEY,
    programme_id    uuid NOT NULL,
    kind            text NOT NULL CHECK (kind IN ('included', 'excluded', 'conditional')),
    label           text NOT NULL
);

CREATE TABLE IF NOT EXISTS matching.match_sessions (
    id              uuid PRIMARY KEY,
    answer_slugs    jsonb NOT NULL,
    created_at      timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS availability.requests (
    id                      uuid PRIMARY KEY,
    public_id               text NOT NULL UNIQUE,
    customer_user_id        uuid,
    customer_name           text NOT NULL,
    retreat_id              uuid NOT NULL,
    programme_id            uuid NOT NULL,
    status                  text NOT NULL,
    price_snapshot          jsonb NOT NULL,
    idempotency_key         text NOT NULL UNIQUE,
    requested_at            timestamptz NOT NULL DEFAULT now(),
    partner_viewed_at       timestamptz,
    partner_responded_at    timestamptz
);

CREATE TABLE IF NOT EXISTS availability.status_history (
    id              uuid PRIMARY KEY,
    request_id      uuid NOT NULL,
    from_status     text,
    to_status       text NOT NULL,
    actor_role      text NOT NULL,
    actor_id        uuid,
    reason          text,
    occurred_at     timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS availability.alternatives (
    id              uuid PRIMARY KEY,
    request_id      uuid NOT NULL,
    proposal        jsonb NOT NULL,
    created_at      timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS booking.bookings (
    id              uuid PRIMARY KEY,
    booking_number  text NOT NULL UNIQUE,
    request_id      uuid NOT NULL,
    status          text NOT NULL,
    snapshot        jsonb NOT NULL,
    created_at      timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS booking.events (
    id              uuid PRIMARY KEY,
    booking_id      uuid NOT NULL,
    type            text NOT NULL,
    payload         jsonb NOT NULL DEFAULT '{}',
    created_at      timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS payment.intents (
    id                  uuid PRIMARY KEY,
    booking_id          uuid NOT NULL,
    provider            text NOT NULL,
    provider_ref        text,
    amount_inr          numeric(12,2) NOT NULL,
    currency            text NOT NULL DEFAULT 'INR',
    status              text NOT NULL,
    idempotency_key     text NOT NULL UNIQUE,
    created_at          timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS payment.webhook_events (
    id                  uuid PRIMARY KEY,
    provider            text NOT NULL,
    provider_event_id   text NOT NULL UNIQUE,
    payload             jsonb NOT NULL,
    received_at         timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS leads.expert_leads (
    id                  uuid PRIMARY KEY,
    full_name           text NOT NULL,
    phone_e164          text NOT NULL,
    email               text NOT NULL,
    whatsapp_consent    boolean NOT NULL DEFAULT false,
    source              text,
    status              text NOT NULL DEFAULT 'NEW',
    context             jsonb,
    created_at          timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS leads.status_history (
    id              uuid PRIMARY KEY,
    lead_id         uuid NOT NULL,
    from_status     text,
    to_status       text NOT NULL,
    actor_id        uuid,
    occurred_at     timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS partners.partners (
    id              uuid PRIMARY KEY,
    display_name    text NOT NULL,
    status          text NOT NULL CHECK (status IN ('pending', 'approved', 'rejected', 'suspended')),
    created_at      timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS partners.partner_users (
    partner_id      uuid NOT NULL,
    user_id         uuid NOT NULL,
    PRIMARY KEY (partner_id, user_id)
);
