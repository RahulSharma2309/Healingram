# Flow — Retreat listing (components 1–8)

**Who:** a guest deciding whether this programme is the one to request.

## Happy path

1. They open `/retreats/{slug}`. One shared stay is in play: programme, duration, dates, guests, occupancy, room, price.
2. They pick a programme. Duration options are only the ones that programme supports. A single duration preselects.
3. For a 7-night programme they pick a start date; checkout is calculated and read-only.
4. Price appears only when the tuple is valid and verified. Otherwise we say so.
5. They can open typical day, expert, story, and room drawers. Rooms are accommodation for the programme — not a separate hotel product.
6. Check Availability (sticky bar, programme card, mobile bar) all start the **same** request flow.

## Missing data

If we do not have experts or stories, those blocks are simply absent. We do not invent people or quotes.

## Recovery

Changing programme clears an incompatible room and tells them what changed. Past dates cannot be requested.
