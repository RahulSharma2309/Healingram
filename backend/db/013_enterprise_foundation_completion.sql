-- Enterprise foundation completion: request DTO columns, inventory linkage,
-- booking lifecycle, content/navigation, pagination indexes, phone uniqueness.
-- Idempotent. Safe to apply once via schema_migrations.

ALTER TABLE availability.requests
    ADD COLUMN IF NOT EXISTS inventory_hold_id uuid,
    ADD COLUMN IF NOT EXISTS booking_number text;

CREATE INDEX IF NOT EXISTS availability_requests_customer_idx
    ON availability.requests (customer_user_id, requested_at DESC);

CREATE INDEX IF NOT EXISTS availability_requests_status_idx
    ON availability.requests (status, requested_at DESC);

CREATE INDEX IF NOT EXISTS inventory_holds_request_idx
    ON inventory.holds (request_public_id);

CREATE UNIQUE INDEX IF NOT EXISTS users_phone_e164_uidx
    ON identity.users (phone_e164)
    WHERE phone_e164 IS NOT NULL AND length(btrim(phone_e164)) > 0;

CREATE UNIQUE INDEX IF NOT EXISTS credentials_user_uidx
    ON identity.credentials (user_id);

ALTER TABLE booking.bookings
    DROP CONSTRAINT IF EXISTS bookings_status_check;

ALTER TABLE booking.bookings
    ADD CONSTRAINT bookings_status_check
    CHECK (status IN (
        'awaiting_payment', 'paid', 'completed', 'cancelled', 'refund_pending', 'refunded'));

CREATE TABLE IF NOT EXISTS content.sections (
    slug        text PRIMARY KEY,
    surface     text NOT NULL,
    title       text,
    body        text,
    image_url   text,
    cta_label   text,
    cta_href    text,
    payload     jsonb NOT NULL DEFAULT '{}'::jsonb,
    sort_order  int NOT NULL DEFAULT 0,
    active      boolean NOT NULL DEFAULT true
);

CREATE TABLE IF NOT EXISTS content.navigation_menus (
    menu_key    text PRIMARY KEY,
    label       text NOT NULL
);

CREATE TABLE IF NOT EXISTS content.navigation_items (
    id          uuid PRIMARY KEY,
    menu_key    text NOT NULL REFERENCES content.navigation_menus (menu_key) ON DELETE CASCADE,
    label       text NOT NULL,
    href        text NOT NULL,
    sort_order  int NOT NULL DEFAULT 0,
    parent_key  text,
    active      boolean NOT NULL DEFAULT true
);

CREATE INDEX IF NOT EXISTS content_navigation_items_menu_idx
    ON content.navigation_items (menu_key, sort_order);
