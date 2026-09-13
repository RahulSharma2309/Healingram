# How you do the next app (from scratch)

Use this as a checklist. Healingram-specific names are only examples.

## Day 0 — you, before any agent

Write one page (even in Notepad):

1. **Who** uses it (three roles max for V1).
2. **The one happy path** in one paragraph.
3. **Three hard rules** (ours were: check availability first; webhook marks paid; no invented prices).
4. **What V1 will not do.**
5. **Shape:** one app + one API + one database, unless you already know you need more.

If this page does not exist, the agent will invent a product.

## Day 1 — make the agents

In the new repo:

1. Create `.cursor/skills/<name>/SKILL.md` for at least:
   - **Owner** — product language, rules, “do not implement unless asked”
   - **Builder** — one slice, tests, no extra features
   - **Tester** — cases, evidence, fail on integrity
2. Create `.cursor/rules/<project>.mdc` with `alwaysApply: true` for the hard rules and the folder that is source of truth.
3. Put `AGENTS.md` at the root listing those skills.

A skill should be **short**. It is a job description, not a novel. Point it at your docs.

You can copy the Healingram skills and **change the product name and rules**. Do not copy our retreat tables into a different business.

## Day 2 — contract before screens

Write one file: public URLs the website will call. Example: `POST /api/auth/login`.

Every builder chat must read that file. Two chats must not invent two login URLs.

## Day 3 — thin vertical slice

Do **not** build all modules empty. Build **one** path that hits the database:

`UI button → gateway → API → table → JSON back`

For Healingram that was health, then catalog needs, then login. For your next app it might be “create account” or “create order.”

Run it. If you cannot curl it, it is not done.

## Day 4 — one journey at a time

For each journey:

1. Owner writes the feeling (plain language).
2. **New chat**, Builder implements **only that**.
3. Tester writes cases and runs them.
4. You click once.

New chat per journey (or when the chat feels lost). Paste: “Read `docs/…` and skill X. Do not expand scope.”

## Day 5 — when you want speed

If you say “finish the local product”, use an **orchestrator** skill: waves, shared contract, no waiting for per-story go. You accept more uncommitted code and you **must** review the running app.

Never let bulk mode skip: webhook-only paid, no PII in logs, no invented catalog.

## How to talk to agents (phrases that work)

| Say this | Not this |
| --- | --- |
| “You are the PO. Update only `docs/po-flows/02-login.md`.” | “Make login nicer somehow.” |
| “You are the developer. Implement payment webhook idempotency. No UI redesign.” | “Do payment and also the homepage.” |
| “Fail if the browser can mark paid.” | “Make sure payment is secure.” |
| “Prove with curl against `:5000`.” | “I think it works.” |

## How to create a new skill in Cursor

1. Folder: `.cursor/skills/my-role/SKILL.md`
2. Top of file:

```yaml
---
name: my-role
description: What it does and when to use it. Include trigger words.
---
```

3. Body: steps, files it may touch, things it must not do.
4. Mention it in `AGENTS.md`.

That is enough. You do not need a plugin.

## Habits that saved us

- **Demo users in the docs** (`guest@local.test` / `Local123!`).
- **One compose for Postgres**, app processes on the host while developing (so you can restart the API after code changes).
- **Know who owns port 5000.**
- **Delete or archive on-the-way markdown** when the product is locally whole — that is this pack.

## Habits that burned tokens

- One giant chat from “hello” to every epic.
- Agents rewriting the plan instead of the current slice.
- Keeping two backlogs (prototype playbook + new spec) without saying which wins.

## After local V1

You are here now: app runs on the laptop. Next is **your** click, then [../engineering/deploy.md](../engineering/deploy.md) and [../engineering/cost.md](../engineering/cost.md). Agents can help write Docker and GitHub Actions. They cannot take KYC at Razorpay for you.
