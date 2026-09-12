-- Partner retreat mapping (idempotent). Safe to re-run. No cross-schema foreign keys.

CREATE TABLE IF NOT EXISTS partners.partner_retreats (
    partner_id    uuid NOT NULL,
    retreat_slug  text NOT NULL,
    PRIMARY KEY (partner_id, retreat_slug)
);
