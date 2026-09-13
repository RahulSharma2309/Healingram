# Request availability (this is “booking”)

**Who:** a guest who is ready to ask “can we come?”

## What they want

To put a real request in front of the retreat **without paying yet**.

## What they do

1. On a listing, they choose programme, dates, guests, occupancy.
2. They tap **Check availability** (not Pay).
3. They enter name, email, phone (prefilled if logged in). Notes are optional and private. **No account is required** to submit.
4. Review says **no payment yet**.
5. Submit once. They get a public id like `HR-2026-10002` and a “request received” page. A guest customer row is created (or reused) for that email/phone.
6. Header **My Request** (and **View My Request** after submit) asks for the email or mobile they used on the request, then a code. Local UAT code is `560142`.
7. After a correct code the server issues a **guest request** token scoped to that public id (never admin/partner, never a password session):
   - **Guest** (never signed up): they see their request only. No Profile, Wishlist, or My Trips. They can come back and use OTP again, or sign up when they want.
   - **Registered customer** (already has a password account): they still only see **this** request. The page asks them to **sign in** for the full account / trips / wishlist. Creating a *new* request with a registered email/phone is refused (“sign in”).
   - Unknown contact: “No request for now” → home.
8. When the retreat confirms, the guest is asked **Yes, I want to book**. That is the force-signup point. Same email/phone **promotes** the guest — no second person. Then they can pay.
9. Profile, Wishlist, and My Trips stay hidden until they are a registered, logged-in customer.

## While they wait

My Request shows the snapshot they submitted. Prices on the website may change later; this request does not silently change.

## If something is wrong

Missing fields or an unpublished retreat: they see an error. The site must **not** pretend the request succeeded only in the browser.

## What this is not

It is not hotel checkout. Pay comes only after the partner (or admin) confirms, or after the guest accepts an alternative.
