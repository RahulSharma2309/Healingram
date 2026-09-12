# Flow — Talk to an Expert

**Who:** a guest who wants a human, from homepage, results, listing, or Find My Match.

## Happy path

1. `/expert` is a concierge request, not a generic contact form. If they came from a retreat, we show “You are asking about …”.
2. They give name, mobile (India +91 unless they change it), email, what they need help with, need, travel window. Message is optional.
3. WhatsApp or phone contact requires an unchecked consent box with the exact agreed wording.
4. **Request a Call** saves the lead and thanks them.
5. **Chat on WhatsApp** saves the lead first, then opens WhatsApp with a readable message and no internal ids.

## Recovery

If WhatsApp fails to open, the lead is still saved. They can Retry or Request a Call instead.
