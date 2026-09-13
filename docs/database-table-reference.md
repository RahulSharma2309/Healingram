# Database table reference

Last reviewed against the current `backend/db` scripts. Module = schema owner.

| Table | Purpose | PK | Important FKs / uniqueness | Status / lifecycle | Written by | Read by |
| --- | --- | --- | --- | --- | --- | --- |
| `identity.users` | Accounts | `id` | unique email; unique `phone_e164` when set | `status`, `account_status` guest/registered | Identity | Identity, Partners seed |
| `identity.credentials` | Password hashes | `user_id` unique | user | — | Identity | Identity |
| `identity.refresh_tokens` | Refresh sessions | `id` | user; stores `auth_kind`, `purpose`, `request_id` | revoked_at | Identity | Identity |
| `identity.user_roles` | Extra roles | (user, role) | user | — | Identity | Identity |
| `identity.admin_permissions` | Admin grants | (user, permission) | user | empty = bootstrap all | Identity seed | Identity policies |
| `identity.otp_challenges` | OTP attempts | id | destination + purpose + publicId | consumed atomically | Identity | Identity |
| `identity.wishlist` | Saved slugs | (user, slug) | user | — | Identity | Identity |
| `catalog.needs` | Need chips | slug | — | `active` | Catalog seed | Catalog API |
| `catalog.destinations` | Place copy | slug | parent_slug logical | — | Catalog seed | Catalog places |
| `catalog.retreats` | Stays | id / unique slug | — | publication gate | Catalog seed | Catalog, Matching port, Partners map |
| `catalog.programmes` | Programmes | id / unique (retreat, slug) | retreat_id | valid slug+name | Catalog seed | Catalog search |
| `catalog.programme_prices` | Prices | id | programme | VERIFIED / ESTIMATED / ON_REQUEST | Catalog seed | Catalog cards/quotes |
| `catalog.rooms` / `experts` / `testimonials` / `inclusions` | Listing sections | id | retreat/programme | verified flags | Catalog seed | Listing DTO |
| `catalog.themes` | Theme labels | slug | — | — | Catalog seed | `/themes` |
| `catalog.discovery_cards` | Home discovery | slug | — | — | Catalog seed | `/discovery` |
| `catalog.retreat_media` / `retreat_sections` | Listing media | id | retreat | — | Catalog seed | Listing DTO |
| `catalog.price_quotes` | Quote snapshots | id | retreat/programme slugs | — | Catalog quote API | Catalog |
| `matching.match_sessions` | Match runs | id | — | — | Matching | Matching |
| `matching.questions` / `question_options` | Questionnaire | id | — | active | Catalog/matching seed | Matching options |
| `availability.requests` | Availability requests | id / publicId | customer, retreat/programme slugs, unique idempotency_key | status machine | Availability | Customer/vendor/admin |
| `availability.status_history` | Request history | id | request | — | Availability | Request detail |
| `availability.alternatives` | Partner alternatives | id | request | — | Availability | Request detail |
| `availability.admin_notes` | Admin notes | id | request | — | Admin API | Admin queue |
| `booking.bookings` | Bookings | id | unique request_id | status check constraint | Booking on confirm | Payment, trips |
| `booking.events` | Booking audit | id | booking | — | Booking | Booking |
| `payment.intents` | Payment intents | id | unique idempotency_key; one open per booking | creating/ready/paid/failed | Payment | Customer/admin |
| `payment.webhook_events` | Provider events | id | unique provider_event_id | received/processed/failed | Payment webhook | Payment |
| `inventory.holds` | Local holds | id | request_public_id | hold/confirm/release | Availability/Payment | Inventory provider |
| `partners.partners` | Vendor orgs | id | — | pending/approved/rejected/suspended | Partner seed | Partner access |
| `partners.partner_users` | Memberships | (partner_id, user_id) | membership_role, status | active/suspended/revoked | Partner seed | Vendor login + PartnerWrite |
| `partners.partner_retreats` | Slug isolation | (partner, slug) | — | — | Partner seed | Partner queues |
| `leads.expert_leads` | Talk-to-expert | id | — | — | Leads | Admin |
| `leads.status_history` / `leads.options` | Lead history / form | id | — | — | Leads | Leads |
| `content.pages` / `sections` / `navigation_*` | CMS-lite content | slug/id | — | published/active | Catalog seed | Content API |
| `notifications.outbox` | Event outbox | id | — | pending/sent | Modules via UoW | Outbox worker |
| `notifications.inbox` | User inbox | id | user | read_at | Outbox consumer | `/api/notifications` |
| `audit.events` | Security/business audit | id | actor | — | Identity/Payment/etc | Admin audit |
| `platform.settings` | Key/value config | key | — | — | Seed | `/api/platform/settings` |
| `public.schema_migrations` | Migration ledger | id | — | applied_at | SchemaInstaller | SchemaInstaller |

Authorization implication: a table is never reachable because a frontend route exists. Every write path above has a server check (session, ownership, membership, or admin permission).
