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
| POST | `/api/auth/guest/verify` | no | `{ email or phone, code }` — `560142` issues a guest JWT, or `{ matched: false }` if that contact has no customer |
| GET | `/api/availability/mine` | Bearer | That customer’s requests only |
| POST | `/api/auth/register` | no | Same email/phone as a guest **promotes** that row (`account_status=registered`) |
| POST | `/api/payment/intents` | Bearer, registered only | Guest JWT is 403 |

Body: `idempotencyKey`, `retreatSlug`, `programmeSlug`, `durationNights`, `occupancy`, `guests`, `checkIn`, `customerName`, `email`, `phone`.

`201` → `{ publicId: "HR-2026-#####", status: "REQUESTED", snapshot }`.  
Same key + same fingerprint → replay. Same key + different body → `409`.

## Tables

| Table | Role |
| --- | --- |
| `identity.users.account_status` | `guest` (request only) or `registered` (password account) |
| `availability.requests` | The stay ask; snapshot JSON; `final_amount_inr` empty until confirm |
| `availability.status_history` | `null → REQUESTED` |
| `catalog.retreats` | Read-only check: published? |

Statuses: `REQUESTED` → `CONFIRMED` | `ALTERNATIVE_OFFERED` | `UNAVAILABLE`.
