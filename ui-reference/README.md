# UI reference (the demo)

This folder is the **frozen look** of the clickable demo: homepage, retreats, listing, login, partner, admin.

**Rule:** do not add new product behaviour here. Each story **copies** a screen (or a piece) into the live app and puts a real API behind that piece. You can still click the demo locally via `npm run dev` on the live app until screens are rewired.

When the live app matches this reference and is end to end, **delete this folder**.

Until [STORY-00-06-01](../docs/engineer/epics/EPIC-00-platform/FEAT-00-06-ui-reference/STORY-00-06-01.md) copies files here, the demo still lives in repo `src/` so `npm run dev` keeps working. Say **go** on that story if you want the snapshot filled first.
