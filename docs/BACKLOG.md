# Fake News – Backlog (Epics & User Stories)

> Purpose: Part of Product + Architecture effort. Scope is intentionally lean for demo reasons.> Purpose: make the project read like a Product + Architecture effort. Scope is intentionally lean but professional.

---

## Epic A — Core Gameplay Engine
**Goal:** MVP engine with deterministic rules and simple scoring.

- **A1. Spawn mechanics**  
  *As a player, I want blocks/words to spawn at a predictable rate so that I can learn and improve.*  
  **Acceptance:** configurable spawn interval; pause/resume keeps schedule.

- **A2. Placement & collision**  
  *As a player, I want pieces to respect boundaries and collisions so that the board behaves consistently.*  
  **Acceptance:** no overlaps; wall-kicks optional (future).

- **A3. Line/cluster clear**  
  *As a player, I want completed lines/clusters to clear so that the game progresses.*  
  **Acceptance:** clear event fires; score increases; next tick proceeds.

- **A4. Game loop state**  
  *As a system, I need explicit states (init → running → paused → game-over) so that engine is testable.*  
  **Acceptance:** public state enum; transitions logged; `/health` OK when running.

---

## Epic B — UI & Accessibility
**Goal:** Simple, responsive UI to interact without Swagger.

- **B1. Board rendering**  
  *As a player, I want to see the board and next piece so that I can plan moves.*  
  **Acceptance:** 60 FPS target; p95 input→render < 100 ms (demo).

- **B2. Controls**  
  *As a player, I want keyboard/touch controls so that I can play on desktop and mobile.*  
  **Acceptance:** Arrow/space; touch buttons; debounced inputs.

- **B3. Feedback**  
  *As a player, I want visual feedback for clears/errors so that the UI feels alive.*  
  **Acceptance:** animations under 300 ms; no layout shifts.

---

## Epic C — Scoring & Leaderboard
**Goal:** Persistent scoring with anti-cheat basics.

- **C1. Scoring rules**  
  *As a player, I want transparent scoring so that progress feels fair.*  
  **Acceptance:** README includes table; score emitted on clear.

- **C2. Persist score**  
  *As a system, I want to store final scores so that leaderboards work.*  
  **Acceptance:** POST `/scores`; schema v1; idempotent retries.

- **C3. Leaderboard**  
  *As a player, I want a top list so that I can compare results.*  
  **Acceptance:** GET `/leaderboard?limit=…`; default 20; cached 60s.

- **C4. Basic anti-cheat**  
  *As a system, I want server-side validation so that obvious tampering is rejected.*  
  **Acceptance:** signed payload (server secret); invalid → 400.

---

## Epic D — Content / Dictionary Management
**Goal:** Manage word lists via API and seed data.

- **D1. Upload dictionary**  
  *As an admin, I want to upload word lists so that gameplay can vary.*  
  **Acceptance:** POST `/dictionary` (CSV/JSON); size limit; schema check.

- **D2. List & versions**  
  *As an admin, I want versioned dictionaries so that I can roll back.*  
  **Acceptance:** GET `/dictionary?version=…`; latest by default.

- **D3. Sampling rules**  
  *As a designer, I want rarity/weighting so that difficulty is tunable.*  
  **Acceptance:** weights in config; default uniform.

---

## Epic E — Deployment & Infrastructure (Demo)
**Goal:** Reproducible demo-grade deployment.

- **E1. Containerize**  
  *As a developer, I want Docker images so that environments are consistent.*  
  **Acceptance:** `Dockerfile` for API/UI; multi-stage build.

- **E2. CI pipeline**  
  *As a team, we want CI to build/test images on each PR so that quality is observable.*  
  **Acceptance:** GitHub Actions status badge; green on main.

- **E3. AWS demo (on request)**  
  *As an architect, I want optional AWS demo so that stakeholders can try it live.*  
  **Acceptance:** S3+CloudFront (UI), ECS/Fargate (API) — documented; link shared on demand.

---

## Epic F — Observability & NFR
**Goal:** Minimal, meaningful signals.

- **F1. Health & readiness**  
  *As ops, I need `/health` and `/ready` so that deploys are safe.*  
  **Acceptance:** `/health` = deps optional; `/ready` = deps required.

- **F2. Structured logs**  
  *As ops, I want JSON logs so that parsing works everywhere.*  
  **Acceptance:** request id, state transitions, errors.

- **F3. Latency target**  
  *As product, I want p95 UI latency < 200 ms (demo) so that interaction feels responsive.*  
  **Acceptance:** measured locally; future CW metrics.

---

## Epic G — Security & Access
**Goal:** Sensible defaults without over-engineering.

- **G1. No PII by design**  
  *As an architect, I want gameplay without personal data so that GDPR is a non-issue.*  
  **Acceptance:** anonymous session id only.

- **G2. Secrets management**  
  *As ops, I want secrets in GitHub/AWS SSM so that configs aren’t in code.*  
  **Acceptance:** README shows env variables; repo has no secrets.

- **G3. Basic RBAC (future)**  
  *As admin, I want admin vs player roles so that ops features are protected.*  
  **Acceptance:** guarded endpoints; 401/403 on misuse.

---

## Epic H — Telegram Mini App & Webhooks

**Goal:** Run the game as a Telegram Mini App with a secure webhook gateway (no PII).

**H1. Mini App shell
*As a player, I want to launch the game inside Telegram so that I can play without a browser.
**Acceptance:** Mini App loads UI; handles back button; responsive layout.

**H2. Auth mapping (no PII)
*As a system, I want to validate initData and map to an anonymous session so that GDPR remains a non-issue.
**Acceptance:** initData signature verified; session id issued; no user data stored.

**H3. Webhook endpoint
*As ops, I want a verified webhook for bot events so that commands/deep links work reliably.
**Acceptance:** /telegram/webhook with secret/token check; idempotent retries.

**H4. Commands & deep links
*As a player, I want /start and deep links so that onboarding is simple.
**Acceptance:** /start opens board; parameters forwarded (difficulty).

**H5. Rate limits & resilience
*As ops, I want rate limiting and backoff so that bursts don’t degrade UX.
**Acceptance:** 429 on abuse; retry-after headers; logs.

**H6. Deploy & config
*As ops, I want setWebhook scripts and env config so that rollout is repeatable.
**Acceptance:** script/runbook added; env vars documented.

**H7. Metrics
*As product, I want basic counters (sessions, avg game time) so that A/B later has baseline.
**Acceptance:** counters emitted; no PII.

---

## Definition of Done (per story)
- Code + minimal tests (or manual check noted)  
- Updated README/docs if behavior changes  
- Log/metrics unaffected or improved  
- CI green on `main`

