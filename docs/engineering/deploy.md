# From this laptop to real customers

You already have the **shape** of production: static website + gateway + API + Postgres. Deploy is packaging that, not inventing a new system.

## Do this in order

1. **You click the local product** and say it is good enough.
2. Commit and push the iteration (private GitHub is enough).
3. Buy a **domain** (`healingram.com` or similar) and point DNS.
4. Put **Postgres on a managed service** (backups on by default).
5. Run **gateway + API** as containers (or two processes on one VM) behind HTTPS.
6. Host the React **build** (`npm run build`) on the same host, CloudFlare Pages, or blob+CDN. Point `VITE_API_BASE_URL` at `https://your-domain/api` or `https://api.your-domain`.
7. Replace Mailpit with a real mail sender (Amazon SES, Postmark, Azure Communication).
8. Replace the fake payment webhook with **Razorpay test**, then live, after KYC.
9. Turn on uptime ping on `GET /api/health`.
10. Do **one backup restore drill** before you invite paying guests.

## Recommended first shape (simplest)

**One Linux VM** (DigitalOcean Droplet, Hetzner, Azure VM) + Docker Compose + Caddy or nginx for TLS.

Why: it matches `scripts/dev-up.ps1` / `infra/docker`. You can SSH. Cost is obvious.

**Alternative if you do not want to patch a VM:** Azure Container Apps, Google Cloud Run, or AWS App Runner for API+gateway, plus managed Postgres, plus static web host.

**Do not start with Kubernetes.** One API does not need a cluster.

## What must change in config

| Local | Production |
| --- | --- |
| `Jwt:Key` in appsettings | Long random key in a secret store |
| `local-dev-webhook-secret` | Provider webhook secret |
| `guest@local.test` seed | Disable demo users or change passwords |
| HTTP `:5000` / `:5080` | HTTPS 443 → gateway |
| Mailpit | Real SMTP/API |
| Seq/Jaeger on the laptop | Cloud logs + OTLP (we already emit OTLP) |

Never commit production connection strings. GitHub Actions secrets + host env vars (or Key Vault / Secrets Manager).

## Images you already have

`infra/docker/api.Dockerfile`, `gateway.Dockerfile`, `web.Dockerfile`. Build from the repo root. Multi-stage: no SDK in the runtime image.

## India-specific

- Host the VM in an India or nearby region (Mumbai / Singapore) so listing images and API feel fast.
- Payments: **Razorpay** is the usual first Indian card/UPI door. Keep the same intent + webhook design.
- Privacy: publish a real privacy page before you collect production leads. You already have `/privacy` as a shell.
