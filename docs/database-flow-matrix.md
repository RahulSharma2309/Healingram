# Database flow matrix

| Flow | Tables read | Tables written | Transaction boundary | Events | Authorization |
| --- | --- | --- | --- | --- | --- |
| Browse catalogue | `catalog.retreats`, `programmes`, `programme_prices`, `needs`, `destinations`, `themes` | none | read-only SQL page | none | public, published only |
| Retreat listing | retreat + programmes + rooms/experts/media/sections | none | read-only | none | public, published only |
| Price quote | retreat, programmes, prices | `catalog.price_quotes` | quote insert | none | public |
| Match session | published cards via catalog port; `matching.questions` | `matching.match_sessions` | session insert | none | public |
| Guest request create | retreat/programme, users | `identity.users` (guest), `availability.requests`, `otp_challenges`, `inventory.holds`, outbox | request + optional hold | request created | anonymous; registered email/phone refused |
| Guest OTP verify | otp_challenges, requests | otp consume, refresh_tokens | consume + token | login/guest | destination + publicId bind |
| Customer request | same as guest, existing user | requests, holds, outbox | request create | request created | registered customer |
| Partner confirm | requests, partner_retreats | request status, booking, outbox, history | UoW: availability + booking + outbox | BookingCreated | PartnerWrite + retreat slug |
| Partner alternative / unavailable | requests, membership slugs | request, alternatives, hold release, outbox | request transition | status | PartnerWrite + slug |
| Payment intent | booking, request | `payment.intents` | intent create | none | owner; not guest |
| Payment webhook | intent, webhook_events, booking, holds | webhook, intent, booking paid, inventory confirm, outbox | webhook process | PaymentPaid, BookingConfirmed | provider signature / local secret |
| Webhook replay | same | unique provider_event_id; reconcile is idempotent | same | no duplicate paid | same |
| Wishlist | catalog slug existence (logical) | `identity.wishlist` | add/remove | none | registered |
| Admin note | request | `availability.admin_notes` | note insert | audit | `requests.manage` |
| Vendor login | users, credentials, partner_users, partners | refresh_tokens, audit | login | login audit | portal + active membership |
| Schema migrate | `schema_migrations` | schema objects + ledger | one file per tx + advisory lock | none | process startup |

Idempotency keys: availability `idempotency_key`, booking `request_id`, payment `idempotency_key`, webhook `provider_event_id`.
