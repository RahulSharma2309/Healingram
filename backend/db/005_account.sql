-- Account wishlist (idempotent). Safe to re-run. No cross-schema foreign keys.

CREATE TABLE IF NOT EXISTS identity.wishlist (
  user_id uuid NOT NULL,
  retreat_slug text NOT NULL,
  created_at timestamptz NOT NULL DEFAULT now(),
  PRIMARY KEY (user_id, retreat_slug)
);
