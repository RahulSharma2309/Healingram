# Languages and tools

## What runs today (this laptop)

| Layer | Technology | Why |
| --- | --- | --- |
| Website | React 19, Vite, React Router, Tailwind | Screens in `src/` |
| Public API door | .NET 10, YARP (`Healingram.Gateway`) | Port **5000**. Stamps `X-Correlation-Id`. No business writes |
| Product API | .NET 10 modular monolith (`Healingram.Api`) | Port **5080**. One process, many modules |
| Database | PostgreSQL 16 | One database, **one schema per module** |
| Auth | JWT (access + refresh) | Identity module |
| Local email | Mailpit (SMTP `:1025`, UI `:8025`) | Outbox worker delivers here, not to the internet |
| Logs | Seq `:5341` | Search by correlation id |
| Traces | Jaeger `:16686` | OpenTelemetry OTLP |
| Repo / CI | Git, GitHub Actions | Private repo is enough |
| Editor | Cursor | Agents + skills in `.cursor/skills/` |

Website env: `VITE_API_BASE_URL=http://localhost:5000`. The browser **never** talks to Postgres.

## Modules inside the API

`Identity` · `Catalog` · `Matching` · `Availability` · `Booking` · `Payment` · `Leads` · `Partners`  
Plus building-blocks: schema installer, outbox, correlation, health.

SQL files: `backend/db/001_schemas.sql` … `013_enterprise_foundation_completion.sql`, applied once via `public.schema_migrations`. Provider ports: `IOtpProvider`, `IPaymentProvider`, `IInventoryProvider`.

## What we deliberately did not add in V1

Kubernetes, Redis, Mongo, a second region, WhatsApp Business API, live Razorpay. Those are later *replacements*, not a rewrite, if we keep the module boundaries.
