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

A **module** owns its tables and its HTTP routes. It talks to another module through a **C# interface** (a port), never `SELECT` from the other schema. Tomorrow, Payment can become its own service by swapping that port for HTTP. You do not pay Kubernetes until a module actually needs its own scale.

## Demo vs live UI

- `src/` — the real app (talks to the gateway).
- `ui-reference/` — old clickable demo. Do not treat its mock prices as launch fact. Delete it when you no longer need the snapshot.

## Hard runtime rules

- Gateway does not write business data.
- Browser never marks payment `paid`.
- Logs must not contain phone, email, or wellness free text.
- Unpublished catalog rows are invisible on every public GET.
