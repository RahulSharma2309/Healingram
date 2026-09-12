# Engineer pack (read this folder)

This folder is for **you** — a senior engineer who is also the product owner. It is meant to be **read**, then you tell the agents to go. Nothing here requires memorizing the chat.

**Start:** [THE-PLAN.md](THE-PLAN.md) → [PRODUCTION-AND-TOOLS.md](PRODUCTION-AND-TOOLS.md) → [HOW-TO-RUN-A-STORY.md](HOW-TO-RUN-A-STORY.md) → [REVIEW.md](REVIEW.md) → [CURRENT.md](CURRENT.md) → that story file.

| File / folder | What it is |
| --- | --- |
| [THE-PLAN.md](THE-PLAN.md) | Product + architecture + agents, including your overrides |
| [PRODUCTION-AND-TOOLS.md](PRODUCTION-AND-TOOLS.md) | Laptop tools **and** production options (GitHub, cloud, K8s vs a server) |
| [HOW-TO-RUN-A-STORY.md](HOW-TO-RUN-A-STORY.md) | Review → go → done notes → next story. Also: how to start a **new chat** so we do not burn tokens |
| [REVIEW.md](REVIEW.md) | Before/after checklist you use as the senior engineer |
| [STORY-TEMPLATE.md](STORY-TEMPLATE.md) | Shape of every story file |
| [epics/](epics/) | One folder per epic, one per feature; full story files only when that story is current |
| [CURRENT.md](CURRENT.md) | Pointer to the story to read next |
| `ui-reference/` at repo root | The existing demo UI. We copy screens from it; we do not treat it as the live app forever |

The numbered backlog table remains in `docs/product/backlog.md`. **Story truth** (acceptance, done notes, next story) lives in this pack so you can review before and after without hunting.

---

## Your overrides (12 September 2026)

These beat the written spec where they conflict:

1. **Geography is inventory-driven.** Any Indian state/city that has published retreats can appear. The location filter is: states from inventory → expand cities under the selected state (as in your screenshot). Not Karnataka/Kerala-only.
2. **Published inventory is the ceiling**, not a hard-coded list of 14 names. Unpublished stays hidden.
3. **UAT is local** until you choose to deploy.
4. **Progressive UI:** keep the current demo as `ui-reference/`. Each story lifts a piece of that UI into the real app and puts a real API behind it. When the real app matches, we delete `ui-reference/`.
5. **One story file you read, then “go.”** After it ships, we write what was done in that file and drop the next story file in the same feature folder.
