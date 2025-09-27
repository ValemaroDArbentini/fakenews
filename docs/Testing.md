# Testing Strategy

> Goal: provide a pragmatic test process that gates releases. Early stages may use stubs, but the pipeline and checklists are real.

---

## 1) Principles
- **Small, meaningful gates:** ship small, validate early.
- **Automate the boring parts:** unit & CI checks first; e2e grows with UI.
- **Fail fast:** red builds block release; green main only.

## 2) Test Pyramid (lean)
- **Unit** (engine rules, utilities): fast, deterministic.
- **API** (contract & status codes): minimal happy paths + key errors.
- **UI/e2e** (smoke): core flows only (start → place → clear → score).

## 3) Environments
- **Local:** dev iteration (hot reload where possible).
- **CI:** build + unit/API tests (no deploy).
- **Demo (optional):** AWS on request; validated by smoke tests.

## 4) Gates & Policy
- `main` must be **green** (CI) before tagging release.
- A release requires: passing tests + updated docs (CHANGELOG/README if behavior changes).
- Demo deployment allowed only from a tagged release (v0.x.y).

## 5) What We Test (early scope)
### 5.1 Engine (unit)
- Spawn cadence respected.
- Collision & boundaries.
- Line/cluster clear → score delta.
- State machine transitions (init → running → paused → game‑over).

### 5.2 API (contract)
- `POST /scores` → 201 on valid, 400 on invalid; idempotent retry OK.
- `GET /leaderboard?limit=` → 200, default 20, max cap enforced.
- `POST /dictionary` → schema/size check; 415 on wrong media type.
- Health endpoints: `/health` (soft), `/ready` (strict deps).

### 5.3 UI (smoke/e2e)
- Render board, accept inputs, show score changes.
- Visual feedback on clear.
- Latency budget: input→render perceptibly < 200ms (manual check early).

## 6) Tooling
- **Backend:** xUnit (or NUnit), FluentAssertions; test project in `tests/backend`.
- **API:** lightweight HTTP tests (dotnet or REST client scripts).
- **UI:** Playwright (preferred) or Cypress later; smoke in CI optional initially.

## 7) Data & Fixtures
- Seed dictionary (small set) for deterministic tests.
- Test doubles for engine timers and RNG.
- Truncate test DB between runs; no shared state.

## 8) Performance & NFR Checks (lightweight)
- Measure p95 request time on simple endpoints in CI (baseline).
- Manual smoke of UI latency each milestone; record in CHANGELOG.

## 9) Security Checks (minimal)
- No secrets in repo (CI guard).
- Deny large dictionaries (>5MB) with clear error.

## 10) Release Checklist
- [ ] CI green on `main`
- [ ] CHANGELOG updated (features/fixes)
- [ ] README/docs adjusted (if behavior changed)
- [ ] Tag `v0.x.y`
- [ ] (Optional) Deploy demo; run smoke e2e; share private link

## 11) Out of Scope (early)
- Full visual regression
- Full perf/load testing
- Penetration testing

---

_Last updated: 2025‑09‑25_
