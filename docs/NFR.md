# Non‑Functional Requirements (NFR)

> Scope: demo/architectural showcase.
---

## 1) Assumptions
- Single developer, spare‑time cadence (≈2‑week sprints).
- Low traffic demo; no PII; anonymous sessions.
- AWS demo is optional (shared on request).

## 2) Service Level Targets (hints)
- **Availability (demo):** 99.5% target for a single region (when demo is published).
- **Latency (UI p95):** < 200 ms for primary interactions.
- **Time‑to‑ack:** < 1 s from action to visible feedback.

## 3) Performance & Capacity
- Frontend served via CDN (CloudFront) when demoed.
- API stateless; request budget p95 < 150 ms (simple endpoints).
- Dictionary uploads: ≤ 5 MB, processed in < 5 s.

## 4) Scalability
- Horizontal scale of API via ECS tasks (planned).
- Worker model reserved for async tasks (future); scale‑out by queue depth.
- Static assets via S3/CloudFront.

## 5) Resilience & Reliability
- Health/readiness endpoints: `/health` (soft), `/ready` (strict deps).
- Retries with backoff on outbound calls (future).
- Graceful shutdown; idempotent score/write operations.
- Single‑AZ acceptable for demo; Multi‑AZ noted for future.

## 6) Observability
- Structured JSON logs with request id and state transitions.
- Basic metrics (requests/sec, errors, p95 latency) — future CloudWatch.
- Alert hints: 5xx rate spike, readiness probe failures.

## 7) Security (minimal, demo‑grade)
- **No PII** by design; anonymous session id only.
- Secrets in GitHub/AWS SSM; no secrets in repo.
- RBAC concept: admin vs player (future guard rails for ops endpoints).

## 8) Data & Consistency
- PostgreSQL for transactional writes (scores, dictionaries).
- Versioned dictionaries; safe rollbacks.
- Backups: snapshots for demo DB (manual/cron).

## 9) Deployability & Operability
- Containers for API/UI; reproducible builds.
- CI builds on PR; main stays green.
- Terraform plan for S3/CF + ECS (WIP).

## 10) Maintainability
- ADRs for significant choices.
- Clear boundaries (UI / API / Engine / Storage).
- Linting and conventional commits.

## 11) Risks & Mitigations
- **Single‑dev bottleneck:** scope control, roadmap cadence.
- **Demo instability:** gated AWS link; health checks.
- **Cost drift:** small instance types; lifecycle policies.

## 12) Out of Scope (for demo)
- Multi‑region HA, advanced auth, full analytics stack.
- Formal SLA/Support; enterprise compliance beyond “no PII”.

