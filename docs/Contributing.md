# Contributing Guidelines

> This is a demo/pet project. The intent is to showcase architecture and process, not to grow a large OSS community. Still, clear conventions help readability.

---

## 1) Workflow
- Default branch: `main` (always green).
- Active work: `feature/*` branches, PRs into `develop` (optional for solo dev).
- Merge into `main` only via pull request (self‑review allowed).

## 2) Commit Messages
- Follow **Conventional Commits**:
  - `feat:` new feature
  - `fix:` bug fix
  - `docs:` documentation
  - `chore:` tooling, config
  - `refactor:` code restructuring without feature change
- Example: `feat(api): add POST /scores endpoint`

## 3) Code Style
- Backend (.NET): follow default dotnet format/lint.
- Frontend (JS/TS): eslint + prettier defaults.
- Dockerfiles: multi‑stage builds, minimal base images.

## 4) Documentation
- Update README when behavior changes.
- Add/change ADRs for major technical decisions.
- Update ROADMAP/Backlog when features land.

## 5) Testing
- Unit tests where practical.
- Manual smoke testing allowed for UI while early.
- CI must pass (even if tests are stubbed).

## 6) Release & Tags
- Use semantic versioning (`v0.x.y`).
- Tag milestones (v0.1, v0.2, …) on `main`.
- Maintain CHANGELOG.md with highlights.

## 7) Issues & Tasks
- Use GitHub Issues for features/bugs.
- Link issues in PRs.
- Keep backlog updated with state (open/closed).

---

_Last updated: 2025‑09‑25_

