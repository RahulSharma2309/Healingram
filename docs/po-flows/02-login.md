# Login and signup

**Who:** guest, partner, or admin.

## What they want

A session that knows who they are, so requests, wishlist, and queues belong to them.

## What they do

1. Open `/login` or `/signup`.
2. Guest signs up with name, email, and an 8+ character password — or logs in with a known account.
3. After a **guest** login they land on **My dashboard**.
4. After a **partner** login they land on **`/vendor`** (their availability queue).
5. After an **admin** login they land on **`/admin`**.
6. Wrong password: a clear “email or password is not right.” No session.
7. Same email signed up twice: “already registered.”

## Local demo accounts

Password for all: `Local123!`

| Email | Role | Lands on |
| --- | --- | --- |
| `guest@local.test` | customer | `/dashboard` |
| `partner@local.test` | partner | `/vendor` |
| `admin@local.test` | admin | `/admin` |

A guest cannot type “admin” into a hidden field and become admin. Role comes from the server.
