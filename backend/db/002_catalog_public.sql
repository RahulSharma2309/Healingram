-- Healingram Catalog — public listing columns (idempotent).
-- Applied after 001_schemas.sql. Safe to re-run. Does not change 001.

ALTER TABLE catalog.retreats
    ADD COLUMN IF NOT EXISTS image_url text,
    ADD COLUMN IF NOT EXISTS typical_duration text,
    ADD COLUMN IF NOT EXISTS positioning text,
    ADD COLUMN IF NOT EXISTS locality_slug text;

ALTER TABLE catalog.programmes
    ADD COLUMN IF NOT EXISTS need_slug text,
    ADD COLUMN IF NOT EXISTS theme_slug text;

ALTER TABLE catalog.experts
    ADD COLUMN IF NOT EXISTS role text;

ALTER TABLE catalog.testimonials
    ADD COLUMN IF NOT EXISTS guest_name text;

CREATE INDEX IF NOT EXISTS retreats_publication_idx
    ON catalog.retreats (status, identity_complete);

CREATE INDEX IF NOT EXISTS retreats_place_idx
    ON catalog.retreats (state_slug, locality_slug);

CREATE INDEX IF NOT EXISTS programmes_need_idx
    ON catalog.programmes (need_slug);
