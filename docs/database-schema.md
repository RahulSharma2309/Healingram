# Database schema

Last reviewed against `backend/db/001_schemas.sql` … `014_catalog_query_readiness.sql`.

One PostgreSQL database. One schema per module. **No cross-schema foreign keys** — ownership is enforced in application code and, where safe, unique/check constraints inside a schema.

## How migrations run

`SchemaInstaller.EnsureCreatedAsync` opens Postgres, takes `pg_advisory_lock(872314001)`, creates `public.schema_migrations` if needed, records already-applied legacy scripts, then runs each pending `*.sql` in filename order inside its own transaction. Re-run is skipped by ledger id (the filename). Two API processes cannot apply the same pending file at once.

Docker images copy the entire `backend/db/` folder. Compose does **not** init-mount `001` into Postgres — the API is the migrator.

## Relationship sketch

```text
identity.users
  ├── identity.credentials
  ├── identity.refresh_tokens
  ├── identity.user_roles
  ├── identity.admin_permissions
  ├── identity.otp_challenges
  ├── identity.wishlist
  └── partners.partner_users → partners.partners → partners.partner_retreats
                                                  └── catalog.retreats.slug (logical)

catalog.retreats
  └── catalog.programmes
        ├── catalog.programme_prices
        └── catalog.inclusions

availability.requests
  ├── availability.status_history
  ├── availability.alternatives
  ├── availability.admin_notes
  ├── inventory.holds
  └── booking.bookings
        └── payment.intents
              └── payment.webhook_events
```

## Publication rule (catalog)

A retreat is public when status is `active`/`published`, `identity_complete` is true, and at least one programme has a non-empty slug and name. Geography never blocks publication.

## Booking statuses (check constraint)

`awaiting_payment` · `paid` · `completed` · `cancelled` · `refund_pending` · `refunded`

## Partner membership

`partners.partner_users` primary key `(partner_id, user_id)`. `status` in (`active`, `suspended`, `revoked`). Effective vendor access = partner `approved` AND membership `active`.

## Indexes that matter for catalogue search

Already present and used by the SQL search path:

- `catalog.retreats (status, identity_complete)`
- `catalog.retreats (state_slug, locality_slug)`
- `catalog.programmes (need_slug)`, `(theme_slug)`, `(retreat_id)`, `(slug)`
- `catalog.programme_prices (programme_id, status)`

See [database-table-reference.md](database-table-reference.md) for every table.
