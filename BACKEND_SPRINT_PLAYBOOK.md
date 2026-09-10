# SereniTrip Full-Stack Sprint Playbook (Backend + Frontend, Agile, Microservices)

This file is a **step-by-step implementation guide** for turning the current dummy front-end into a **real, shippable full-stack product**—implemented progressively in **sprints** so UAT can happen **every sprint** on another laptop (your wife’s).

The product you described in `README.md` is a **wellness retreat marketplace**:
- **Customer site**: browse/search retreats, view details, book, pay, confirmation, questionnaire + recommendations, login/signup, dashboard
- **Vendor panel**: manage retreats, availability/calendar sync, bookings, earnings
- **Admin panel**: manage vendors, retreats, bookings, commission, CMS/SEO, reports, recommendation rules

This playbook assumes a microservice backend with a simple API Gateway (BFF) and event-driven integration where useful.

---

## Global decisions (set once, early)

### Architecture baseline
- **API Gateway / BFF**: 1 public backend entrypoint for the frontend (`/api/...`), does auth enforcement, request routing, response shaping.
- **Microservices**: each service owns its data store schema and is deployed independently.
- **Async events**: for cross-service side effects (email confirmation, payment status updates, booking confirmations).

### Recommended tech (opinionated but pragmatic)
- **Gateway/BFF**: Node.js (Fastify/NestJS) or .NET Minimal APIs (since you’re on Windows), whichever you prefer.
- **Services**: Node.js or .NET; keep everything consistent.
- **Databases**:
  - Start with **PostgreSQL** for transactional services (users, retreats, availability, bookings).
  - Add **Redis** for sessions/caching/rate-limits/locks (later sprint).
  - Add **Search engine** (OpenSearch/Meilisearch/Typesense) only when needed.
- **Auth**: JWT access token + refresh token (or sessions), backed by Auth service.
- **Messaging**: start with in-process “event outbox table” + polling worker; later switch to RabbitMQ/Kafka.

### Repo layout (mono-repo recommended for speed)
Create these folders from scratch:
- `apps/frontend` (React + Vite; this is where your current dummy UI will evolve)
- `apps/gateway-bff`
- `services/auth`
- `services/user`
- `services/retreat-catalog`
- `services/availability`
- `services/booking`
- `services/payment`
- `services/notification`
- `services/vendor`
- `services/admin` (optional; can be BFF endpoints + admin-only policies)
- `infra` (docker compose, db migrations, local scripts)

### Environments
Define 3 from the beginning:
- **local-dev** (your laptop)
- **uat** (wife’s laptop)
- **prod** (future)

Each sprint ends with a UAT package: `docker compose up` + seeded data + a URL + a **short script of manual test steps** for your wife.

---

## Sprint format used below

Every sprint includes:
- **Goal** (business value)
- **Stories** (what you build)
- **Service(s) added/changed**
- **DB schema** (minimum tables/collections)
- **Endpoints** (minimum API surface)
- **Frontend features unlocked**
- **UAT checklist** (wife can validate on the other laptop)

---

## Sprint 0 — Foundations: repo layout, gateway skeleton, dummy frontend wiring

### Goal
Wife can run the system on another laptop and see:
- Frontend running
- API Gateway responding on `/api/health`
- Swagger UI for the gateway showing the health endpoint

### Stories (developer-facing, step by step)
1. **Story S0.1 — Create mono-repo layout**
   - Create the folder structure listed above.
   - Add a root `README.md` that explains:
     - How to run frontend
     - How to run gateway
     - What services exist (currently: none, only gateway).
2. **Story S0.2 — Create gateway-bff app skeleton**
   - Implement a minimal HTTP server (e.g., Fastify or ASP.NET Minimal API) with:
     - `GET /api/health` → `{ status: "ok", version, time }`
     - `GET /api/meta` → `{ environment, build, commit }`
   - Add **Swagger/OpenAPI** generation for the gateway:
     - Swagger UI at `/api/docs`
     - Document `GET /api/health` and `GET /api/meta`.
3. **Story S0.3 — Wire frontend to gateway**
   - Move your current dummy Vite app into `apps/frontend`.
   - Add an `.env.local` for frontend with `VITE_API_BASE_URL=http://localhost:5000/api` (example).
   - Add a small page or header component:
     - Calls `GET /api/health` when app loads.
     - Displays status text and environment name.
4. **Story S0.4 — UAT packaging basics**
   - Under `infra/`, create a minimal `docker-compose.uat.yml` that:
     - Builds and runs `gateway-bff`.
     - Builds and runs `frontend`.
   - Create `scripts/uat-start.ps1` that:
     - Calls `docker compose -f infra/docker-compose.uat.yml up --build`.

### Services
- **gateway-bff** (only; with Swagger)

### DB
- None yet.

### Endpoints (gateway-bff)
- `GET /api/health` → `{ status: "ok", version, time }`
- `GET /api/meta` → `{ environment, build, commit }`
- Swagger: `GET /api/docs`

### Frontend
- Add a simple `System Status` page or app banner:
  - Shows `status` and `environment` from `/api/health` and `/api/meta`.

### UAT checklist (for wife)
1. Run `.\scripts\uat-start.ps1`.
2. Open the URL written in the README (e.g., `http://localhost:5173`).
3. Confirm:
   - Page loads.
   - “System Healthy” (or similar) text is shown.
   - Environment name (e.g., `uat`) is visible.

**Exit criteria**: she can run the system with one script, see system status, and you can see gateway Swagger UI locally.

---

## Sprint 1 — Auth backend (with Swagger) + User backend (with Swagger)

In this sprint you focus purely on **backend APIs**, with Swagger-first development and **no frontend login form yet**. You will test everything from Swagger UI or Postman.

### Goal
You have:
- Auth service with working register/login/logout endpoints, documented in Swagger.
- User service with basic profile endpoints, documented in Swagger.
- Gateway routes passing through to both services.

### Services
- **auth** (new)
- **user** (new)
- **gateway-bff** (routes + auth middleware)

### Auth service responsibilities
- Registration + login
- Token issuance (access + refresh) OR session cookies
- Password reset (can stub email sending until Notification sprint)

### Auth DB (Postgres)
Minimum tables:
- `auth_credentials`
  - `id (uuid pk)`
  - `email (unique)`
  - `password_hash`
  - `user_id (uuid, unique)` (links to User service identity)
  - `created_at`, `updated_at`
- `auth_refresh_tokens` (if using refresh tokens)
  - `id (uuid pk)`
  - `user_id`
  - `token_hash`
  - `expires_at`
  - `revoked_at`

### User service responsibilities
- Profile, contact info, addresses, preferences
- “Roles” concept begins here (customer/vendor/admin), or keep roles in auth and replicate via events later.

### User DB (Postgres)
- `users`
  - `id (uuid pk)`
  - `email (unique)` (duplicate for convenience; auth is source of truth)
  - `full_name`
  - `phone`
  - `role` (`customer|vendor|admin`)
  - `status` (`active|blocked`)
  - `created_at`, `updated_at`
- `user_addresses`
  - `id (uuid pk)`
  - `user_id (fk -> users.id)`
  - `line1`, `line2`, `city`, `state`, `country`, `zip`
  - `is_default`

### Endpoints (with Swagger-first mindset)
Auth service:
- `POST /auth/register` body `{ email, password, fullName }`
- `POST /auth/login` body `{ email, password }`
- `POST /auth/logout`
- `POST /auth/refresh` (if using refresh tokens)
- `POST /auth/forgot-password` body `{ email }` (stub)
- `POST /auth/reset-password` body `{ token, newPassword }` (stub)

User service:
- `GET /users/me`
- `PATCH /users/me` body `{ fullName, phone }`
- `GET /users/me/addresses`
- `POST /users/me/addresses`
- `PATCH /users/me/addresses/:id`
- `DELETE /users/me/addresses/:id`

Gateway-bff:
- Create a route group `/api/auth/*` that proxies to Auth service.
- Create a route group `/api/users/*` that proxies to User service.
- Implement gateway-level auth middleware:
  - Reads JWT or session cookie.
  - Injects `userId` into downstream calls.
- Expose gateway Swagger at `/api/docs`:
  - Include all `/api/auth/*` and `/api/users/*` operations.

### Stories (developer-facing, step by step)
1. **Story S1.1 — Implement Auth service with Swagger**
   - In `services/auth`, create a REST API project.
   - Add Swagger/OpenAPI:
     - `/swagger` or `/docs` endpoint.
     - Document `POST /auth/register` and `POST /auth/login`.
   - Implement registration:
     - Validate email and password.
     - Hash password.
     - Create `auth_credentials` row and call User service (or emit event) later.
   - Implement login:
     - Verify credentials.
     - Issue JWT (or set cookie).
2. **Story S1.2 — Implement User service with Swagger**
   - In `services/user`, create another REST API project with Swagger.
   - Implement:
     - `GET /users/me` (reads `userId` from auth context).
     - `PATCH /users/me`.
   - Hard-code `role="customer"` for now.
3. **Story S1.3 — Wire gateway to Auth and User services**
   - Configure gateway environment variables for service URLs:
     - `AUTH_SERVICE_URL`, `USER_SERVICE_URL`.
   - Implement proxy routes as described.
   - Add OpenAPI definitions in gateway and test via `/api/docs`.
4. **Story S1.4 — Backend test only (no frontend yet)**
   - Using Swagger UI on gateway:
     - Call `POST /api/auth/register`.
     - Call `POST /api/auth/login`.
     - Use the token/cookie to call `GET /api/users/me`.

### Frontend
- No new UI yet; still just status page from Sprint 0.

### UAT checklist (for you, developer)
On your machine:
1. Open gateway Swagger `/api/docs`.
2. Register a user.
3. Log in and confirm you receive token/cookie.
4. Call `/api/users/me` and see profile.

**Exit criteria**: Both auth and user services are completely testable **from Swagger**, and gateway forwards correctly.

---

## Sprint 2 — Frontend login & registration with gateway integration

Now you take the working backend from Sprint 1 and plug it into the React frontend.

### Goal
- Users can sign up and log in from the UI.
- After login, they see a simple “My Account” page with data from `/api/users/me`.

### Stories
1. **Story S2.1 — Frontend auth API client**
   - In `apps/frontend`, create a small API client module:
     - `login({ email, password })` → calls `POST /api/auth/login`.
     - `register({ email, password, fullName })` → calls `POST /api/auth/register`.
     - `getCurrentUser()` → calls `GET /api/users/me`.
   - Handle token/cookie automatically (browser-managed cookies are easiest).
2. **Story S2.2 — Login & registration pages**
   - Build `Login` page:
     - Email, password fields, validation, “Login” button.
     - On success, redirect to `/account`.
   - Build `Register` page:
     - Email, full name, password, confirm password.
     - On success, auto-login or redirect to login.
3. **Story S2.3 — Auth-aware layout**
   - Add a small auth context or store:
     - Keeps `currentUser` in memory.
     - Uses `getCurrentUser()` on app load if token/cookie present.
   - Show:
     - “Login / Register” buttons when logged out.
     - “My Account / Logout” when logged in.
4. **Story S2.4 — Logout behaviour**
   - Implement:
     - `POST /api/auth/logout` on backend.
     - Frontend `Logout` button that calls logout and clears auth context.

### UAT checklist (for wife, on other laptop)
1. Start system (`.\scripts\uat-start.ps1`).
2. Open the app.
3. Go to `Register`:
   - Enter email + name + password.
   - Confirm she is either logged in immediately or can log in then.
4. Go to `My Account`:
   - Confirm email and name are visible.
5. Click `Logout` and verify the header/menu changes to logged-out state.

**Exit criteria**: login and registration are working end-to-end through gateway + UI. The first **full story** is now truly done.

---

## Sprint 3 — Retreat Catalog service (retreats, destinations, therapies) + public browsing UI

### Goal
Customers can browse retreats from a real backend.

### Services
- **retreat-catalog** (new)
- **gateway-bff** (routes)

### Retreat Catalog responsibilities
- Retreat CRUD (admin/vendor later; for now seed-only or admin-only)
- Query listings with filters (destination, therapy category, price range, rating)
- Retreat detail page data

### Catalog DB (Postgres)
- `retreats`
  - `id (uuid pk)`
  - `title`
  - `description`
  - `destination_id`
  - `vendor_id` (uuid, later linked to Vendor service)
  - `base_price`
  - `currency`
  - `status` (`draft|published|archived`)
  - `created_at`, `updated_at`
- `destinations`
  - `id (uuid pk)`, `name`, `country`, `state`, `city`, `slug`
- `therapies`
  - `id (uuid pk)`, `name`, `slug`
- `retreat_therapies`
  - `retreat_id`, `therapy_id` (composite key)
- `retreat_media`
  - `id (uuid pk)`, `retreat_id`, `url`, `kind`, `sort_order`

### Endpoints
Catalog service:
- `GET /retreats` query params:
  - `q`, `destination`, `therapy`, `minPrice`, `maxPrice`, `page`, `pageSize`, `sort`
- `GET /retreats/:id`
- `GET /destinations`
- `GET /therapies`

### Frontend features unlocked
- Home search and listing pages backed by real API.
- Retreat detail page uses `GET /api/retreats/:id`.

### Stories (backend + frontend)
1. **Story S3.1 — Implement Catalog service + Swagger**
   - Create `services/retreat-catalog` project with Swagger.
   - Implement DB schema described above.
   - Implement `GET /retreats`, `GET /retreats/:id`, `GET /destinations`, `GET /therapies`.
   - Add a `/swagger` or `/docs` endpoint.
2. **Story S3.2 — Gateway routes for catalog**
   - Map `/api/retreats*`, `/api/destinations`, `/api/therapies` to Catalog service.
   - Document these in gateway Swagger.
3. **Story S3.3 — Frontend: listing page wired to API**
   - Replace dummy data on listing page with real `GET /api/retreats` call.
   - Implement filters in the UI that map to query params.
4. **Story S3.4 — Frontend: retreat detail page wired to API**
   - Use route param `id` to call `GET /api/retreats/:id`.
   - Display real title, description, destination, therapies, price.

### UAT checklist
1. Open home/listing page.
2. Verify retreats are coming from backend (e.g., by changing seeded data).
3. Apply destination and therapy filters and see results change.
4. Click into a retreat and verify the detail page matches the data in DB.

**Exit criteria**: browsing and detail flow are fully backed by real services and testable also via Swagger.

---

## Sprint 3 — Availability service (calendar, capacity, reservation holds)

### Goal
Customers can select dates and see availability; system prevents overselling.

### Services
- **availability** (new)
- **retreat-catalog** (minor integration: each retreat references availability rules)
- **gateway-bff**

### Availability responsibilities
- Maintain per-retreat date availability and capacity
- Provide “price for date” overrides later
- Support temporary **holds** during checkout (reservation lock)

### Availability DB (Postgres)
- `retreat_availability_days`
  - `id (uuid pk)`
  - `retreat_id`
  - `date`
  - `capacity_total`
  - `capacity_reserved`
  - `is_closed`
  - unique(`retreat_id`,`date`)
- `availability_holds`
  - `id (uuid pk)`
  - `retreat_id`
  - `date_from`, `date_to`
  - `qty`
  - `user_id`
  - `status` (`active|expired|consumed`)
  - `expires_at`

### Endpoints
Availability service:
- `GET /availability/retreats/:retreatId` query `{ dateFrom, dateTo }`
- `POST /holds` body `{ retreatId, dateFrom, dateTo, qty }` → returns `holdId, expiresAt`
- `DELETE /holds/:holdId`

### Frontend features unlocked
- Date-picker calls availability API
- “Reserve” step creates a hold before payment

### UAT checklist
- Pick dates and see available vs sold-out
- Start checkout → hold created → refresh page → still held until expiry

**Exit criteria**: capacity protection works and holds expire.

---

## Sprint 4 — Booking service (order/booking lifecycle) + basic checkout

### Goal
Customer can create a booking record and see it in dashboard; vendor can see bookings (read-only).

### Services
- **booking** (new)
- **availability** (consume hold)
- **user** (link user)
- **gateway-bff**

### Booking responsibilities
- Create booking from a valid hold
- Track booking status: `pending_payment → confirmed → cancelled → completed`
- Provide booking history APIs for customer/vendor/admin

### Booking DB (Postgres)
- `bookings`
  - `id (uuid pk)`
  - `booking_number` (human-friendly unique)
  - `user_id`
  - `retreat_id`
  - `date_from`, `date_to`
  - `qty`
  - `amount_total`, `currency`
  - `status`
  - `created_at`, `updated_at`
- `booking_events` (audit trail)
  - `id`, `booking_id`, `type`, `payload_json`, `created_at`

### Endpoints
Booking service:
- `POST /bookings` body `{ holdId, contactInfo }` → `bookingId, status=pending_payment`
- `GET /bookings/:id`
- `GET /bookings/my` (customer)
- `GET /bookings/vendor` (vendor; can be stubbed until Vendor sprint)

### Frontend features unlocked
- Checkout creates a booking (pending payment)
- Dashboard shows booking list + status

### UAT checklist
- Create booking from a hold
- See booking in “My Bookings”
- Confirm it is `pending_payment`

**Exit criteria**: booking creation stable; no oversell; idempotent retry safe.

---

## Sprint 5 — Payment service (gateway integration) + webhooks

### Goal
Customer can pay and booking becomes confirmed.

### Services
- **payment** (new)
- **booking** (reacts to payment success/fail)
- **gateway-bff**

### Payment responsibilities
- Create payment intent/session
- Handle gateway webhook callbacks
- Emit events `payment_succeeded` / `payment_failed`

### Payment DB (Postgres)
- `payments`
  - `id (uuid pk)`
  - `booking_id`
  - `provider` (e.g., stripe/razorpay)
  - `provider_payment_id`
  - `amount`, `currency`
  - `status` (`created|authorized|captured|failed|refunded`)
  - `created_at`, `updated_at`
- `payment_webhook_events`
  - `id`, `provider_event_id (unique)`, `payload_json`, `received_at`

### Endpoints
Payment service:
- `POST /payments/create-intent` body `{ bookingId }` → returns provider client secret/redirect URL
- `POST /payments/webhook/:provider` (called by provider)

Booking service updates:
- `POST /bookings/:id/mark-confirmed` (internal/admin-only) OR event-driven consumer

### Frontend features unlocked
- Real payment step (redirect or embedded)
- Confirmation page after payment success

### UAT checklist
- Complete a payment in test mode
- Booking becomes `confirmed`
- Payment status visible in booking detail

**Exit criteria**: webhook is reliable; retries are idempotent; booking confirms exactly once.

---

## Sprint 6 — Notification service (email/SMS) + templates + triggers

### Goal
System sends booking confirmations and password reset emails.

### Services
- **notification** (new)
- **auth**, **booking**, **payment** emit events

### Notification responsibilities
- Email/SMS providers integration (start with email only)
- Templates (booking confirmation, password reset)
- Delivery logs + retry policy

### Notification DB
- `notification_messages`
  - `id (uuid pk)`
  - `type` (email/sms)
  - `template_key`
  - `to`
  - `payload_json`
  - `status` (`queued|sent|failed`)
  - `attempt_count`
  - `last_error`
  - `created_at`, `updated_at`

### Endpoints
Usually internal-only:
- `POST /notifications/email` (internal) OR consume from queue
- `GET /notifications/:id` (admin)

### Frontend features unlocked
- “Check your email” flows (forgot password, booking confirmation)

### UAT checklist
- Password reset request produces an email (in dev: mailhog/inbucket)
- Booking confirmation email sent on successful payment

**Exit criteria**: emails reliably show up in UAT environment.

---

## Sprint 7 — Vendor service (vendor onboarding + vendor panel becomes real)

### Goal
Vendors can manage retreats and availability; see bookings and earnings summary.

### Services
- **vendor** (new)
- **retreat-catalog**: allow vendor-owned retreats
- **availability**: vendor edits calendar/capacity
- **booking**: vendor booking views

### Vendor DB (Postgres)
- `vendors`
  - `id (uuid pk)`
  - `owner_user_id` (links to User)
  - `display_name`
  - `status` (`pending|approved|rejected|suspended`)
  - `created_at`, `updated_at`
- `vendor_payout_settings` (later)

### Endpoints
Vendor service:
- `POST /vendors/apply` body `{ displayName, docs... }` (docs can be stubbed)
- `GET /vendors/me`

Catalog service (vendor actions):
- `POST /retreats` (vendor)
- `PATCH /retreats/:id` (vendor)
- `POST /retreats/:id/publish` (vendor, subject to approval rules)

Availability service (vendor actions):
- `PATCH /availability/retreats/:retreatId/days` (bulk update capacity/closed)

### Frontend features unlocked
- Vendor panel uses real APIs for retreat management + calendar + bookings

### UAT checklist
- Create vendor application (admin can approve in Sprint 8)
- Vendor creates a retreat draft and edits it
- Vendor updates availability calendar

**Exit criteria**: vendor panel is not a mock anymore.

---

## Sprint 8 — Admin capabilities (approvals, commission, moderation, reports basics)

### Goal
Admin can manage vendors and retreats; adjust commissions; view bookings.

### Services
- Either:
  - **admin** service (new), or
  - admin-only endpoints in existing services behind admin auth policies

### Admin DB (optional)
- `commission_rules`
  - `id`, `vendor_id (nullable)`, `percent`, `effective_from`, `effective_to`
- `admin_actions_audit`
  - `id`, `admin_user_id`, `action_type`, `target_type`, `target_id`, `payload_json`, `created_at`

### Endpoints (minimum)
- `POST /admin/vendors/:id/approve`
- `POST /admin/vendors/:id/reject`
- `POST /admin/retreats/:id/approve` / `reject`
- `GET /admin/bookings` (filters)
- `GET /admin/reports/summary` (basic)

### Frontend features unlocked
- Admin panel becomes real for the critical workflows (approval + overview)

### UAT checklist
- Admin approves a vendor
- Admin approves a retreat
- Customer books and pays; admin sees booking in reports

**Exit criteria**: core admin workflows are functional and audited.

---

## Sprint 9 — Questionnaire + Recommendation rules (MVP rule engine)

### Goal
Questionnaire responses affect recommended retreats list.

### Services
- **recommendation** (new) OR as part of catalog initially

### Recommendation DB (Postgres)
- `questionnaire_responses`
  - `id`, `user_id`, `answers_json`, `created_at`
- `recommendation_rules`
  - `id`, `name`, `condition_json`, `output_json`, `priority`, `enabled`

### Endpoints
- `POST /questionnaire/submit`
- `GET /recommendations` (returns curated retreat IDs + reasons)

### Frontend features unlocked
- Questionnaire persists responses
- Recommended section becomes real

### UAT checklist
- Submit questionnaire
- See “recommended retreats” change based on answers

---

## Sprint 10 — Hardening for real UAT (rate limits, audit, observability, data seeding)

### Goal
UAT can run reliably every sprint; errors are diagnosable; data resets are easy.

### Additions
- Centralized logging, request IDs, error codes
- Audit trail everywhere for admin/vendor actions
- `seed` command for deterministic demo dataset
- Backups (optional for UAT)
- Automated test smoke suite: “register → browse → hold → book → pay”

### UAT checklist
- One-command reset + seed
- Run automated smoke
- Manual sanity of key flows

---

## Gateway/BFF responsibilities (what belongs here vs services)

Put in **gateway-bff**:
- Auth verification (JWT/session)
- Role-based route protection
- API composition for frontend convenience (e.g., combine retreat detail + availability in 1 call)
- Rate limiting
- Serving OpenAPI docs (`/api/docs`)

Keep in **services**:
- All business rules and database writes
- Ownership checks (user/vendor/admin)
- Idempotency keys for POST endpoints

---

## “Run on wife’s laptop” UAT packaging rules (do this every sprint)

### Deliverable each sprint
- `docker-compose.uat.yml`
- `.env.uat.example`
- `scripts/uat-start.ps1` (Windows friendly)
- `scripts/uat-reset-and-seed.ps1`
- “What changed this sprint” section in a `SPRINT_NOTES.md`

### Minimum UAT instructions (copy/paste friendly)
1. Install Docker Desktop
2. Clone repo / copy zip
3. Run `.\scripts\uat-start.ps1`
4. Open `http://localhost:5173`
5. Use test accounts listed in `SPRINT_NOTES.md`

---

## Service list summary (final target)

You don’t need all of these on day 1—but this is the “company-grade” set aligned to your UI:
- `gateway-bff`
- `auth`
- `user`
- `retreat-catalog`
- `availability`
- `booking`
- `payment`
- `notification`
- `vendor`
- `admin` (optional; can be policies + endpoints)
- `recommendation` (when questionnaire becomes real)
- `search` (when catalog filtering gets heavy)

