# Observability plan

We instrument now. We buy cloud tools later.

## What every feature must emit

| Signal | Dev / UAT (Docker) | Production later |
| --- | --- | --- |
| Logs | [Seq](http://localhost:5341) via Serilog | Cloud log store (same structured fields) |
| Traces | [Jaeger](http://localhost:16686) via OTLP | Cloud APM / Tempo |
| Email | [Mailpit](http://localhost:8025) | ESP (SES, Postmark, …) |
| Health | `/health` on gateway and API | Same, scraped by the host |

## Correlation

- Gateway creates `X-Correlation-Id` if missing.
- API and modules log and span with that id.
- Frontend may send the id on retries; it must never put PII in query strings.

## Never log

Phone, email, customer notes, wellness free text, raw card data, webhook secrets.

## Telemetry product view (planned, not built this hour)

A later hardening story can add a simple **ops status** page for admin: request rates, failed webhooks, aging availability requests. For V1, Seq + Jaeger + the admin queue are enough to operate locally.
