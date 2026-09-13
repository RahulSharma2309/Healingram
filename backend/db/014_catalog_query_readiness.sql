-- Catalogue query indexes and partner membership status.
-- Filters already used by public search: publication, place, need, theme, programme, price.
-- Do not index speculative columns. Safe to apply once via schema_migrations.

CREATE INDEX IF NOT EXISTS programmes_theme_idx
    ON catalog.programmes (theme_slug);

CREATE INDEX IF NOT EXISTS programmes_retreat_idx
    ON catalog.programmes (retreat_id);

CREATE INDEX IF NOT EXISTS programmes_slug_idx
    ON catalog.programmes (slug);

CREATE INDEX IF NOT EXISTS programme_prices_programme_status_idx
    ON catalog.programme_prices (programme_id, status);

ALTER TABLE partners.partner_users
    ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'active';

ALTER TABLE partners.partner_users
    DROP CONSTRAINT IF EXISTS partner_users_status_check;

ALTER TABLE partners.partner_users
    ADD CONSTRAINT partner_users_status_check
    CHECK (status IN ('active', 'suspended', 'revoked'));
