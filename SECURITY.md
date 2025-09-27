# Security Policy (Demo‑grade)

> Scope: architectural demo / pet project. Intent is to set sensible, minimal safeguards without over‑engineering.

---

## 1) Data Classification & Privacy
- **No PII by design.** Gameplay uses anonymous session IDs; no names, emails, or identifiers.
- Content dictionaries are non‑sensitive demo data.
- Logs avoid payloads and secrets; include request id and minimal context.

## 2) Secrets Management
- **Never** commit secrets to the repo.
- Local dev: `.env.local` (not committed).
- CI/CD: GitHub Encrypted Secrets.
- AWS demo: SSM Parameter Store / Secrets Manager (planned).

## 3) Access Control
- Public gameplay endpoints only; admin/ops endpoints are guarded (future RBAC: admin vs player).
- Principle of least privilege for AWS IAM roles/policies (when demoed).

## 4) Transport & Storage
- HTTPS enforced for public endpoints (when demoed via ALB/CloudFront).
- Storage (Postgres/RDS) with auth; no public exposure; security groups restrict ingress.
- Backups/snapshots for demo DB (manual/cron).

## 5) Dependencies & Builds
- Dependabot (planned) for security updates.
- Reproducible builds via Docker; pinned base images where practical.

## 6) Logging & Monitoring
- Structured JSON logs; avoid secrets; rotate via platform defaults (CloudWatch when demoed).
- Health (`/health`) and readiness (`/ready`) endpoints for safe deployments.

## 7) Responsible Disclosure
- Report vulnerabilities via GitHub Issues or private message to the repo owner.
- Do not test against non‑public deployments without explicit permission.

## 8) Out of Scope (Demo)
- Advanced auth (SSO/MFA), DLP, WAF ruleset, multi‑region HA.
- Compliance certifications (ISO, SOC, PCI) — not applicable to demo.

---

_Last updated: 2025‑09‑25_

