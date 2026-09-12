# How we run a story (and how we save tokens)

## The loop

1. Open [CURRENT.md](CURRENT.md). It names one story file.
2. **You read that story file** using [REVIEW.md](REVIEW.md). Ask questions if needed. We change the file until you agree.
3. You say **go** (or “start this story”).
4. Agents implement **only that story** (Developer → CI → QA).
5. UAT is **on your laptop** (browser and/or `http://localhost:5080` / gateway).
6. We write a **Done** section in the **same** story file: what shipped, how to test locally, leftover risk.
7. We add the **next** story file in the same feature folder (or the next feature’s folder) and point `CURRENT.md` at it.
8. You read the next file. Repeat.

Do not start implementation of story N+1 in the same breath as story N.

---

## How to avoid one giant chat eating all the tokens

The chat is a **bad** source of truth. The **story file + this pack + the skills** are the source of truth.

**Do this:**

| When | What you do |
| --- | --- |
| New story | **Start a new Cursor chat.** Paste only the block below. |
| Same story, small question | Stay in the current chat. |
| Chat feels slow or confused | New chat. The files still have the context. |

**Paste into a new chat (copy as-is, change the path):**

```text
You are working Healingram V1.

Read, in this order, and do not require earlier chat history:
1. docs/engineer/README.md
2. docs/engineer/HOW-TO-RUN-A-STORY.md
3. docs/engineer/CURRENT.md
4. The story file CURRENT.md points to
5. The matching skill: .cursor/skills/healingram-software-developer/SKILL.md
   (or product-owner / qa if I named that role)

Then do only that story. When finished, update the story file’s Done section
and CURRENT.md. Do not implement the next story until I say go.
```

Why this works:

- Project **skills and rules** load the standing process (branch names, modular monolith, no client-paid, inventory-driven geography).
- The **story file** has acceptance, files to touch, and out of scope.
- The **repo** has the code. You do not need to re-paste THE-PLAN every time; the agent can open it if the story says so.

**Do not** keep one conversation from “hello” through every epic. That is how context windows fill with old UI debates and CI logs.

---

## What “done” looks like in the story file

Every story file has:

- Status
- What / why
- Acceptance
- Out of scope
- Local how-to-test
- **Done** (empty until shipped)
- **Next** (link to the following story file)

After implementation, Done must be specific: commands, URLs, what changed. Not “implemented.”
