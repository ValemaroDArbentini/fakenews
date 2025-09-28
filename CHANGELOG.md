# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html) (`v0.x.y` while in demo).

---

## [Unreleased]
### Planned
- v0.2 Scoring & Content, v0.3 Persistence & Leaderboard, v0.4 AWS Demo (on request), v0.5 Telegram Mini App.

---

## [0.1.2] – 2025‑09‑28
### Changed
- CI workflow refined for stable builds and correct api/ui paths.

### Fixed
- Minor hardening in FiguresController and LexemesController.
- Small UI polish in App.tsx and Grid.tsx.

---

## [0.1.1] – 2025‑09‑27
### Added
- **CI:** `.github/workflows/ci.yml` — build-only pipeline (backend .NET, frontend Node, Docker build, artifact upload).
- **Docs (product/architecture):** `docs/BACKLOG.md`, `docs/ROADMAP.md`, `docs/NFR.md`, `docs/Testing.md`, `SECURITY.md`, `docs/Contributing.md`.
- **ADRs:** `docs/adr/adr_001_database_choice.md` (PostgreSQL), `docs/adr/adr_002_deployment_target.md` (API→ECS Fargate, UI→S3+CloudFront).
- **A/B Plan:** `docs/A-B_testing.md`.
- **Seed dictionaries:** `lexemes_ru.ndjson`, `lexemes_en.ndjson`, `lexemes_es.ndjson`.

### Changed
- **API:** `LexemesController.cs` — refined dictionary import endpoints (validation, status codes, errors).
- **Auth:** `AdminTokenAttribute.cs` — admin-gated actions (moderation, import).
- **Runtime:** `Program.cs`, `docker-compose.yml` — minor adjustments for local/dev parity.
- **README:** updated with current setup and references.
- **.env.example:** refreshed variables for local/dev.
- **UI:** `ui/src/App.tsx` — small improvements.

### Fixed
- Minor edge cases in import flow and error handling.

---

## [0.1.0] – 2025‑09‑16
### Added
- First public commit/push with **working demo**: backend API + frontend UI.
- Docker setup and local compose for dev/testing.
- Initial endpoints and UI interactions implemented (pre‑Git development and debugging were local).

### Notes
- Architectural demo positioning; public AWS link available on request only.

---

## Legend
- **Added** for new features.
- **Changed** for changes in existing functionality.
- **Deprecated** for soon‑to‑be removed features.
- **Removed** for now removed features.
- **Fixed** for any bug fixes.
- **Security** in case of vulnerabilities.

