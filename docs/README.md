# Healingram docs

All planning, architecture, flows, prerequisites, QA, and UAT notes for V1 live here.

| Folder | Who writes it | What it is |
| --- | --- | --- |
| [product](product/) | Product Owner | Vision, backlog (EPIC → feature → story) |
| [flows](flows/) | Product Owner | User flows in PO language |
| [prerequisites](prerequisites/) | Product Owner | External systems and paid services per epic |
| [architecture](architecture/) | Developer + PO | Technical architecture, modules, observability, CI |
| [process](process/) | All | Working agreement and definition of done |
| [qa](qa/) | QA | Story test evidence |
| [uat](uat/) | Product Owner | Story UAT evidence |

Authoritative product spec (binary): `../Healingram_Developer_Functional_Specification_V1.docx`

## How we work

Understand → plan → technical requirements → implement one story → test → PR into `feature/v1-iteration-1` → merge only after CI + QA + UAT. Deploy is the last remaining step after every V1 epic is `done`.

## Current iteration

- Branch: `feature/v1-iteration-1`
- Start here: [product/backlog.md](product/backlog.md)
- Current first epic: [EPIC-00 Platform Foundations](prerequisites/epic-00-platform.md)
