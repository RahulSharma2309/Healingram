# Technical flows

Same journeys as `docs/po-flows/`, but **how the code runs**: browser → gateway `:5000` → API `:5080` → Postgres schema.

JSON is camelCase. The website sends `Authorization: Bearer <access>` after login. Gateway stamps `X-Correlation-Id`.

| File | Flow |
| --- | --- |
| [01-browse.md](01-browse.md) | Catalog reads |
| [02-login.md](02-login.md) | Identity JWT |
| [03-find-my-match.md](03-find-my-match.md) | Matching session |
| [04-request-availability.md](04-request-availability.md) | Availability create |
| [05-partner-confirm.md](05-partner-confirm.md) | Confirm / alternative |
| [06-payment.md](06-payment.md) | Intent + webhook |
| [07-wishlist-and-trips.md](07-wishlist-and-trips.md) | Wishlist + trips |
| [08-contact.md](08-contact.md) | Leads |
| [09-create-retreat.md](09-create-retreat.md) | Seed only |
| [10-admin.md](10-admin.md) | Admin queue |

Modules do **not** join each other’s tables. Availability talks to catalog/booking/partners through in-process ports, not SQL across schemas.
