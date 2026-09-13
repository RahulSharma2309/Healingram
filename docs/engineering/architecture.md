# Architecture (simple)

```text
Customer browser
    │
    │  pages + JSON
    ▼
React app (static files in production)
    │
    │  /api/*   +  Authorization  +  X-Correlation-Id
    ▼
Gateway (YARP)          ← only public door
    │
    ▼
Healingram.Api          ← one process
    │
    ├── Identity schema
    ├── Catalog schema
    ├── Matching schema
    ├── Availability schema
    ├── Booking schema
    ├── Payment schema
    ├── Leads schema
    ├── Partners schema
    └── notifications.outbox
    │
    ▼
One Postgres
```

## Why a modular monolith (not microservices)

You are one founder. The guest journey is one product. Splitting into `infra-auth`, `infra-catalog`, … already bit us locally (old Docker gateway stole port 5000).

A **module** owns its tables and its HTTP routes. It talks to another module through a **C# interface** (a port), never `SELECT` from the other schema. Business code depends on `IOtpProvider`, `IPaymentProvider`, `IPartnerAuthorization`, `INotificationOutbox`, and `IAuditPort` — not Twilio, Razorpay, or SMTP. Tomorrow, Payment can become its own service by swapping that port for HTTP. You do not pay Kubernetes until a module actually needs its own scale.

Configurable hosts: `App:CustomerUrl`, `App:VendorUrl`, `App:AdminUrl` (frontend: `VITE_*_APP_URL`). Production CORS is those origins only (no localhost). Production refuses default JWT/webhook secrets, demo seeds, `DemoMode`, local payment simulation, and OTP/payment/inventory providers that are not implemented in this build. `Inventory:Provider` is selected at startup the same way as OTP and Payment — unknown names fail start; `local` is blocked in Production unless `Inventory:AllowLocalInProduction=true`. Schema scripts apply once via `public.schema_migrations` (`001`–`013`). Cross-module writes (availability confirm + booking + outbox) share `IUnitOfWork`. Provider selection is at startup — see [security-and-authorization.md](security-and-authorization.md).

Business data path is PostgreSQL → module services → `/api` → React. The frontend may keep session tokens in `sessionStorage`, React query state, and form drafts. It must not keep an authoritative availability/trips/catalog store. Collection GETs return `{ items, page, pageSize, total }`. Catalogue search (`need`, `state`, `locality`, `duration`, `theme`, `programme`, `type`, price, `sort`) is executed in PostgreSQL (`WHERE` / `JOIN` / `ORDER BY` / `LIMIT`), then one page is returned. The frontend requests `page` + `pageSize=24` and does not download the full catalogue. Matching returns complete retreat cards so the questionnaire UI does not re-fetch inventory. Homepage sections and customer navigation come from `content.sections` and `content.navigation_*`. Schema scripts apply once via `public.schema_migrations` (`001`–`014`) under a Postgres advisory lock.

Gateway = routing, CORS, correlation. API = authentication, authorization, rate limit.

## Canonical surfaces

- `src/` — the real React app (presentation only; talks to the gateway).
- `backend/` — the modular monolith.
- PostgreSQL — the only business source of truth.
- `ui-reference/` — design-only snapshot. Not the production frontend. Do not import it from `src/`.

## Hard runtime rules

- Gateway does not write business data.
- Browser never marks payment `paid`.
- Logs must not contain phone, email, or wellness free text.
- Unpublished catalog rows are invisible on every public GET.
