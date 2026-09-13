# Blockers in plain English

Easy version of [leadership-review.md](leadership-review.md). Updated 13 September 2026 against the **current** app.

| We say | Meaning |
| --- | --- |
| **Yes — laptop only** | Built so you can click the product at home. |
| **No — even on the laptop this is a mistake** | A bug. |
| **Mix** | The idea is for local testing; the live site must not do this. |
| **Fixed on this branch** | The old review was right then; the code is different now. |

None of the laptop-only items are OK on a public Healingram.com.

---

## 1. Admin and Vendor doors

**Now:** `/vendor` and `/admin` ask you to log in. A customer token cannot confirm stays. A partner without an **active membership** cannot get a vendor session. The green demo bar with staff links shows only when `VITE_DEMO_MODE=true` (local Vite turns that on; production builds default off).

The footer still says “Partner with Healingram.” That opens vendor **login**, not an unlocked ops screen.

**Intended for now?** Yes — one website, three portals, for the laptop.  
**Before real guests?** Use separate hosts if you want. Keep the guards. Keep demo links off.

**Old review:** “anyone can open Admin.” **Fixed on this branch.**

---

## 2. Anyone can mark a stay paid

**Now:** The browser cannot set paid. Only a webhook (local fake door in Development) can. Production **refuses** to start if that fake door is on, or if the webhook password is the one in git.

**Intended for now?** Fake paid path on the laptop: **yes**. Fake door on the public internet: **no**.  
**Before real guests?** Razorpay (or similar) and no simulate flag.

---

## 3. OTP is a printed number

**Now:** On the laptop, Development can show code `560142` when demo mode is on. The server still goes through `IOtpProvider`. The code is bound to that request. It issues a **guest request** token, not “you are now admin.” A registered email cannot be turned into a new guest on create. Using OTP on a registered email still only opens that request — they must **sign in** for the full account.

**Intended for now?** Printed code: laptop only.  
**Before real guests?** Twilio or MSG91. Never print codes.

**Old review:** “type 560142 + admin@ and you are admin.” **Fixed on this branch.**

---

## 4. Demo users and seed retreats

**Now:** Development can seed `guest@` / `partner@` / `admin@local.test` and a demo catalogue. Production refuses those seed flags.

**Before real guests?** Real listings, real passwords, seeds off.

---

## 5. Tokens in the browser

**Now:** Login tokens sit in `sessionStorage` so the Vite app can call the API. That is transport, not a second database. A stolen XSS script could still read them.

**Before real guests?** Prefer HttpOnly cookies / a small BFF. Not a reason to rip out working login this week.

---

## 6. No real payment / OTP / inventory company

**Now:** The plugs exist. The real companies are not wired. Production will not start on a pretend provider unless someone sets a dangerous allow flag.

**Before real guests?** Must add the real adapters. That is the next programme, not a hidden leftover.

---

## Quick table

| # | Topic | Laptop? | Live site? |
| --- | --- | --- | --- |
| 1 | Staff portals | Guards on; demo links local | Demo links off |
| 2 | Fake paid webhook | Development only | Forbidden |
| 3 | OTP `560142` | Demo only | Real SMS/email |
| 4 | Seed users / catalogue | Development | Off + real data |
| 5 | `sessionStorage` tokens | OK for UAT | Harden later |
| 6 | Local providers | Yes | Real providers |

---

## Closed leftovers (do not re-open as if they were still true)

- Unguarded `/admin` and `/vendor` shells  
- `localStorage` business database  
- Guest OTP privilege escalation to admin  
- Frontend downloading every retreat to filter or to decorate matches  
- Backend inventing needs/themes when Postgres is empty  
- Vendor JWT from “has partner role” without membership  
