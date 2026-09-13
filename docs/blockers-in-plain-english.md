# Blockers in plain English

This is the easy version of [leadership-review.md](leadership-review.md).

Each **blocker** is something that stops a public website. We go one by one. For each we say:

- what it is, in simple words
- **is it meant for the laptop right now?**
- **must it be gone before real guests?**

## How to read “intended or not”

| We say | Meaning |
| --- | --- |
| **Yes — laptop only** | Built on purpose so you can click the product at home. Not a live-site feature. |
| **No — even on the laptop this is a mistake** | Nobody asked for this. It is a bug or leftover copy. |
| **Mix** | The *idea* is for local testing. The *way it was built* went too far. |

**None of these are OK on a public Healingram.com.** “Intended for now” only means “we built this so the laptop demo works.”

---

## 1. A customer can open Admin and Vendor

**What happens**

You open the website as a guest or as a normal customer. At the top there is a green bar: “Proposal MVP” plus links **Vendor panel** and **Admin panel**. The header also says “For retreat partners.” Anyone can click those, or type `/vendor` or `/admin` in the address bar. The staff screens open. Nobody asks “are you a partner?” or “are you admin?”

The server usually **refuses** the real “confirm this stay” buttons if you are only a customer. So this is not always “they can run the whole business.” They can still **see** the staff screens, and on the same computer they can see request details that were saved in the browser.

**Intended for now?**

**Mix.**

| Part | Intended? |
| --- | --- |
| One website with three areas (guest, partner, admin) so you can demo everything on one laptop | **Yes — laptop only.** That is how the prototype was built. |
| A banner that jumps you into Vendor and Admin without logging in | **Yes — laptop only.** We wanted the founder to click around fast. |
| A customer who is logged in still being able to walk into Admin / Vendor | **No.** A real marketplace never does this. The product rule is: a guest is a guest. Role comes from the server. |
| Putting “Vendor panel” and “Admin panel” on the public homepage | **No** for a live site. Fine as a private demo shortcut. Wrong if strangers can see it. |

**Before real guests?** **Must fix.** Lock the doors. A customer should get “you cannot open this page.” Partner and admin links should not sit on the guest site.

---

## 2. Anyone can mark a stay as “paid”

**What happens**

Healingram is supposed to take money only after the retreat says yes. On a real site, a payment company (like Razorpay) would send a signed “this payment is real” message. Today there is **no** real payment company. There is a **fake** “payment message” door. The password for that door is written in the project and also inside the website code. The admin screen has a button that presses that door. So a person who can open Admin (see blocker 1) can make the system say **paid** even though no money moved.

**Intended for now?**

**Mix.**

| Part | Intended? |
| --- | --- |
| No real Razorpay / UPI yet. A fake “paid” path so you can test the journey on the laptop | **Yes — laptop only.** The product doc says V1 will not do live Razorpay. |
| Only a server message (not the “thank you” page) can set paid | **Yes.** That rule is correct. Keep it. |
| The fake-door password sitting in the website that every visitor downloads | **No.** Even for a demo, that is too open. If the site is on the internet, a stranger can mark stays paid. |
| Admin picking “PAID” from a dropdown in the browser | **No.** The product rule is: **admin must not mark paid.** |

**Before real guests?** **Must fix.** Real payments, or do not take money. Remove the fake door from any public site. Admin cannot click “paid.”

---

## 3. The “OTP” is a printed number that can log you in as anyone

**What happens**

When a guest wants to see their request, we ask them to “verify.” On a real site we would send a one-time code to their phone or email. Today we do **not** send a code. The screen tells you to type **560142**. The server always accepts that number.

If you type that number with **any** email or phone that already exists — including `admin@local.test` — the system can give you that person’s login. So “check my request” can become “I am now the admin.”

**Intended for now?**

**Mix.**

| Part | Intended? |
| --- | --- |
| A fixed test code so you can verify on the laptop without SMS | **Yes — laptop only.** Testers need a way in. |
| Showing `560142` on the page | **Yes — laptop only.** So UAT is easy. Must never appear on a live site. |
| That same code working for admin, partner, or any customer account | **No — even on the laptop this is a mistake.** Guest verify should only open a **guest** request. It should never become staff. |

**Before real guests?** **Must fix.** Real SMS or email codes. Guest path stays guest-only. Remove `560142` from the live site and from public docs.

---

## 4. Demo logins, demo password, and “one partner owns every retreat”

**What happens**

The login page (and the docs) print three accounts:

- `guest@local.test`
- `partner@local.test`
- `admin@local.test`

Password for all of them: **Local123!**

When the app starts, it often **creates** these users if they are missing. It also loads a list of retreat names and gives **all** of them to the demo partner. Anyone who knows the password (it is written in public files) can be admin or the “owner” of the whole catalog.

**Intended for now?**

**Yes — laptop only.**

This is the local demo kit. You need a guest, a partner, and an admin so you can click the full story without hiring real retreats.

| Part | Intended? |
| --- | --- |
| Seed users and a seed catalog on **your computer** | **Yes — laptop only.** |
| Printing the admin password on the live login page | **No** for any public URL. |
| Those users still being created if you deploy the app as-is | **No.** The code does not turn them off by itself. That is the danger. |

**Before real guests?** **Must fix.** Turn seed users off on the live server. Change all secret keys. Do not print passwords on login.

---

## 5. The site says it is a prototype — and also invents “facts”

**What happens**

Two different problems sit together.

**A. The honest banner.** The green bar says this is a UX prototype and bookings are not real. That is true today.

**B. The dishonest pages.** Other pages talk as if Healingram is already live:

- “Verified wellness retreats”
- FAQ: pay online and get **instant** confirmation
- FAQ: **Razorpay / Stripe** is on in production
- FAQ: free cancel 7 days before
- Made-up guest stories (Priya, Arjun, Meera) shown as real, consented reviews
- A fake Rishikesh booking voucher
- A `/therapies` page with invented retreat counts
- Real retreat **brand names** in the seed list, with stock photos, as if they already listed with you

The product rule is: never show an unverified price, story, or badge as if it were true.

**Intended for now?**

**Mix.**

| Part | Intended? |
| --- | --- |
| Banner: “prototype, no real payments” while you test on the laptop | **Yes — laptop only.** So nobody thinks the demo is a live shop. |
| A seed list of retreats so Browse and Find My Match have something to show | **Yes — laptop only.** Empty catalog = you cannot test. |
| Showing those seed names, prices, and stories as **verified / real / live Razorpay** | **No.** That breaks the product rules. The FAQ is leftover hotel-style copy. The demo reviews are labelled “demo” in the code but still shown as real. |
| Using other companies’ names on a public site without a signed listing | **No.** Fine as private demo data. Risky as Healingram.com. |

**Before real guests?** **Must fix.** Turn the prototype banner off for the public. Remove or clearly mark demo stories. Fix FAQ to match the real product (ask first, pay after confirm). Only show retreats you have the right to list.

---

## Quick card (all five)

| # | Blocker | Meant for the laptop? | OK on a public site? |
| --- | --- | --- | --- |
| 1 | Customer can open Admin and Vendor | **Mix** — shortcuts yes, unlocked doors no | **No** |
| 2 | Fake “paid” anyone can trigger | **Mix** — fake pay for testing yes, open secret no | **No** |
| 3 | OTP is always `560142` | **Mix** — test code yes, staff takeover no | **No** |
| 4 | Demo users + `Local123!` | **Yes — laptop only** | **No** |
| 5 | Prototype banner + invented facts | **Mix** — banner and seed data yes, fake “verified” copy no | **No** |

---

## What is *not* a blocker — it is planned “later”

These are missing on purpose for V1. They are **not** bugs. They still mean you cannot run a full live marketplace yet.

| Later on purpose | Why it is OK to wait |
| --- | --- |
| Real Razorpay / Stripe | Written in the product: V1 will not do live card/UPI |
| Partner “create a retreat” screen | Written in the product: no CMS in V1 |
| Auto payouts to retreats | Later |
| Ratings, featured lists, guest-favourite | Later — and we must not fake them |
| WhatsApp Business automation | Later |
| Big-company extras (SSO, two regions, SOC 2) | Not this stage |

**Do not confuse “later” with “broken.”** Blockers 1–5 are either laptop shortcuts or mistakes. The list above is scope we already said we would not ship in V1.

---

## One-line summary

The laptop is meant to feel like the whole product: three panels, a test login, a test OTP, a fake payment, and sample retreats.

**That is intended for now.**

What is **not** intended: a customer walking into Admin, a printed code that becomes the admin, a payment password in the website, and pages that claim verified partners and live Razorpay.

Until those are fixed, keep Healingram on the laptop. Do not point a customer domain at it.
