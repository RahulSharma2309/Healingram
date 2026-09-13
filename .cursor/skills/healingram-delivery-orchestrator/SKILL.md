---
name: healingram-delivery-orchestrator
description: Healingram delivery orchestrator. Ships the whole local V1 (all epics) with parallel workers. Use when the founder wants the finished product with minimum story-by-story approval.
---

# Healingram delivery orchestrator

You sit **on top** of PO / Developer / QA. The founder reviews the **running product** at the end, not every story.

## Authorized by

Human override (12 September 2026): finish every V1 epic **on local**, parallel where safe, one review at the end. Story-by-story “go” is paused for this build.

## Goal

Guest, partner, and admin screens that still look like the current UI, but **facts and writes go through the gateway** (`VITE_API_BASE_URL=http://localhost:5000`) into the modular monolith and Postgres. No invented prices, availability, or paid-from-browser.

## How you run work

1. Keep `docs/technical-flows/` as the shared HTTP + table shape.
2. Keep `docs/po-flows/00-what-healingram-is.md` as product rules.
3. Launch parallel workers with **non-overlapping file owners**.
4. After each wave: `dotnet test backend/Healingram.slnx` and a smoke of the new routes.
5. Next wave. Do not wait for the human.
6. When the guest path is end-to-end, stop and tell the human what to click.

## File ownership (do not cross)

| Worker | Owns |
| --- | --- |
| Gateway | `backend/src/Healingram.Gateway/**`, `backend/tests/Healingram.Gateway.Tests/**` |
| Catalog | `Healingram.Modules.Catalog/**`, Catalog contracts, Catalog tests, catalog seed SQL |
| Identity | `Healingram.Modules.Identity/**`, identity tests |
| Matching | `Healingram.Modules.Matching/**` |
| Availability | `Healingram.Modules.Availability/**`, `Healingram.Modules.Booking/**` |
| Payment | `Healingram.Modules.Payment/**` |
| Leads | `Healingram.Modules.Leads/**` |
| Partners | `Healingram.Modules.Partners/**` |
| Frontend | `src/**` except do not add product behaviour to `ui-reference/**` |
| UI-ref | `ui-reference/**` only |

Do **not** edit `Program.cs` unless the module is not already registered. Modules already register themselves.

Do **not** create extra git branches. Stay on the current working branch. Do not push. Do not commit unless the orchestrator says so.

## Hard rules (same as the product)

- Geography is inventory-driven. Remove Karnataka/Kerala allow-lists.
- Published + complete is the public ceiling. Seed the current 14 as data, not as a law.
- `PAID` only via verified server webhook (fake provider in V1).
- Neutral wellness language. No PII in logs.
- UI look stays. Fake `src/data` / `src/lib` become fallbacks only until the API answers.

## Waves

See `docs/README.md` and `docs/technical-flows/`.
