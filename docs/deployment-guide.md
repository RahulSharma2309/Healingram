# Deployment guide

Practical path for the architecture the application actually supports: **static React app + YARP gateway + one API process + one PostgreSQL**. No Kubernetes manifests ship in this repo.

Also see [engineering/deploy.md](engineering/deploy.md) and [production-infrastructure-and-cost.md](production-infrastructure-and-cost.md).

## Local development

[local-development.md](local-development.md). Docker alternative: `./scripts/dev-up.ps1` (web `:8080` via nginx → gateway).

## Staging / production (same shape)

1. **PostgreSQL** — managed instance, private network, automated backups.
2. **API** — `infra/docker/api.Dockerfile`. Env: Production, real `Jwt__Key`, `ConnectionStrings__Postgres`, `Otp__Provider` / `Payment__Provider` / `Inventory__Provider` that exist in the build (today only `local` is implemented, so Production **will not start** until real providers exist or explicit allow flags are set).
3. **Gateway** — `infra/docker/gateway.Dockerfile`. Point YARP cluster at the API. CORS = `App__*Url` only.
4. **Frontend** — `infra/docker/web.Dockerfile`. Build with `VITE_API_BASE_URL=https://api.example.com` (the gateway), `VITE_DEMO_MODE=false`.
5. **DNS / TLS** — terminate HTTPS on a load balancer or reverse proxy in front of the gateway (and the static host/CDN).
6. **Migrations** — API `Schema__ApplyOnStartup=true` is supported (advisory lock). Or run one installer instance before scaling out.
7. **Seed** — off in Production. Load real catalogue with SQL/ETL, not `LocalDemoCatalogData`.
8. **Health** — probe gateway + API ready endpoints. Logs to Seq-equivalent; traces optional.
9. **Rollback** — previous container image. Schema rollback = restore backup.
10. **Scale** — more API replicas behind the gateway after migrations are applied. Stateless API. Sticky sessions not required (JWT).

## What this app does not ship

Terraform/Helm, object-storage CDN automation, multi-region failover. Those can sit around the same images.

## India notes

Razorpay and MSG91/Twilio are the intended future payment/OTP vendors. Keep data residency and invoice requirements in the production checklist when those providers are wired.
