# What Healingram is

A guest chooses a **programme + duration + who they travel with**, then **asks the retreat if those dates work**. Money moves only after the retreat (or Healingram staff) confirms the exact stay.

## People

- **Guest** — browse, match, request, pay, wishlist, trips, contact.
- **Partner** — see requests for *their* retreats; confirm, suggest other dates, or say no.
- **Admin** — see all pending requests, add internal notes. **Cannot** mark a booking paid.

## Hard rules (do not break these)

1. **Check availability first.** There is no “buy now” from the listing.
2. **Paid only on the server.** The browser “payment success” page must never decide that money landed. Only a signed webhook (today: the local fake webhook with a secret) may set paid.
3. **No invented facts.** Never show an unverified price, availability, testimonial, credential, inclusion, or ranking as if it were true.
4. **Geography follows inventory.** Any Indian state/city that has a **published** retreat can appear. We are not locked to Karnataka and Kerala. A place with zero published stays stays hidden.
5. **Published inventory is the ceiling.** Today’s 14 names are seed, not a law. Unpublished rows stay hidden.
6. **Neutral wellness language.** No diagnosis, cures, or “medically recommended”.
7. **Sensitive text stays off the URL.** No phone, email, or wellness notes in query strings.

## What V1 will not do

Create-retreat CMS, live instant inventory, real Razorpay/Stripe, WhatsApp Business automation, featured/ranked lists, ratings, guest-favourite badges, automated partner payouts.
