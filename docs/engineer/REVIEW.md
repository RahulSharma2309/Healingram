# How you review (before and after)

You are the senior engineer and the PO. Agents write; you decide.

## Before you say go

Open the story file [CURRENT.md](CURRENT.md) points to. Check:

| Check | Pass means |
| --- | --- |
| One slice | You can UAT it on this laptop without the next epic |
| Geography | Places come from **published inventory**, not a state allow-list |
| Honest data | No unverified price / availability / testimonial as fact |
| UI | If the story touches UI, it names the `ui-reference` (or current `src/`) screen to copy |
| Out of scope | Next stories are not smuggled in |
| Local test | Commands and URLs you can run without a deploy |

If something is wrong, we edit **that markdown**. Then you say go.

## After it ships (same file)

The Done section must answer:

| Check | Pass means |
| --- | --- |
| What landed | Files / endpoints, not “implemented” |
| How you retest | Exact local commands |
| Honesty | Unpublished rows stay hidden; client cannot mark paid |
| Next | A **new** story file exists and CURRENT points at it |

Then you UAT locally and write pass/fail in `docs/uat/STORY-XX-YY-ZZ.md`.

## Tokens

Do **not** keep this conversation forever. New chat per story — paste is in [HOW-TO-RUN-A-STORY.md](HOW-TO-RUN-A-STORY.md).
