# Healingram documentation

This folder is the institutional memory of the current V1 implementation. Start here.

Healingram is a **programme-led retreat marketplace**. A guest does not buy a hotel night. They pick a programme, ask if the stay is available, and pay only after the retreat (or Healingram) confirms.

Canonical code: `src/` (React frontend), `backend/` (modular monolith), PostgreSQL (business data). `ui-reference/` is a **design-only** snapshot — not the production frontend.

Written spec (binary): `Healingram_Developer_Functional_Specification_V1.docx` at the repo root.  
Human overrides that beat that spec: inventory-driven geography, published inventory as the ceiling, local UAT first, no create-retreat CMS in V1, **webhook-only paid**.

Last reviewed: 13 September 2026, branch `feature/v1-final-foundation-release-readiness`.

## Where to start

| If you want… | Read |
| --- | --- |
| What the product is | [po-flows/00-what-healingram-is.md](po-flows/00-what-healingram-is.md) |
| Every user journey (21-section maps) | [product-journeys.md](product-journeys.md) |
| Screen → API → table | [technical-flows/](technical-flows/) |
| System picture | [architecture-overview.md](architecture-overview.md) |
| Modules | [architecture-modules.md](architecture-modules.md) |
| Tables | [database-schema.md](database-schema.md), [database-table-reference.md](database-table-reference.md) |
| Flow × tables | [database-flow-matrix.md](database-flow-matrix.md) |
| HTTP API | [api-reference.md](api-reference.md) |
| AuthZ | [engineering/security-and-authorization.md](engineering/security-and-authorization.md) |
| OTP / payment / inventory ports | [provider-architecture.md](provider-architecture.md) |
| End-to-end map | [system-traceability.md](system-traceability.md) |
| Clone and run | [local-development.md](local-development.md) |
| Env vars | [configuration.md](configuration.md) |
| Tests | [testing.md](testing.md) |
| Deploy | [deployment-guide.md](deployment-guide.md) |
| Cost | [production-infrastructure-and-cost.md](production-infrastructure-and-cost.md) |
| Ops | [operations-runbook.md](operations-runbook.md) |
| Public-launch honesty | [leadership-review.md](leadership-review.md) |

## Folder map

| Folder | Who | What |
| --- | --- | --- |
| [po-flows/](po-flows/) | Product | Plain-language journeys |
| [technical-flows/](technical-flows/) | Engineer | Screen → API → Postgres |
| [engineering/](engineering/) | Founder-engineer | Stack, deploy, security, scale, cost |
| [test-cases/](test-cases/) | Local UAT | Click-through cases |
| [how-we-built-it/](how-we-built-it/) | Agents / process | How this repo was built |

## Documentation inventory (no duplicates)

| Document | Decision | Notes |
| --- | --- | --- |
| `po-flows/00`–`10` | **KEEP + UPDATE** | Product language. Implementation maps live in `product-journeys.md` |
| `po-flows/11`–`14` | **KEEP** | Cancellation, inventory, notifications, identity |
| `technical-flows/*` | **KEEP + UPDATE** | Must match current APIs |
| `engineering/architecture.md` | **KEEP** | Short picture; detail in `architecture-overview.md` |
| `engineering/security.md` | **KEEP** | Checklist. Authoritative model is `security-and-authorization.md` |
| `engineering/security-and-authorization.md` | **KEEP** | Canonical auth |
| `engineering/deploy.md` | **KEEP** | Short path; detail in `deployment-guide.md` |
| `engineering/cost.md` | **KEEP** | Laptop/beta numbers; scenarios in `production-infrastructure-and-cost.md` |
| `engineering/stack.md` | **KEEP** | Tool versions |
| `leadership-review.md` / `blockers-in-plain-english.md` | **UPDATE** | Must describe *current* code, not 2026-early leftovers |
| New root `docs/*.md` listed above | **KEEP** | Single authority per subject |

Do not add a second “architecture.md” that contradicts `architecture-overview.md`.

## Local URLs

| What | URL |
| --- | --- |
| Website | http://localhost:5173 |
| Gateway | http://localhost:5000 |
| API | http://localhost:5080 |
| Mailpit | http://localhost:8025 |

Demo password (Development seed only): `Local123!`  
Users: `guest@local.test` · `partner@local.test` · `admin@local.test`

How to start: [local-development.md](local-development.md) or the root [README.md](../README.md).
