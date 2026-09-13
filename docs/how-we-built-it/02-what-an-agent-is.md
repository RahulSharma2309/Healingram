# What an agent is (and is not)

An **agent** in Cursor is not a separate employee. It is **this same kind of AI**, but given a **skill file** — a short `SKILL.md` that says:

- who you are (PO, developer, QA, orchestrator)
- what you may change
- what you must never do
- which files are the source of truth

When a chat “loads a skill”, it reads that file first and behaves like that role.

## The hats we used

| Hat | Skill file | Job |
| --- | --- | --- |
| Product Owner | `.cursor/skills/healingram-product-owner/SKILL.md` | What the guest feels; rules; UAT language |
| Software Developer | `.cursor/skills/healingram-software-developer/SKILL.md` | One slice of code + tests |
| QA | `.cursor/skills/healingram-qa/SKILL.md` | Prove the slice; write cases |
| QA + PO together | `.cursor/skills/healingram-qa-po-uat/SKILL.md` | Run the whole local product |
| Orchestrator | `.cursor/skills/healingram-delivery-orchestrator/SKILL.md` | Ship many slices in waves when you asked for “just finish it” |

Plus a **rule** that is always on: `.cursor/rules/healingram-delivery.mdc` (branch names, “spec wins”, modular monolith).

## What an agent is good at

- Turning a written story into code in the *shape you already chose*
- Writing the boring tests next to that code
- Searching the repo and wiring API ↔ screen
- Repeating a checklist (login, confirm, webhook)

## What an agent is bad at (you stay in charge)

- Knowing what you **want** if you never wrote it down
- Protecting a rule you never stated (we had to write “browser must not mark paid”)
- Clicking the site like a human unless you ask and tools exist
- Cost, taste, and “is this enough to launch?” — that is **you**

## Chat vs files

| If you only talk in chat | If you write files |
| --- | --- |
| Next chat forgets | Next chat opens the file |
| Two agents invent two APIs | One contract file |
| “Done” means “the model felt done” | Done means tests + your click |

Healingram’s contract lived in one place (`docs` now; it used to be `API-CONTRACT.md`). That stopped workers inventing different URLs.
