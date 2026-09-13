# Wishlist and My Trips

**Who:** a logged-in guest.

## Wishlist

Header **Wishlist** is only shown after a password login. They tap a heart on a published retreat. It stays after refresh. Un-heart removes it. Login can merge hearts they saved as a guest on that browser.

## My Request (no account)

Header **My Request** while logged out opens `/my-request` (email or mobile + OTP). A **guest** sees only their request. A **registered** contact who uses OTP still has a scoped guest-request token — they are asked to sign in for the dashboard. Logged-in **My Request** (password session) goes straight to the dashboard.

## My Trips

Header **My Trips** (logged-in only) opens `/dashboard?tab=trips`. **Wishlist** opens `/dashboard?tab=wishlist`. The profile avatar opens `/dashboard?tab=profile`. `/dashboard` redirects to login when they are not signed in.

Groups:

- **Payment pending** — confirmed, not yet paid.
- **Upcoming** — paid (webhook succeeded).
- **Completed** — empty in V1 until a real “stay finished” rule exists.
- **Cancelled** — dates marked unavailable.

They only see **their** rows. A guest request appears here after they verify (`560142` locally) or create an account with the same email/phone.

## What we will not do

Show another customer’s trip. Invent a completed stay.
