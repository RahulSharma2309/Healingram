# How we actually built Healingram

This is the real path, not a textbook.

## 1. You already had a picture

There was a React demo and a written spec (the `.docx`). You are the founder **and** the senior engineer. You overrode the spec in a few places:

- geography follows **inventory**, not only Karnataka/Kerala
- 14 retreats are **seed**, not a law
- UAT stays **local** until you can click it
- you asked to **finish the local product** in waves, not wait for “go” on every tiny story

Those overrides had to live in a file, or every new chat would “helpfully” lock the map to two states again.

## 2. We chose a shape before features

```text
Browser → Gateway → one .NET API (modules) → one Postgres
```

Not microservices. An older Docker gateway on port 5000 later proved why: two “gateways” cannot share a door.

## 3. We cut work into journeys, not random tickets

A guest must feel:

**browse → login → match → request → partner confirm → pay (webhook) → trips**

Each journey got a module (catalog, identity, availability, payment, …) and later a PO page + a technical page + test cases.

## 4. Two speeds of agents

**Story mode** (when you want control): new chat, one story, Developer → tests → QA.

**Bulk mode** (when you said “ship the laptop product”): orchestrator + parallel workers, same contract, you review the **running app** at the end.

Both speeds need the same rules. Bulk is faster and sloppier about git branches. That is a trade you chose.

## 5. Prove on the wire, not in a slide

We started Postgres, API, gateway, Vite. We hit real HTTP:

- login returns a JWT
- request returns `HR-2026-…`
- partner confirm writes booking `awaiting_payment`
- webhook with secret sets `paid`
- webhook without secret stays 401

If the site talked to the **wrong** process on port 5000, catalog 404’d. That is an ops lesson: **know which binary owns the port.**

## 6. Fix the lies the UI told

Agents had left “success” in `localStorage` when the server failed. That is worse than a crash. We changed confirm and request so the **server wins**. Partner login now lands on `/vendor`, not the guest dashboard.

That is PO + QA + developer in one pass — only because you asked for both hats **and** fixes.

## 7. What we did not pretend was done

- Create-retreat screen (out of V1)
- Real Razorpay
- Your own click-through
- Accessibility polish
- Production deploy

An honest agent writes those down. A sloppy one says “all done.”

## Timeline in one glance

```text
Spec + demo
    → plan + skills + rules
    → platform (API, gateway, Postgres)
    → catalog + login
    → website talks to gateway
    → request / partner / payment / leads / trips
    → local UAT script
    → this final docs pack
```
