-- Availability + booking operational columns (idempotent).
-- Applied after 001_schemas.sql. Safe to re-run. No cross-schema foreign keys.

ALTER TABLE availability.requests
    ADD COLUMN IF NOT EXISTS customer_email text NOT NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS customer_phone text NOT NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS retreat_slug text NOT NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS programme_slug text NOT NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS final_amount_inr numeric(12,2);

CREATE SEQUENCE IF NOT EXISTS availability.request_public_seq START WITH 10001;
CREATE SEQUENCE IF NOT EXISTS booking.booking_number_seq START WITH 10001;

CREATE TABLE IF NOT EXISTS availability.admin_notes (
    id          uuid PRIMARY KEY,
    request_id  uuid NOT NULL,
    body        text NOT NULL,
    actor_id    uuid,
    created_at  timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS requests_status_idx ON availability.requests (status);
CREATE INDEX IF NOT EXISTS admin_notes_request_idx ON availability.admin_notes (request_id);
CREATE INDEX IF NOT EXISTS alternatives_request_idx ON availability.alternatives (request_id);
CREATE INDEX IF NOT EXISTS status_history_request_idx ON availability.status_history (request_id);

CREATE UNIQUE INDEX IF NOT EXISTS bookings_request_id_uidx ON booking.bookings (request_id);
