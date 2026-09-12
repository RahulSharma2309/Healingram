# Payment

**Who:** a guest whose request is confirmed (or who accepted an alternative).

## What they want

To pay the **confirmed** amount, once, safely.

## What they do

1. Open the payment-ready page for that request id.
2. They see the frozen stay and the final amount the partner set.
3. Pay starts a **payment intent** on the server. If the stay is still only “requested”, pay is refused.
4. The browser may show “processing” or a return URL. **That page does not mark paid.**
5. Locally, an admin (or the UAT script) fires the **fake webhook** with a shared secret. Only then is the intent `paid` and the trip moves to upcoming.
6. Doing the same webhook event twice does not charge twice.

## Local vs live

Today the provider is a **fake**. When you go to market you replace it with Razorpay (or Stripe) **test**, then live. The rule stays: **only the signed webhook marks paid.**

## What we will not do

A “I paid” button in the browser. Admin “mark paid” without the webhook. A guest-edited total.
