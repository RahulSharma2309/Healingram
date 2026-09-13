# Technical — request availability

## Path

```text
Listing modal
  → POST /api/availability/requests
  → Availability module
  → catalog port (is this slug published?)
  → INSERT availability.requests + status_history
  → optional notifications.outbox
```

Frontend: `src/lib/api/availability.ts`, `src/lib/availabilityRequests.ts`.  
If the API fails, the UI **shows an error**. It must not invent a local-only booking.

## APIs

| Method | Path | Auth | Notes |
| --- | --- | --- | --- |
| POST | `/api/availability/requests` | optional Bearer | Logged-in user id, or a new/existing **guest** customer from email/phone |
| GET | `/api/availability/requests/{publicId}` | Bearer (owner, partner, or admin) | 401 if anonymous. Public id alone is not enough. |
| POST | `/api/auth/guest/verify-start` | no | `{ email or phone, channel }` — always `{ sent: true }` when the contact looks valid |
| POST | `/api/auth/guest/verify` | no | `{ email or phone, code, publicId?, purpose }` — request-scoped OTP via `IOtpService`. Local demo code is not returned unless `DemoMode=true`. |
| GET | `/api/availability/mine` | Bearer | That customer’s requests only. Paginated `{ items, page, pageSize, total }` |
| POST | `/api/availability/requests/{publicId}/cancel` | Bearer (owner) | `REQUESTED` / `ALTERNATIVE_OFFERED` / `CONFIRMED` → `CANCELLED` |
| POST | `/api/auth/register` | no | Same email/phone as a guest **promotes** that row (`account_status=registered`) |
| POST | `/api/payment/intents` | Bearer, registered only | Guest JWT is 403 |

Body: `idempotencyKey`, `retreatSlug`, `programmeSlug`, `durationNights`, `occupancy`, `guests`, `checkIn`, `customerName`, `email`, `phone`, optional `quoteId`, `checkOut`, `source`, `countryCode`, `customerNotes`. The server loads catalog stay labels and freezes the snapshot; the browser amount is not authoritative. Country and settlement are not invented.

`201` → complete request DTO (`publicId`, `status`, names, dates, snapshot, optional `bookingNumber`).  
Same key + same fingerprint → replay. Same key + different body → `409`.

Partner and admin lists are also paginated. Unpaid cancel releases inventory; paid cancel starts `refund_pending`.

Statuses: `REQUESTED` → `CONFIRMED` | `ALTERNATIVE_OFFERED` | `UNAVAILABLE` | `CANCELLED` | `REFUND_PENDING` | `REFUNDED`.

## Tables

| Table | Role |
| --- | --- |
| `identity.users.account_status` | `guest` (request only) or `registered` (password account) |
| `availability.requests` | The stay ask; snapshot JSON; `final_amount_inr` empty until confirm |
| `availability.status_history` | `null → REQUESTED` |
| `catalog.retreats` | Read-only check: published? |

Partner confirm / alternative / unavailable and GET by public id are not authorized by the partner role alone. Availability calls `IPartnerAccess.CanAccessRetreatAsync` so Partner A cannot read or change Partner B’s retreat. A public request id is not authentication. Guest OTP is purpose `REQUEST_ACCESS` and may bind `request_id` on the JWT. Create also opens a local inventory hold (`IInventoryProvider`); unavailable/cancel release it; paid webhook confirms it.
