# Healingram docs

**If you are the founder / senior engineer:** start in **[engineer/README.md](engineer/README.md)**. That pack is the living plan, tools, token rule, and per-story files.

The older folders below stay as product/architecture reference. Where they conflict with `docs/engineer/THE-PLAN.md`, the engineer plan wins (inventory-driven geography, local UAT, progressive UI).

| Folder | Who writes it | What it is |
| --- | --- | --- |
| [engineer](engineer/) | You + PO/Dev/QA agents | Plan, tools, review loop, **story truth** |
| [product](product/) | Product Owner | Vision, backlog index (same IDs) |
| [flows](flows/) | Product Owner | User flows in PO language |
| [prerequisites](prerequisites/) | Product Owner | External systems and paid services per epic |
| [architecture](architecture/) | Developer + PO | Technical architecture, modules, observability, CI |
| [process](process/) | All | Working agreement and definition of done |
| [qa](qa/) | QA | Story test evidence |
| [uat](uat/) | Product Owner | Story UAT evidence (local until you deploy) |

Authoritative product spec (binary): `../Healingram_Developer_Functional_Specification_V1.docx`  
Human overrides: [engineer/THE-PLAN.md](engineer/THE-PLAN.md)

## How we work

You read one story file → say go → implement that story → CI → QA → **local UAT** → write Done in the same file → write the next story. New Cursor chat per story ([engineer/HOW-TO-RUN-A-STORY.md](engineer/HOW-TO-RUN-A-STORY.md)).

## Current iteration

- Branch: `feature/v1-iteration-1`
- What to read next: [engineer/CURRENT.md](engineer/CURRENT.md)
- Backlog index: [product/backlog.md](product/backlog.md)
