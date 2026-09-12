# Epics (review map)

Story **truth** lives in the story files. This page is the map so you can scan the whole product before anyone codes the next slice.

**Read next (implementation):** see [../CURRENT.md](../CURRENT.md).  
**Do not write full story files ahead of the current one.** Feature folders list planned titles only until it is that story’s turn.

| Epic | Folder | What it is |
| --- | --- | --- |
| 00 | [EPIC-00-platform](EPIC-00-platform/) | Machine: API, gateway, Postgres, logs, CI, UI snapshot |
| 01 | [EPIC-01-inventory](EPIC-01-inventory/) | Catalog + **inventory-driven** states/cities |
| 02 | [EPIC-02-identity](EPIC-02-identity/) | Register, login, roles |
| 03 | [EPIC-03-discovery](EPIC-03-discovery/) | Header, homepage, destination chips from catalog |
| 04 | [EPIC-04-results](EPIC-04-results/) | `/retreats` search + location expand |
| 05 | [EPIC-05-matching](EPIC-05-matching/) | Find My Match |
| 06 | [EPIC-06-listing](EPIC-06-listing/) | Retreat page, programme as product |
| 07 | [EPIC-07-availability](EPIC-07-availability/) | Check Availability request |
| 08 | [EPIC-08-operations](EPIC-08-operations/) | Partner + admin queues |
| 09 | [EPIC-09-payment](EPIC-09-payment/) | Payment-ready + webhook only |
| 10 | [EPIC-10-expert](EPIC-10-expert/) | Expert lead, then WhatsApp |
| 11 | [EPIC-11-account](EPIC-11-account/) | Wishlist, My Trips |
| 12 | [EPIC-12-hardening](EPIC-12-hardening/) | Mail, a11y, acceptance suite |

The numbered table in `docs/product/backlog.md` is the same IDs. When a story ships, we update **both** the story file’s Done section and the backlog status.
