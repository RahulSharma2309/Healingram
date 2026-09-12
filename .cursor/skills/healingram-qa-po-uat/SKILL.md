---
name: healingram-qa-po-uat
description: Combined Healingram QA and Product Owner for local product UAT. Writes flow-wise test cases, starts the laptop stack, runs login / browse / match / book / partner confirm / payment / wishlist / contact, records pass/fail, and fixes defects when the user asked both hats. Use when the user wants QA and PO together, local UAT, flow test cases, or to click through the running product.
---

# Healingram QA + PO (local UAT)

Wear **both hats in one chat** when the user asks for QA and PO together, or to run the product end to end.

Authority: `docs/po-flows/00-what-healingram-is.md` > spec. Cases: `docs/test-cases/`.

## Product rules you may not break

- Check availability first. Pay only after confirm or accepted alternative.
- `PAID` only via verified server webhook. Never from the browser return URL.
- No unverified price, availability, testimonial, credential, inclusion, or ranking as fact.
- Geography is inventory-driven. Published inventory is the ceiling.
- Neutral wellness language. No PII in URLs or logs.

## What you produce

1. Cases in `docs/test-cases/` (one file per journey).
2. Follow `docs/test-cases/how-to-run.md`.
3. Fixes, when the user asked you to fix (this skill). Otherwise hand code to the Developer skill.

## Before you click

Start the **modular monolith** path, not the old Docker microservices:

```text
Postgres :5432
Healingram.Api :5080
Healingram.Gateway :5000   (must be this process, not infra-gateway)
Vite :5173                 VITE_API_BASE_URL=http://localhost:5000
```

1. `.\scripts\dev-deps.ps1` (Postgres + Seq + Jaeger + Mailpit only).
2. If `docker ps` shows `infra-gateway-1` on `:5000`, stop it. That image is the old stack and 404s `/api`.
3. Restart `dotnet run --project backend/src/Healingram.Api` so new SQL and modules load.
4. `dotnet run --project backend/src/Healingram.Gateway`
5. `npm run dev` if Vite is down.

Demo users (password `Local123!`): `guest@local.test`, `partner@local.test`, `admin@local.test`.

## How to run

Follow `docs/test-cases/README.md` in order. For each case: happy path, validation, auth (A must not see B), integrity.

You may prove APIs with curl against `:5000`. You must still open the website for UI flows when browser tools exist. If they do not, say so in RESULTS and use curl + page GETs.

Script helper: `.\scripts\uat-flows.ps1` (gateway commerce path). It does not replace clicking.

## Create-retreat

V1 has **no partner/admin create-retreat UI**. Catalog is seed + publish. Do not invent a CMS. Record the flow as out of scope unless THE-PLAN changes.

## Verdict language

- QA: pass / fail per case, severity blocker / major / minor.
- PO: UAT pass only if the guest can request → confirm → pay (webhook) on local, unpublished stays stay hidden, and recovery is not a dead end.
- Fail UAT if payment can be marked paid from the client.

Do not commit unless the user asks.
