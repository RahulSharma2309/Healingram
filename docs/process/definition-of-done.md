# Definition of done

A **story** is done when all of the following are true:

- Acceptance criteria in the backlog are met.
- Backend unit tests cover the new rules. Integration tests exist if the story crosses a process or webhook boundary.
- Feature is reachable as an end-to-end unit (UI and/or gateway), including empty and error paths named in the spec.
- Structured logs and a trace exist for the request; no PII in logs.
- Works via `docker compose` and via localhost instructions in the README.
- CI on the story PR is green.
- `docs/qa/STORY-…` is `qa_passed`.
- `docs/uat/STORY-…` is `uat_passed`.
- Backlog status is `done`.

A **feature** is done when every story in it is `done` and the flow doc still matches the product.

An **epic** is done when every feature is `done` and the epic prerequisite doc lists what was deferred.

The **iteration** is done when every V1 epic is `done`. The only remaining work is production deployment.
