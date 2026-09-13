# Security (what to take care of)

These are the things that would hurt guests or the business if you skip them.

## Must keep (already designed in)

- **Passwords hashed**, never stored plain (`identity.credentials`).
- **JWT** with short-lived access and revocable refresh.
- **Role on the server.** Partner/admin routes use policies. A customer token cannot confirm or read admin queues.
- **Webhook secret.** Paid is not a UI status.
- **Idempotency** on availability create and payment webhook — no double book / double pay from a retry.
- **PII out of URLs and logs.** Correlation id yes; phone/email/notes no.
- **Unpublished inventory hidden** on every public catalog GET.

## Must add before real customers

| Item | Why |
| --- | --- |
| HTTPS everywhere | Tokens and passwords on the wire |
| New JWT signing key | The local key is in the repo |
| Rotate / disable demo users | `Local123!` is public in docs |
| Managed Postgres + TLS + private network | API talks to DB on a private subnet, not `0.0.0.0` |
| Secrets not in git | Connection string, Razorpay key, SMTP |
| Gateway-level rate limits | API already partitions login / register / OTP / payment / `sensitive` by client IP. Gateway still has no limiter. |
| CORS only your website origin | Production already uses configured `App:*Url` origins only. Development still adds localhost |
| Security headers | HSTS, no sniff, tight referrer |
| Dependency alerts | GitHub Dependabot; we already have some NuGet advisories to watch |
| Backup encryption and a restore test | Ransomware / disk death |
| Admin 2FA (later, even a simple one) | Admin is the dangerous role |

## Payment

- Never trust `?status=success` from the browser.
- Verify amount, currency, and booking id on the webhook.
- Store provider event ids uniquely (`payment.webhook_events`).
- PCI: do **not** collect raw card numbers on your server. Razorpay/Stripe checkout handles that.

## Data / legal

- WhatsApp consent stored on the lead.
- Do not put wellness free text into analytics tools.
- When you have real partner contracts, keep them off this repo.

## What we are not doing in V1

WAF, bot farms, SOC 2, a second region. Add a WAF (Cloudflare in front of the VM is enough) when you have public traffic, not before the first guest.
