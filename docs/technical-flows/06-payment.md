# Technical — payment

## Path

```text
/requests/{publicId}/payment
  → POST /api/payment/intents
  → Payment looks up booking by publicId
  → INSERT payment.intents (status creating, stable idempotency key)
  → IPaymentProvider.CreatePayment (provider-neutral; local today)
  → UPDATE intent ready
  → later provider webhook (local: admin simulate, `/api/payment/webhooks/local`, or `/api/payment/webhooks/fake`)
  → webhook event received → process → processed
  → payment.intents = paid
  → booking marked paid (`awaiting_payment` only)
  → inventory hold confirmed via `IInventoryProvider`
  → notifications.outbox (`PaymentPaid`, `BookingConfirmed`)
  → GET /api/trips puts the stay in upcoming
```

The return URL and `GET /api/payment/intents/{id}` are **read-only**. Auth and ownership: [security-and-authorization.md](../engineering/security-and-authorization.md).

## APIs

| Method | Path | Auth | Result |
| --- | --- | --- | --- |
| POST | `/api/payment/intents` | Bearer (registered owner) | `{ id, status: "ready", checkoutUrl }` only if booking is `awaiting_payment` and has amount. Guest token → 403. Other customer → 403 |
| GET | `/api/payment/intents/{id}` | Bearer (owner or admin) | Current status |
| POST | `/api/payment/webhooks/local` | header `X-Webhook-Secret` | Dev/UAT scripts only. Disabled when `Payment:AllowLocalSimulate=false`. Same `providerEventId` replays and **reconciles** booking paid |
| POST | `/api/admin/payments/simulate` | Admin + `payments.simulate` | Same as local webhook; secret stays on the server. 404 when `Payment:AllowLocalSimulate=false`. Production refuses `AllowLocalSimulate=true` |

## Tables

| Table | Role |
| --- | --- |
| `booking.bookings` | Must exist and be `awaiting_payment` before an intent |
| `payment.intents` | Amount copied from booking; status `creating` → `ready` → `paid` (or `failed`). At most one open intent per booking |
| `payment.webhook_events` | Provider event id + `processing_status` (`received` / `processing` / `processed` / `failed`). Replay still reconciles booking |
| `notifications.outbox` | Paid mail job; enqueue failure fails the webhook so the provider can retry |

## Production swap

`Payment:Provider` and `Inventory:Provider` select adapters at startup (`local` today). Unimplemented or unknown names refuse to start. Do not put a webhook secret in `VITE_*`. Never trust the browser.
