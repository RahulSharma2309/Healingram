# Prerequisites — EPIC-09 Payment

| Need | V1 approach | Paid? | Later |
| --- | --- | --- | --- |
| Payment gateway | **Fake provider** in Docker/dev that can post a signed webhook | No | Razorpay / Stripe after compliance |
| Webhook public URL | Local: provider container → API. Real: ngrok or cloud endpoint | ngrok may be paid | Cloud LB |
| PCI | We never collect raw card PAN in our forms when a real gateway exists | Gateway fees | — |
| Settlement | Data field only (`MARKETPLACE_SPLIT` / `PARTNER_DIRECT`). No automated split in V1. | — | Marketplace payouts |

Real charging is **not** a V1 launch blocker. The webhook contract is.
