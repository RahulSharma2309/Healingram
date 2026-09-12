# Flow — Payment ready

**Who:** a guest whose stay is confirmed (or who accepted an alternative).

## Happy path

1. They see a frozen summary: retreat, programme, dates, guests, room, price, tax, extra charges, cancellation, total.
2. Pay INR … securely. If we have not connected a live gateway yet, they see a clear “payment is not yet available” — never a fake success.
3. After paying, the browser may say “Processing payment”. That page does not decide the outcome.
4. Only when our server accepts a signed webhook (right amount, currency, reference, once only) do we mark paid, then confirmed, then My Trips and notifications.

## Failure

“Payment not confirmed” or still processing. No confirmed booking. Safe retry that cannot double-charge.
