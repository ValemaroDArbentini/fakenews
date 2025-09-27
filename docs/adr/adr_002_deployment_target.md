# ADR 002 — Deployment Target (API & UI)

- **Status:** Accepted
- **Date:** 2025-09-25

## Context
We need a demo-grade but professional deployment target for:
- **UI** (React/Vite): static assets.
- **API** (.NET): containerized web service.

Constraints:
- Single developer, spare‑time cadence.
- Low traffic; cost control matters.
- Reproducibility over raw speed; infra as code (Terraform planned).
- Public demo **optional** (shared on request), to avoid premature “prod” perception.

## Options Considered
1) **EC2 (VM-based)**
   - + Full control, simplest mental model.
   - − Pets not cattle; patching/AMIs, scaling & HA manual; more ops toil.

2) **ECS on Fargate (serverless containers)**
   - + Managed control plane; scale‑to‑N; integrates with CloudWatch/ALB.
   - + Good fit for containerized .NET API; minimal ops.
   - − Slightly higher $ than EC2 at micro scale; task definitions to learn.

3) **Lambda (serverless functions)**
   - + No servers; scale per request.
   - − Cold starts, packaging .NET, WebSockets/long‑running isn’t ideal; state & sessions trickier.

4) **Amplify/Beanstalk**
   - + Fastest bootstrap.
   - − Opinionated stacks; educational value (IaC clarity) lower.

5) **UI on S3 + CloudFront**
   - + Best practice for static sites; cheap, cacheable, TLS easy.
   - − Invalidation on deploy to handle cache.

## Decision
- **API:** **ECS on Fargate** behind an Application Load Balancer.
- **UI:** **S3 + CloudFront** for static hosting.

## Rationale
- Matches containerized dev flow; reduces ops load vs EC2.
- Clear IaC story (Terraform modules for ECS/ALB/RDS/S3/CF).
- Demo-friendly: start small, scale horizontally if needed.
- Keeps cost predictable; easy to pause/destroy with Terraform.

## Consequences
- Need task definitions, ALB target groups, service autoscaling policies (even if minimal).
- Docker images must be pushed to ECR; CI updates required later.
- TLS termination on ALB/CloudFront; route via Route 53 (optional).

## Rollout Plan (phased)
- **Phase 1 (v0.4):**
  - UI → S3/CloudFront; API → single Fargate task; RDS (dev) optional.
  - Health/ready probes; basic logs to CloudWatch.
- **Phase 2:**
  - Add autoscaling (CPU/latency); ECR push in CI; Terraform modules.
- **Phase 3:**
  - Cost tuning (task size, idle schedules), WAF if public; ALB access logs.

## Alternatives for Future
- Lambda + API Gateway if endpoints remain short‑lived and purely request/response.
- EC2 spot for cost‑centric experiments.
- Static UI to GitHub Pages for zero‑AWS demo (if needed).

## Related
- NFR: deployability, observability, latency.
- ADR‑001: PostgreSQL (RDS path).

