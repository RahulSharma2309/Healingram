# Approximate cost to run this for real customers

Figures are **order-of-magnitude, mid-2026**, in **USD and INR** (₹85 ≈ $1). Always re-check the vendor page before you buy. This is not a quote.

Healingram V1 is a small site: static React + two .NET processes + one Postgres. You should not be paying for Kubernetes.

## Three bills you will actually see

### A. Keep it on the laptop (now)

| Item | Monthly |
| --- | --- |
| Your time + electricity + Cursor | already paying |
| Docker Postgres / Seq / Mailpit | $0 |
| GitHub private repo (Free) | $0 |
| **Total to develop** | **$0 extra** |

### B. First public beta (recommended)

One small VM **or** a tiny container app, managed Postgres, a domain, TLS, cheap email.

| Item | USD / month | INR / month | Notes |
| --- | --- | --- | --- |
| Domain + DNS | ~$1–2 | ₹80–170 | `.com` is ~$12–15 / year |
| TLS | $0 | $0 | Let’s Encrypt / Caddy / Cloudflare |
| Compute (1–2 vCPU, 2 GB) | $6–24 | ₹500–2,000 | Hetzner / DigitalOcean Droplet / small Azure VM |
| Managed Postgres (1 GB) | $15–20 | ₹1,300–1,700 | DigitalOcean / Neon / Azure Basic. **Buy this.** |
| Container registry (if used) | $0–5 | ₹0–400 | GitHub Packages often free at this size |
| Email (SES / Postmark starter) | $0–15 | ₹0–1,300 | Mailpit cannot send to guests |
| Uptime ping | $0 | $0 | Free tier is enough |
| Logs (cloud, light) | $0–10 | ₹0–850 | Or keep a small Seq later |
| **Subtotal platform** | **~$25–75** | **₹2,100–6,400** | |
| Cloudflare (optional CDN/WAF) | $0 | $0 | Free plan is enough at the start |
| Razorpay | % of GMV | % of GMV | Not a monthly SaaS. See below |

**Plan on ~$40–80 / month (about ₹3,500–7,000)** to put a real URL on the internet with backups.

### C. Small live marketplace (real guests, still one region)

| Item | USD / month | INR / month |
| --- | --- | --- |
| 2–4 vCPU app + staging VM or second container | $24–60 | ₹2,000–5,100 |
| Postgres 2–4 GB + PITR | $30–60 | ₹2,500–5,100 |
| Email at volume | $15–30 | ₹1,300–2,500 |
| Error/APM | $0–30 | ₹0–2,500 |
| Object storage (partner docs, later) | $1–10 | ₹80–850 |
| **Platform** | **~$80–180** | **₹7,000–15,000** |

Add a **standby Postgres** (~2× DB) only when downtime would lose you more than that bill.

## Payment fees (this is the real “cost of sales”)

Razorpay (typical India card/UPI, **confirm on their site**):

- Roughly **2% + GST** on many card flows; UPI is often cheaper.
- On ₹50,000 of **successful** guest payments that is about **₹1,000 + tax**, not $80 of servers.

Your **server bill stays small**. Your **payment bill grows with GMV**. That is correct. Do not pick Kubernetes to “save” 2% fees.

## People / software you already use

| Item | Typical |
| --- | --- |
| Cursor / AI | whatever you already subscribe |
| GitHub Team | $0 until you hire and want SSO |
| Designer / extra engineer | not required to go live |

## What not to buy in year one

| Skip | Why |
| --- | --- |
| Kubernetes (AKS/EKS) | $70–300+ before your first guest |
| Multi-region active-active | You do not have that traffic |
| Mongo + Redis “because scale” | Extra failure modes |
| Enterprise support on day one | You can SSH |

## Honest yearly picture (beta → small live)

| Year-1 posture | Platform / year | Plus |
| --- | --- | --- |
| Careful beta | **$500–1,000** (₹40k–85k) | Domain, Razorpay KYC, your time |
| Small live | **$1,000–2,200** (₹85k–1.9L) | Payment % on real bookings |

That is the cost to **keep the lights on**. Marketing, retreat commissions, and your salary are separate.

## Suggested buy list (copy this)

1. GitHub Free private (done).
2. Domain.
3. DigitalOcean or Hetzner **or** Azure: one 2 GB VM.
4. Managed Postgres $15–20.
5. Caddy + Let’s Encrypt.
6. Amazon SES or Postmark.
7. Razorpay test account, then live after you click the local path yourself.
8. Free Cloudflare in front when the hostname is public.
