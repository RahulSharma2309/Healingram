# Working agreement

## Roles

| Role | Agent skill | Output |
| --- | --- | --- |
| Product Owner | `healingram-product-owner` | Backlog, flows, prerequisites, UAT |
| Software Developer | `healingram-software-developer` | Story branch, code, unit tests, PR |
| QA | `healingram-qa` | QA notes, pass/fail |

## Cadence for every story

1. You read the story file in `docs/engineer/epics/` (see `docs/engineer/CURRENT.md`).
2. You say **go**. Prefer a **new Cursor chat** for that story (`docs/engineer/HOW-TO-RUN-A-STORY.md`).
3. Developer branches from `feature/v1-iteration-1`.
4. Developer implements and adds unit tests.
5. Developer opens a PR into `feature/v1-iteration-1`.
6. CI builds and runs tests. Red CI is not reviewable as done.
7. QA tests the story on **local**.
8. PO performs UAT on **local** (no deploy required).
9. Done notes go in the same story file. The next story file is written. Only then merge.

## Language

- **V1** = launch blocking.
- **Later** = not this iteration.
- **Verified** = allowed to show as fact.
- **On request / estimated** = labeled, never dressed up as a firm total.

## Progressive next iteration

Design modules, events, and Docker stand-ins so the next iteration can replace Mailpit with a real ESP, Jaeger with a cloud APM, and a module with an extracted service — without a rewrite.
