# System traceability

Map from product flow to code and data. Last reviewed against the current implementation.

| Product flow | Frontend | API | Module / service | Tables | Event / provider |
| --- | --- | --- | --- | --- | --- |
| Landing / browse | `Home.tsx`, `RetreatList.tsx`, `SearchResults.tsx` | `GET /api/catalog/retreats`, needs, places, homepage, navigation | `CatalogQueryService` → Postgres search | `catalog.*`, `content.*` | none |
| Retreat detail | `RetreatDetail.tsx` | `GET /api/catalog/retreats/{slug}` | Catalog | retreat + children | none |
| Questionnaire / match | `Questionnaire.tsx` | `GET /api/matching/options`, `POST /api/matching/sessions` | `MatchingService` + `ICatalogReadPort` | `matching.*`, published catalog via port | none |
| Availability request | `AvailabilityRequestModal.tsx` | `POST /api/availability/requests` | `AvailabilityService` | `availability.requests`, optional `inventory.holds`, guest user | outbox |
| Guest verify | `GuestVerifyForm.tsx` | `/api/auth/guest/verify-start`, `/verify` | `OtpService` + `AuthService` | `otp_challenges`, refresh_tokens | `IOtpProvider` |
| Customer login | `Login.tsx` | `POST /api/auth/login` | `AuthService` | users, credentials, refresh, audit | none |
| Vendor login | `VendorLogin.tsx` | `POST /api/auth/login` `portal=vendor` | `AuthService` + `IPartnerAccess` | users, `partner_users`, partners | none |
| Vendor queue | `VendorDashboard.tsx` | `GET /api/partner/availability`, confirm/alternative/unavailable | Availability + PartnerAccess | requests, bookings, partner_retreats | outbox |
| Admin queue | `AdminDashboard.tsx` | `/api/admin/availability`, notes, simulate | Availability + Payment | requests, notes, intents, webhooks | local simulate only in demo |
| Payment | `PaymentReady.tsx` | `POST /api/payment/intents`, webhook | `PaymentService` | intents, webhook_events, bookings, holds | `IPaymentProvider` |
| Trips / requests | `UserDashboard.tsx` | `/api/availability/mine`, `/api/trips` | Availability | requests, bookings | none |
| Wishlist | `WishlistButton.tsx` | `/api/wishlist` | Identity | `identity.wishlist` | none |
| Talk to expert | `TalkToExpert.tsx` | `POST /api/leads` | Leads | `leads.expert_leads` | none |
| Content pages | `StaticPage.tsx`, `Blog.tsx` | `/api/content/pages` | Catalog | `content.pages` | none |

## Adding a vendor

Insert `partners.partners` (`approved`), `partners.partner_users` (`status=active`), `partners.partner_retreats` for that org’s slugs, and grant the user `partner` role. No auth redesign.

## Adding an admin

Grant `admin` role. Optionally insert `identity.admin_permissions` (empty set = bootstrap all known permissions).

## Changing product content

Update `catalog.needs` / `destinations` / `themes` / `discovery_cards` / `content.*` / matching questions. Restart is not required for row updates; the next GET reads Postgres. Do not edit `src/` copy for those values.
