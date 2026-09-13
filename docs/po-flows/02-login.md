# Login and signup

**Who:** guest, partner, or admin.

## What they want

A session that knows who they are, so requests, wishlist, and queues belong to them.

## What they do

1. Open `/login` or `/signup`.
2. Guest signs up with first name, last name, an India `+91` phone (they type only the 10 digits), email, password, and confirm password. Address is optional. Clicking **Sign up** shows every client-side problem at once and does not call the server until the form is clean. The server uses the same rules: names, exactly 10 Indian mobile digits, email format, and a password with 8+ characters plus a letter, a number, and a special character. The (i) next to Password lists those rules.
3. After a **guest** login or signup they land on **My dashboard**.
4. After a **partner** login they land on **`/vendor`** (their availability queue). The server issues that session only if they have an **active partner membership**. Partner role without membership, or a revoked/suspended membership, is refused. Admins may open the vendor portal for support without a membership.
5. After an **admin** login they land on **`/admin`**.
6. Wrong password: a clear “email or password is not right.” No session.
7. Same email signed up twice: “already registered.”
8. On **My dashboard → Profile** they can edit first name, last name, phone, email, and optional address. Password stays on signup/login only.

## Local demo accounts

Password for all: `Local123!`

| Email | Role | Lands on |
| --- | --- | --- |
| `guest@local.test` | customer | `/dashboard` |
| `partner@local.test` | partner | `/vendor` |
| `admin@local.test` | admin | `/admin` |

A guest cannot type “admin” into a hidden field and become admin. Role comes from the server.
