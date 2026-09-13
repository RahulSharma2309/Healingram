# Admin

**Who:** `admin@local.test` locally.

## What they do

1. Land on `/admin`.
2. See **all** pending availability requests (not only one partner).
3. Add an **internal note**. The guest never sees that note.
4. They may confirm a stay (admin is allowed to act like partner-write).
5. If they simulate payment locally, it must still go through the **server webhook** and secret.

## What they must not do

Mark paid by clicking a status. Read as a partner. Show internal notes on the guest request page.

Admin is operations. Admin is not the bank.
