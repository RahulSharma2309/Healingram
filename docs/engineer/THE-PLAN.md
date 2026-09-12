# Healingram — the plan

Read this once. Then use [CURRENT.md](CURRENT.md) and the story files. This file records **your** decisions, not only the original spec.

---

## 1. What we are building

Healingram is a **programme-led retreat marketplace**.

A guest does not shop hotel rooms by the night. They pick a **programme**, a **duration**, **who they travel with**, and then they **ask the retreat if that stay is available**. Money moves only after the retreat (or Healingram staff) confirms the exact stay.

**Hard rule:** the website must never show an unverified price, availability, testimonial, expert credential, inclusion, or ranking as if it were a fact.

### Geography (your override)

We are **not** locked to Karnataka and Kerala.

- The site shows **whatever is published in the catalog**.
- **States** in filters and menus are derived from inventory (any Indian state that has at least one published retreat).
- When the guest picks a state, we **expand the cities** that exist under that state in inventory (your screenshot: Karnataka → Bengaluru & nearby, Devanahalli, … then Kerala as the next state group).
- Adding a retreat in Goa or Himachal later is a **data** change, not a product rewrite — as long as it is published and complete.

### Inventory (your override)

We are **not** permanently capped at 14 named retreats. Those 14 are today’s seed, not the law. Unpublished rows stay hidden.

The original spec said Karnataka/Kerala and 14 names. **You overrode that.** Agents must follow this file.

---

## 2. How we work

**Epic → feature → story.** One story at a time.

You read the story markdown. You say go. Developer builds. CI runs. QA writes evidence. **You UAT on local.** We write Done in that same file and drop the next story file.

Iteration branch: `feature/v1-iteration-1`.  
`main` waits until you say the iteration is finished.  
Deploy is after you can test the product **locally end to end**. You said you will not deploy first.

Detail of the loop and **new-chat / token rule:** [HOW-TO-RUN-A-STORY.md](HOW-TO-RUN-A-STORY.md).

---

## 3. Agents

| Hat | Skill | Job |
| --- | --- | --- |
| Product Owner | `.cursor/skills/healingram-product-owner` | Story files, flows, UAT notes |
| Software Developer | `.cursor/skills/healingram-software-developer` | One story, tests, PR |
| QA | `.cursor/skills/healingram-qa` | Evidence on local |

You start stories, UAT locally, and approve merges.

---

## 4. Tools

Laptop + production options: [PRODUCTION-AND-TOOLS.md](PRODUCTION-AND-TOOLS.md).

Short version: Node, .NET 10, Docker, GitHub. Production = private GitHub (Free is enough) + **one VM or managed containers**, **not Kubernetes for V1**, + managed Postgres.

---

## 5. How the software is shaped

```text
Browser
  →  Real app (we grow this from pieces of ui-reference)
  →  Gateway  (public /api, correlation id, later auth)
  →  Healingram.Api  (modular monolith)
       Identity | Catalog | Matching | Availability
       Booking  | Payment | Leads    | Partners
  →  One Postgres (one schema per module)
```

Modules do not join each other’s tables. Tomorrow a module can move out without a rewrite.

**Demo vs real app**

- `ui-reference/` — today’s clickable demo (what is on `main` today). **Reference only.** We lift screens from it.
- The live app we ship grows in `src/` (and later we can slim it). Each story copies the bit of UI it needs and points it at the gateway.
- When the real app matches the demo and is end-to-end, we **delete `ui-reference/`**.

The website talks only to `VITE_API_BASE_URL` (local: `http://localhost:5000`). It never talks to Postgres. It never marks payment paid.

---

## 6. Frontend ↔ backend (when a story is wired)

1. Guest clicks.
2. React calls `http://localhost:5000/api/...`.
3. Gateway stamps `X-Correlation-Id`, forwards to the API (`:5080`).
4. One module runs the rules and writes its schema.
5. JSON comes back. Seq/Jaeger show that id.

Unwired screens still behave like the demo until their story.

We use **minimal APIs** (routes on the module), not a pile of old `*Controller` classes. Same idea: URL in, JSON out.

**Already there (plumbing):** `/api/health`, `/api/meta`, `/api/catalog/needs` (temporary), and `/api/{module}/ready`.

**Product routes** are listed in each story file as that story needs them — not all invented up front in this page.

---

## 7. When V1 epics are finished

**Guest:** need or match → inventory-driven places → listing with honest price → request availability → confirm or alternative → pay (webhook only) → My Trips. Expert lead saved before WhatsApp.

**Partner / admin:** queues, audit, admin cannot mark paid.

**You as developer:** one repo, local compose or localhost + Docker deps, story PRs, green CI, story markdown with Done notes.

**You as PO:** same laptop, click the journey, say pass/fail.

---

## 8. Epic order

| Epic | Meaning |
| --- | --- |
| 00 | Platform (enough of it is already started) |
| 01 | Catalog + inventory-driven places — **first product epic you will read** |
| 02 | Login |
| 03–06 | Discovery, results, match, listing (UI lifted from `ui-reference`) |
| 07–09 | Request, partner/admin, payment-ready + webhook |
| 10–12 | Expert, account, hardening |

---

## 9. After you read this

Questions go against **this pack**. We edit these files when the plan changes.

When you are ready: open [CURRENT.md](CURRENT.md), review with [REVIEW.md](REVIEW.md), read that story, then say **go**.
