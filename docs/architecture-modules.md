# Module architecture

Last reviewed against the current modular monolith in `backend/src`.

Modules register themselves. Do not add a second host. Do not `SELECT` across schemas.

| Module | Responsibility | Depends on | Persistence | Public HTTP | Provider boundary |
| --- | --- | --- | --- | --- | --- |
| Identity | Register, login, refresh, logout, profile, OTP, guest verify, wishlist, inbox, admin overview | Partners (membership list), Availability (request access lookup), Audit | `identity.*`, `audit.events`, `notifications.inbox` | `/api/auth/*`, `/api/users/me`, `/api/wishlist`, `/api/notifications`, `/api/admin/overview` | `IOtpProvider` |
| Catalog | Published inventory, content, quotes, places, needs, themes | none | `catalog.*`, `content.*`, `platform.settings` | `/api/catalog/*`, `/api/content/*`, `/api/platform/settings` | none |
| Matching | Questionnaire options, rank published retreats | `ICatalogReadPort` | `matching.*` | `/api/matching/*` | none |
| Availability | Availability requests, partner/admin queues, trips, inventory holds | Catalog labels, Booking commands, Inventory, Partners, Identity guest | `availability.*`, `inventory.holds` | `/api/availability/*`, `/api/trips`, `/api/partner/availability`, `/api/admin/availability` | `IInventoryProvider` |
| Booking | Booking records and status transitions | Payment port (inbound) | `booking.*` | `/api/booking/ready` (writes go through Availability/Payment) | none |
| Payment | Intents, webhooks, booking reconcile, inventory confirm | Booking, Inventory, Outbox | `payment.*` | `/api/payment/*`, `/api/admin/payments/simulate` | `IPaymentProvider` |
| Partners | Memberships, retreat-slug isolation, local partner seed | Identity users (seed only) | `partners.*` | `/api/partner/me`, `/api/partner/retreats` | none |
| Leads | Expert lead capture | none | `leads.*` | `/api/leads/*` | none |
| BuildingBlocks | Schema installer, `PageResult`, outbox, UoW, runtime safety | — | `public.schema_migrations`, `notifications.outbox` | health/meta on the host | — |
| Gateway | Reverse proxy, CORS, correlation | API upstream | none | `/api/*` → API | none |

## Invariants

- Catalog publication: `active` + `identity_complete` + at least one valid programme.
- Paid bookings only via verified webhook (or admin simulate when explicitly enabled in Development).
- Partner isolation is retreat-slug based. Never trust a client `partnerId`.
- Matching ranks only published cards from `ICatalogReadPort`.
- Empty catalogue/needs/themes tables return empty arrays. The API does not invent product data.

## Important files

- Host: `backend/src/Healingram.Api/Program.cs`
- Runtime safety: `Healingram.BuildingBlocks/Runtime/HealingramRuntime.cs`
- Catalog search SQL: `Healingram.Modules.Catalog/Persistence/CatalogSearchSql.cs`
- Vendor login gate: `Healingram.Modules.Identity/Auth/AuthService.cs`
