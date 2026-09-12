# Technical architecture (V1)

## Goal

Ship a liveable first iteration: React frontend, an API gateway, a .NET **modular monolith**, one Postgres. Tomorrow a module can be extracted and scaled on its own without rewriting the product.

## Runtime picture

```text
Browser
   │
   ▼
React (Vite) ──────────────────────────── static in Docker/nginx
   │  /api/*
   ▼
Gateway (.NET + YARP)
   │  correlation id, auth surface, rate-limit hook
   ▼
Healingram.Api  (modular monolith host)
   │
   ├── Identity module      schema identity
   ├── Catalog module       schema catalog
   ├── Matching module      schema matching
   ├── Availability module  schema availability
   ├── Booking module       schema booking
   ├── Payment module       schema payment
   ├── Leads module         schema leads
   └── Partners module      schema partners
   │
   ▼
PostgreSQL (one server, many schemas)

Side cars in Docker (dev/UAT only):
  Seq          logs
  Jaeger       traces
  Mailpit      email
  (optional later) Redis  — only if a story proves we need cache/locks
```

Production later replaces Seq/Jaeger/Mailpit with the cloud vendor’s equivalents. Application code already speaks **OpenTelemetry** and **SMTP**, so that is a config change, not a rewrite.

## Why modular monolith (not microservices now)

V1 has one team and one deploy. Separate processes would add network, versioning, and local-run cost before we have traffic. Modules still have:

- their own schema and DbContext
- their own domain events
- public contracts in `Healingram.Contracts`
- no cross-table joins

Extract later: host the module in its own process, implement the same contract over HTTP, leave the gateway route in place.

## Gateway responsibilities

- Terminate public HTTP
- Forward `/api/*` to the monolith
- Attach/propagate `X-Correlation-Id`
- Future: JWT/cookie check, rate limits, WAF headers
- Health and OpenAPI at the edge

Gateway does **not** write availability, prices, or payments.

## Frontend

Existing React 19 + Vite + React Router + Tailwind app stays the customer/partner/admin shell. It talks only to the gateway base URL (`VITE_API_BASE_URL`).

## Data

| Store | V1 use |
| --- | --- |
| PostgreSQL | All transactional records |
| Redis | Not in V1 unless a story’s prerequisites approve it (holds, rate limits) |
| Mongo | Not in V1 |

## Logging and telemetry

See [observability.md](observability.md). Every inbound request gets a correlation ID. Module boundaries emit spans. PII is excluded from logs.

## Identity (planned)

JWT access + refresh (or cookie session — decided in EPIC-02). Roles: `customer`, `partner`, `admin`. Authorization is checked in the module, not only in the UI.

## Payments (planned)

Provider-ready interface + webhook with signature, amount, currency, reference, and idempotency. Settlement mode is data (`MARKETPLACE_SPLIT` | `PARTNER_DIRECT`), not a hardcoded code path. Dev uses a fake provider + Mailpit.

## Notifications (planned)

Outbox in Postgres + a worker. Dev SMTP = Mailpit. No WhatsApp Business API in V1; WhatsApp is a consented handoff after the lead is saved.

## Run modes

| Mode | How |
| --- | --- |
| Docker everything | `docker compose -f infra/docker-compose.yml -f infra/docker-compose.observability.yml up --build` |
| Local API + Docker deps | Postgres/Seq/Jaeger/Mailpit in Docker; `dotnet run` the API and gateway; `npm run dev` the web |
| CI | GitHub Actions builds and tests every PR into `feature/v1-iteration-1` |

## Image rules

Multi-stage Dockerfiles. Runtime images contain only the compiled app + runtime. Frontend image is nginx:alpine serving the Vite build. No secrets in images.
