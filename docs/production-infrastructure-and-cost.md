# Production infrastructure and approximate cost

Prices change. These are **order-of-magnitude** figures for planning, not quotes. Check the vendor’s current public price page before budgeting.

## What we host vs buy

| Need | Service examples | Why | Current status | Future |
| --- | --- | --- | --- | --- |
| Frontend | Cloudflare Pages, Netlify, S3+CloudFront, Azure Static Web Apps | Static `dist/` | Local Vite / nginx image | CDN + HTTPS |
| API + gateway | One small VM or two containers (AWS ECS/Fargate, Azure App Service, Cloud Run) | Modular monolith | Local `dotnet run` / Compose | Same images |
| PostgreSQL | AWS RDS, Azure Database, Cloud SQL, Neon, Supabase | Source of truth | Local Docker | Managed + backups |
| Logs / traces | Seq Cloud, Grafana Cloud, Honeycomb, CloudWatch | Correlation IDs | Local Seq/Jaeger | Hosted |
| OTP | Twilio Verify, MSG91 | SMS/WhatsApp OTP | `LocalOtpProvider` | Real provider |
| Payments | Razorpay (India) | Cards/UPI | `LocalPaymentProvider` | Razorpay webhooks |
| Email | Mailpit local; later SES, Postmark, MSG91 email | Transactional | Outbox → Mailpit | Provider |
| Maps | None required in V1 | — | Place labels from DB | Optional later |
| Object storage | Optional for images | Retreat media URLs can be absolute HTTPS | Seeded URLs | S3/R2 if you host binaries |

WhatsApp: not required to launch. Platform setting `whatsapp.number` is a display value.

## Cost types

**Fixed infrastructure:** VM/containers, Postgres, CDN, log retain.

**Variable usage:** OTP SMS, payment MDR, email, egress, serverless invocations.

## Approximate monthly scenarios (USD, 2026 planning bands)

| Scenario | Compute + Postgres | CDN / frontend | Observability | OTP (variable) | Payments (variable) | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| Low-traffic MVP | $25–80 | $0–10 | $0–20 | tens of SMS | Razorpay MDR on GMV | Single region, one API instance |
| Growing product | $80–250 | $10–40 | $20–80 | hundreds of SMS | MDR + refunds | Backups, staging clone |
| Moderate production | $250–700 | $40–150 | $80–200 | usage-priced | MDR at scale | HA Postgres, two API replicas |

Razorpay: typically a **percentage + GST** of successful payments (see razorpay.com/pricing). Twilio/MSG91: **per SMS** (see twilio.com/sms/pricing and msg91.com). Cloud VMs/Postgres: see aws.amazon.com/rds/pricing, azure.microsoft.com/pricing, cloud.google.com/sql/pricing.

India GST applies to many of these invoices.

## Cloud choice

Any of AWS / Azure / GCP can run this. Pick the one where the founder already has billing and IAM. The app does not require a specific cloud API.

Related: [engineering/cost.md](engineering/cost.md), [engineering/scale-and-availability.md](engineering/scale-and-availability.md).
