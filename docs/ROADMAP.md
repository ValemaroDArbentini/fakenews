# Fake News – Roadmap

> Purpose: incremental development. Each milestone = 1 sprint (~2 weeks in spare-time mode).

---

## v0.1 — MVP Engine & UI ✔
- Basic game loop (spawn → place → clear)
- Simple UI with keyboard controls
- Dictionary hardcoded (seed list)
- Manual local run (`npm run dev`, `dotnet run`)
- Status: delivered (first commit/push)

---

## v0.2 — Scoring & Content
- Implement scoring rules + display
- Dictionary upload via API (`/dictionary`)
- Versioned word lists
- Basic animations (clear feedback)
- CI pipeline skeleton (GitHub Actions)
- ETA: Sprint 2

---

## v0.3 — Persistence & Leaderboard
- Store scores in DB (PostgreSQL)
- REST endpoints: `POST /scores`, `GET /leaderboard`
- Leaderboard UI
- Anti-cheat basics (server-side validation)
- Observability: `/health`, `/ready`, structured logs
- ETA: Sprint 3

---

## v0.4 — AWS Demo (on request)
- Containerized services (Docker)
- Terraform plan (S3 + CloudFront + ECS)
- Optional AWS demo (available via private link)
- Latency/SLA targets documented (p95 <200ms)
- Roadshow post on LinkedIn
- ETA: Sprint 4

---

## v0.5 — Telegram Mini App & Webhooks

- Telegram Mini App shell (responsive, back button)
- Auth mapping via initData → anonymous session (no PII)
- Verified webhook endpoint + /start deep links
- Baseline metrics (sessions, avg game time)
- Runbook: setWebhook, env config
- ETA: Sprint 5

---

## v0.6+ — Future Ideas

- Mobile-friendly controls
- RBAC for admin ops
- A/B testing of spawn/word rules
- Analytics dashboard (basic metrics)
- Community contributions (if interest)

---

## Release Cadence
- Target: 1 milestone per sprint (≈2 weeks)
- Small bugfix releases allowed between milestones
- CHANGELOG.md updated each release

