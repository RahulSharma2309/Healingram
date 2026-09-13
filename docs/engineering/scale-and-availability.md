# Scale and availability

V1 traffic is “founders + early guests + a handful of partners.” Design for **that**, then grow.

## What “up” means for you

| Target (honest V1) | Meaning |
| --- | --- |
| ~99.5% | A few hours a month of planned or small outages is acceptable |
| RPO ~ 24h | Daily backup: you may lose up to a day’s new requests |
| RTO ~ 1–2h | You can rebuild the VM from compose + restore Postgres |

When money is real, move RPO to hourly PITR (point-in-time recovery) — that is why managed Postgres is worth it.

## What actually needs to stay up

1. **Postgres** — if this dies, everything dies. Buy managed. Turn on daily backups + PITR.
2. **API + gateway** — two containers. Health: `GET /api/health`. Restart on failure (Compose `restart: always` or Container Apps).
3. **Website** — static files. If the API is down, show a calm error; do not invent stays.

Mail, Seq, Jaeger can be down without blocking browse. Outbox retries when SMTP returns.

## How you scale (in order, not all at once)

1. **Bigger VM / more CPU on the API** — cheapest first step.
2. **CDN for images and the JS bundle** — listings already use remote image URLs.
3. **Read replicas** — only when catalog GET is hot. Catalog is read-heavy and a good first replica.
4. **Split Payment out** — only if webhooks or PCI force it. The port is already there.
5. **Second region** — only after you have paying traffic in two geographies and a failover runbook.

Do **not** add Redis until you measure a real cache need (session is JWT; catalog is small).

## Availability habits that cost almost nothing

- One uptime check on `/api/health` (Better Stack, UptimeRobot, Azure).
- Disk alerts on the VM.
- Do not deploy on Friday night without a rollback (`docker compose` previous image).
- Keep gateway and API **stateless**. Sessions live in JWT + Postgres. You can run two API replicas behind one gateway when you need them.

## Load this design already handles

Hundreds of concurrent browsers browsing 14–50 retreats is fine on a small VM. The dangerous spikes are **login brute force** and **lead spam** — rate-limit those on the gateway — not homepage reads.
