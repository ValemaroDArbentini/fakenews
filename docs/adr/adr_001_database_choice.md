# ADR 001 — Database Choice

- **Status:** Accepted
- **Date:** 2025-09-25

## Context
We need simple, transactional persistence for two domains:
1) **Scores** (append-heavy, consistent writes, simple reads: top-N, by-user in future),
2) **Dictionaries** (versioned, occasionally bulk-updated, read-mostly).

Constraints:
- Single-developer cadence, fast local dev,
- Demo-grade AWS deployment (on request),
- Low traffic, low cost,
- Prefer SQL for transparency and easy debugging.

## Options Considered
- **PostgreSQL** (local Docker → AWS RDS/pg): ACID, mature tooling, SQL familiarity.
- **DynamoDB**: serverless scale, but requires careful key design and adds complexity.
- **SQLite**: simplest local dev, but limited concurrency for API demo.

## Decision
**Choose PostgreSQL** for both scores and dictionaries.

## Rationale
- ACID transactions and familiar SQL for quick iteration.
- Simple local → cloud path (Docker pg → RDS pg).
- Rich ecosystem (migrations, ORMs, psql) and easy export/import.
- Fits latency and cost targets for a low-traffic demo.

## Consequences
- We manage a stateful service (backups/snapshots needed).
- Migrations discipline required (versioned schema).
- If traffic grows or access patterns change, we can:
  - Move hot leaderboard reads to a cache (Redis) or materialized views,
  - Offload dictionary versions to object storage (S3) with an index in Postgres,
  - Re-evaluate DynamoDB for write-heavy leaderboards.

## Related
- NFR: performance, latency targets, backups.
- Roadmap v0.3: persistence & leaderboard.

