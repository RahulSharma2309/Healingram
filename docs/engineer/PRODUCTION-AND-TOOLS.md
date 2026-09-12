# Tools — local now, production later

Two lists. You need the first to build. You need the second when you deploy. V1 does **not** need Kubernetes on day one.

---

## A. Laptop (already enough to build and UAT)

| Tool | Why |
| --- | --- |
| Cursor | Edit + agents |
| Git, GitHub account | Branches and PRs |
| Node.js 22+ | React / Vite |
| .NET SDK 10 | API and gateway |
| Docker Desktop | Postgres, Seq, Jaeger, Mailpit |
| Browser | Local UAT |

Docker stand-ins (free while developing):

| Tool | URL | Later replace |
| --- | --- | --- |
| Postgres 16 | `localhost:5432` | Managed Postgres |
| Seq | http://localhost:5341 | Cloud logs |
| Jaeger | http://localhost:16686 | Cloud APM / Tempo |
| Mailpit | http://localhost:8025 | Real ESP (SES, Postmark, ACS) |

---

## B. Production — what you will actually buy or sign up for

### 1. Source control

| Option | When |
| --- | --- |
| **GitHub Free private repo** | Fine for a single founder. Private repos are free. |
| **GitHub Team** | You want org SSO, required reviewers, or several people. Not required to go live. |
| GitHub Pro | Nice-to-have (advanced insights). Not a blocker. |

You do **not** need a paid “private licence” just to hide the code. Free private is enough until you hire.

CI: GitHub Actions minutes on a private repo are limited on Free; Team raises the cap. For this size of repo, Free is usually enough at the start.

### 2. Where the app runs (pick one shape)

You have a **modular monolith** (one API process + one gateway + static React). That is one or two containers, not a mesh.

| Shape | What it is | Verdict for V1 |
| --- | --- | --- |
| **One Linux VM** (Azure VM, AWS EC2, Hetzner, DigitalOcean) + Docker Compose | You SSH, `compose up`, nginx/Caddy for TLS | **Best first production.** Matches how we already run locally. Cheap. You understand it. |
| **Managed containers** (Azure Container Apps, AWS App Runner, Google Cloud Run, Fly.io) | You push images; they run and scale the process | **Best if you do not want to patch a VM.** Still not Kubernetes. |
| **PaaS** (Azure App Service) | Deploy the API and a static web app | Fine if you already live in Azure. |
| **Kubernetes** (AKS / EKS / GKE / a raw k8s box) | Cluster, ingress, HPA, YAML | **Do not use for V1.** You have one team, one API, no module extracted yet. K8s is an operations product. Take it when a module is split out and you need independent scale, or you already run a cluster for other work. |

**Recommendation:** start with **Docker on one VM** or **Container Apps / Cloud Run**. Keep the same images we already defined in `infra/docker/`. Moving to k8s later is wrapping those images in Deployments — not a rewrite — if we stay modular.

### 3. Database

| Option | Notes |
| --- | --- |
| **Managed Postgres** (Azure Database, RDS, Cloud SQL, Neon, Supabase Postgres) | **Use this in production.** Automated backup, TLS, upgrades. |
| Postgres on the same VM | Acceptable for a private beta if you snapshot disks. Weaker. |

Mongo / Redis: still **not** in V1 unless a story proves the need.

### 4. Edge and TLS

- Domain + DNS
- HTTPS: Caddy or nginx on the VM, or the cloud load balancer’s certificate
- Gateway container stays the public `/api` entry (or the host maps 443 → gateway)

### 5. Secrets and config

- Never commit production connection strings
- GitHub Actions secrets for CI
- On the host: environment variables or a secret store (Azure Key Vault, AWS Secrets Manager)

### 6. Observability in production (replace Docker toys)

| Signal | Dev | Production |
| --- | --- | --- |
| Logs | Seq | Azure Monitor / CloudWatch / Grafana Cloud / Seq Cloud |
| Traces | Jaeger | Same vendor’s OTLP endpoint (we already emit OTLP) |
| Email | Mailpit | SES, Postmark, Azure Communication, or your ESP |
| Uptime | Your eyes | A simple ping on `/api/health` (Better Stack, Azure, etc.) |

### 7. Payments and comms (when those epics land)

- Payment: Razorpay or Stripe **test** first; production after compliance
- WhatsApp Business API: **later**; V1 is save-lead-then-`wa.me`
- Object storage (S3 / Blob): only if we start storing partner docs

### 8. Backups and restore

- Daily automated Postgres backup
- One restore drill before you call it live

---

## C. Suggested first production bill (honest)

Minimum live stack:

1. GitHub private repo (Free)
2. One small VM **or** one Container Apps environment
3. Managed Postgres
4. Domain + TLS
5. An email provider when we send real mail
6. A payment test account when EPIC-09 starts

Skip until you feel pain: Kubernetes, Redis, Mongo, a second region, a service mesh.
