-- Backend-driven application: catalog metadata, matching options, content,
-- notifications inbox, lead options, inventory holds, price quotes.
-- Idempotent. Safe to apply once via schema_migrations.

ALTER TABLE catalog.needs
    ADD COLUMN IF NOT EXISTS description text,
    ADD COLUMN IF NOT EXISTS image_url text,
    ADD COLUMN IF NOT EXISTS icon_key text,
    ADD COLUMN IF NOT EXISTS sort_order int NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS kind text NOT NULL DEFAULT 'need',
    ADD COLUMN IF NOT EXISTS active boolean NOT NULL DEFAULT true;

ALTER TABLE catalog.destinations
    ADD COLUMN IF NOT EXISTS description text,
    ADD COLUMN IF NOT EXISTS image_url text,
    ADD COLUMN IF NOT EXISTS sort_order int NOT NULL DEFAULT 0;

ALTER TABLE catalog.programmes
    ADD COLUMN IF NOT EXISTS description text,
    ADD COLUMN IF NOT EXISTS best_for text;

ALTER TABLE catalog.experts
    ADD COLUMN IF NOT EXISTS bio text,
    ADD COLUMN IF NOT EXISTS image_url text;

ALTER TABLE catalog.rooms
    ADD COLUMN IF NOT EXISTS description text;

ALTER TABLE catalog.retreats
    DROP CONSTRAINT IF EXISTS retreats_status_check;

ALTER TABLE catalog.retreats
    ADD CONSTRAINT retreats_status_check
    CHECK (status IN (
        'draft', 'pending_review', 'approved', 'active', 'published', 'suspended', 'archived'));

CREATE TABLE IF NOT EXISTS catalog.themes (
    slug        text PRIMARY KEY,
    label       text NOT NULL,
    sort_order  int NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS catalog.discovery_cards (
    slug        text PRIMARY KEY,
    surface     text NOT NULL,
    label       text NOT NULL,
    description text,
    image_url   text,
    icon_key    text,
    href        text,
    sort_order  int NOT NULL DEFAULT 0,
    active      boolean NOT NULL DEFAULT true
);

CREATE TABLE IF NOT EXISTS catalog.retreat_media (
    id          uuid PRIMARY KEY,
    retreat_id  uuid NOT NULL,
    url         text NOT NULL,
    alt         text,
    category    text NOT NULL,
    sort_order  int NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS catalog.retreat_sections (
    retreat_id  uuid NOT NULL,
    kind        text NOT NULL,
    payload     jsonb NOT NULL,
    PRIMARY KEY (retreat_id, kind)
);

CREATE TABLE IF NOT EXISTS catalog.price_quotes (
    id                uuid PRIMARY KEY,
    retreat_slug      text NOT NULL,
    programme_slug    text NOT NULL,
    duration_nights   int NOT NULL,
    occupancy         text NOT NULL,
    guests            int NOT NULL,
    currency          text NOT NULL DEFAULT 'INR',
    base_amount       numeric(12,2),
    tax_amount        numeric(12,2),
    total_amount      numeric(12,2),
    price_status      text NOT NULL,
    pricing_version   text NOT NULL DEFAULT '1',
    snapshot          jsonb NOT NULL,
    created_at        timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS matching.questions (
    question_key    text PRIMARY KEY,
    label           text NOT NULL,
    selection_mode  text NOT NULL CHECK (selection_mode IN ('multi', 'single')),
    sort_order      int NOT NULL,
    active          boolean NOT NULL DEFAULT true
);

CREATE TABLE IF NOT EXISTS matching.question_options (
    id              uuid PRIMARY KEY,
    question_key    text NOT NULL REFERENCES matching.questions (question_key),
    option_key      text NOT NULL,
    label           text NOT NULL,
    description     text,
    icon_key        text,
    sort_order      int NOT NULL,
    active          boolean NOT NULL DEFAULT true,
    theme_slugs     text[] NOT NULL DEFAULT '{}',
    UNIQUE (question_key, option_key)
);

CREATE SCHEMA IF NOT EXISTS content;

CREATE TABLE IF NOT EXISTS content.pages (
    slug            text PRIMARY KEY,
    title           text NOT NULL,
    body            text NOT NULL,
    status          text NOT NULL CHECK (status IN ('draft', 'published', 'archived')),
    kind            text NOT NULL DEFAULT 'page',
    sort_order      int NOT NULL DEFAULT 0,
    published_at    timestamptz,
    updated_at      timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS notifications.inbox (
    id            uuid PRIMARY KEY,
    user_id       uuid NOT NULL,
    kind          text NOT NULL,
    title         text NOT NULL,
    body          text NOT NULL,
    entity_type   text,
    entity_id     text,
    read_at       timestamptz,
    created_at    timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS notifications_inbox_user_idx
    ON notifications.inbox (user_id, created_at DESC);

CREATE TABLE IF NOT EXISTS leads.options (
    kind        text NOT NULL,
    option_key  text NOT NULL,
    label       text NOT NULL,
    sort_order  int NOT NULL,
    active      boolean NOT NULL DEFAULT true,
    PRIMARY KEY (kind, option_key)
);

CREATE SCHEMA IF NOT EXISTS platform;

CREATE TABLE IF NOT EXISTS platform.settings (
    key     text PRIMARY KEY,
    value   text NOT NULL
);

CREATE SCHEMA IF NOT EXISTS inventory;

CREATE TABLE IF NOT EXISTS inventory.holds (
    id                  uuid PRIMARY KEY,
    request_public_id   text,
    retreat_slug        text NOT NULL,
    programme_slug      text,
    status              text NOT NULL CHECK (status IN ('held', 'released', 'confirmed')),
    created_at          timestamptz NOT NULL DEFAULT now()
);
