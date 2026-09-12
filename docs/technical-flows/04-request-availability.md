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
| POST | `/api/availability/requests` | optional Bearer | If logged in, `customer_user_id` is set (needed for My Trips) |
| GET | `/api/availability/requests/{publicId}` | no | Snapshot + status |

Body: `idempotencyKey`, `retreatSlug`, `programmeSlug`, `durationNights`, `occupancy`, `guests`, `checkIn`, `customerName`, `email`, `phone`.

`201` → `{ publicId: "HR-2026-#####", status: "REQUESTED", snapshot }`.  
Same key + same fingerprint → replay. Same key + different body → `409`.

## Tables

| Table | Role |
| --- | --- |
| `availability.requests` | The stay ask; snapshot JSON; `final_amount_inr` empty until confirm |
| `availability.status_history` | `null → REQUESTED` |
| `catalog.retreats` | Read-only check: published? |

Statuses: `REQUESTED` → `CONFIRMED` | `ALTERNATIVE_OFFERED` | `UNAVAILABLE`.
