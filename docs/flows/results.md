# Flow — All Retreats results

**Who:** a guest coming from Explore, a need, or a destination.

## Happy path

1. `/retreats` shows only **published** retreats from the catalog (any Indian state that has inventory).
2. Location filter: state groups from inventory; the guest selects a state and sees that state’s cities with counts. If they came with a need, location choices shrink to places that still have a match.
3. Filters apply immediately on desktop; on mobile a sheet shows the result count before Apply.
4. A card shows photo, name, city/state, verified tags, duration range, and a price only when verified — otherwise “Price on request”.
5. View Retreat (or the card) opens that retreat’s listing.

## Empty and error

- No exact intersection: do not sneak in unrelated retreats. Offer Clear Location, Clear filters, View all matching retreats, Talk to an Expert.
- Load failure: keep the filters, show Retry. Do not flash “No results” while loading.

## What they will not see

Zero-count locations, fake ratings, discounts, urgency, placeholder properties, personal data in the URL.
