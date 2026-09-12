# Working agreement

## Roles

| Role | Agent skill | Output |
| --- | --- | --- |
| Product Owner | `healingram-product-owner` | Backlog, flows, prerequisites, UAT |
| Software Developer | `healingram-software-developer` | Story branch, code, unit tests, PR |
| QA | `healingram-qa` | QA notes, pass/fail |

## Cadence for every story

1. PO confirms the story is ready (prerequisites for its epic are listed).
2. Developer branches from `feature/v1-iteration-1`.
3. Developer implements and adds unit tests.
4. Developer opens a PR into `feature/v1-iteration-1`.
5. CI builds and runs tests. Red CI is not reviewable as done.
6. QA tests the story.
7. PO performs UAT.
8. Only then merge.

## Language

- **V1** = launch blocking.
- **Later** = not this iteration.
- **Verified** = allowed to show as fact.
- **On request / estimated** = labeled, never dressed up as a firm total.

## Progressive next iteration

Design modules, events, and Docker stand-ins so the next iteration can replace Mailpit with a real ESP, Jaeger with a cloud APM, and a module with an extracted service — without a rewrite.
