# API reference

Browser calls go to the **gateway** (`http://localhost:5000/api/...`). The API process also listens on `:5080`. Auth header: `Authorization: Bearer <accessToken>`. Correlation: `X-Correlation-Id`.

Errors use HTTP status plus `{ error, details? }`. Do not treat HTTP 200 + empty `items` as a transport failure. Transport failure is 5xx / network / 401 after refresh.

Last reviewed against the current module endpoints.

## Auth

| Method | Route | Auth | Purpose | Notes |
| --- | --- | --- | --- | --- |
| POST | `/api/auth/register` | no | Create customer | Rate: `auth-register` |
| POST | `/api/auth/login` | no | Session | `portal`: customer/vendor/admin. Vendor requires active membership (non-admin). Rate: `auth-login` |
| POST | `/api/auth/refresh` | no | Rotate refresh | Preserves guest_request scope |
| POST | `/api/auth/logout` | no | Revoke refresh | |
| POST | `/api/auth/guest/verify-start` | no | Start OTP | Requires `publicId`. Rate: `otp-send` |
| POST | `/api/auth/guest/verify` | no | Consume OTP | Scoped guest token. Rate: `otp-verify` |
| GET | `/api/users/me` | Bearer | Profile + roles + memberships | |
| PATCH | `/api/users/me` | Bearer registered | Profile | Guest tokens forbidden |

## Catalogue / content

| Method | Route | Auth | Purpose |
| --- | --- | --- | --- |
| GET | `/api/catalog/needs` | no | Active needs from Postgres (empty if none) |
| GET | `/api/catalog/places` | no | Published place tree |
| GET | `/api/catalog/themes` | no | Theme labels (empty if none) |
| GET | `/api/catalog/discovery` | no | Home cards |
| GET | `/api/catalog/retreats` | no | Page of published cards. Query: need, state, locality, duration, theme, programme, type, minPrice, maxPrice, sort, page, pageSize |
| GET | `/api/catalog/retreats/{slug}` | no | Full listing or 404 |
| POST | `/api/catalog/pricing/quote` | no | Persist quote snapshot |
| GET | `/api/content/homepage` | no | Homepage sections |
| GET | `/api/content/navigation` | no | `?menu=` |
| GET | `/api/content/pages` | no | Published pages |
| GET | `/api/content/pages/{slug}` | no | One page or 404 |
| GET | `/api/platform/settings` | no | e.g. WhatsApp number |

## Matching

| Method | Route | Auth | Purpose |
| --- | --- | --- | --- |
| GET | `/api/matching/options` | no | Questions from DB |
| POST | `/api/matching/sessions` | no | Rank + persist. Response includes `matches[].retreat` card |

## Availability / trips

| Method | Route | Auth | Purpose |
| --- | --- | --- | --- |
| POST | `/api/availability/requests` | optional | Create request; idempotency key required |
| GET | `/api/availability/requests/{publicId}` | owner / guest scope / partner slug / admin | Detail |
| POST | `/api/availability/requests/{publicId}/confirm` | PartnerWrite + slug | → booking awaiting_payment |
| POST | `.../alternative` | PartnerWrite + slug | |
| POST | `.../unavailable` | PartnerWrite + slug | |
| POST | `.../cancel` | owner | |
| POST | `.../accept-alternative` | owner | |
| GET | `/api/availability/mine` | Bearer | Customer or scoped guest |
| GET | `/api/guest/requests/{publicId}` | guest_request | Same as scoped read |
| GET | `/api/trips` | registered | Grouped bookings |
| GET | `/api/partner/availability` | PartnerWrite | Own slugs only |
| GET | `/api/admin/availability` | `requests.read` | All |
| POST | `/api/admin/availability/{publicId}/note` | `requests.manage` | |

## Payment

| Method | Route | Auth | Purpose |
| --- | --- | --- | --- |
| POST | `/api/payment/intents` | owner, not guest | Create intent. Rate: `payment-create` |
| GET | `/api/payment/intents/{id}` | owner or admin | |
| POST | `/api/payment/webhooks/local` | webhook secret | Dev only; 404 if simulate disabled |
| POST | `/api/payment/webhooks/fake` | webhook secret | Dev only |
| POST | `/api/admin/payments/simulate` | `payments.simulate` | Demo/UAT |

## Vendor / admin / other

| Method | Route | Auth | Purpose |
| --- | --- | --- | --- |
| GET | `/api/partner/me` | PartnerWrite | Memberships + slugs |
| GET | `/api/partner/retreats` | PartnerWrite | Isolated slugs |
| GET | `/api/wishlist` | registered | |
| POST | `/api/wishlist` | registered | |
| DELETE | `/api/wishlist/{slug}` | registered | |
| GET | `/api/notifications` | Bearer | Inbox |
| POST | `/api/notifications/{id}/read` | Bearer | |
| GET | `/api/admin/overview` | `users.read` | |
| GET | `/api/admin/leads` | AdminLeadsRead | |
| GET | `/api/admin/audit` | `audit.read` | |
| POST | `/api/leads` | no | Expert lead |
| GET | `/api/leads/options` | no | |
| GET | `/api/leads/{id}` | AdminLeadsRead | |
| GET | `/api/*/ready` | no | Module liveness |

Idempotency: availability create and payment create require keys. Webhook `provider_event_id` is unique.
