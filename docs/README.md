# Healingram documentation

This folder is the **final V1 doc pack**. Start here. The old story-by-story notes have been removed.

Healingram is a **programme-led retreat marketplace**. A guest does not buy a hotel night. They pick a programme, ask if the stay is available, and pay only after the retreat (or Healingram) confirms.

| Folder | Who it is for | What you get |
| --- | --- | --- |
| [po-flows/](po-flows/) | You as Product Owner | How each journey feels, in plain language |
| [technical-flows/](technical-flows/) | You as engineer | Screen → API → Postgres table for each journey |
| [engineering/](engineering/) | You as founder-engineer | Stack, deploy, security, scale, **cost** |
| [test-cases/](test-cases/) | You testing locally | Cases you can tick pass/fail |
| [how-we-built-it/](how-we-built-it/) | You learning agents | How this app was built with Cursor agents, and how to do the next one |

Written spec (binary): `Healingram_Developer_Functional_Specification_V1.docx` at the repo root.  
Human overrides that beat that spec: inventory-driven geography, published inventory as the ceiling, local UAT first, no create-retreat CMS in V1.

## Local URLs

| What | URL |
| --- | --- |
| Website | http://localhost:5173 |
| Gateway (the site’s `/api` door) | http://localhost:5000 |
| API | http://localhost:5080 |
| Mailpit (local email) | http://localhost:8025 |

Demo password: `Local123!`  
Users: `guest@local.test` · `partner@local.test` · `admin@local.test`

How to start the laptop: [../README.md](../README.md).
