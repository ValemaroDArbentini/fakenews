# A/B Testing Plan

> Purpose: validate gameplay and UX hypotheses with lightweight experiments. No PII; anonymous sessions only.

---

## 1) Principles
- Keep tests small, time‑boxed to a sprint.
- Measure one primary metric per experiment.
- Roll forward by default; roll back if clear regression.

## 2) Metrics
- **Engagement:** avg session length, sessions/player (anonymous id).
- **Responsiveness:** input→render delay (ms), p95 API latency (ms).
- **Retention proxy:** return within 24h (cookie/session‑based, anonymous).

## 3) Hypotheses & Experiments (initial)

### H‑01 Spawn Cadence
**Hypothesis:** faster spawn increases engagement up to a point, after which churn rises.  
**Variant A:** spawn every 1200 ms (control).  
**Variant B:** spawn every 900 ms.  
**Success:** +10% avg session length without >5% increase in early quits.

### H‑02 Dictionary Difficulty
**Hypothesis:** weighted rarities increase perceived challenge without hurting retention.  
**A:** uniform sampling (control).  
**B:** 70/25/5 rarity weighting.  
**Success:** +15% clears per minute, stable session length (±5%).

### H‑03 Feedback Animations
**Hypothesis:** short animations improve UX; long ones reduce responsiveness.  
**A:** 0 ms (no animation).  
**B:** 180 ms clear animation.  
**Success:** +CSAT (thumbs‑up), input→render p95 stays < 200 ms.

### H‑04 Leaderboard Prompt
**Hypothesis:** a subtle prompt increases score submissions without hurting flow.  
**A:** no prompt (control).  
**B:** non‑blocking hint on game‑over.  
**Success:** +20% POST /scores, no increase in drop rate.

---

## 4) Implementation Notes
- Anonymous session id only; no personal data.
- Variant flag via config (UI and API); cookie for stickiness.
- Metrics emitted as counters/timers (future CW).

## 5) Process
1. Choose 1 hypothesis per sprint.
2. Implement variant(s) behind a flag.
3. Run for a fixed window (≥ 1 week of organic usage or N sessions).
4. Analyze metrics; decide: roll forward / roll back / iterate.
5. Record outcome in CHANGELOG.

---

## 6) Risks
- Low traffic → inconclusive results (mitigate with longer window or stronger signals).
- Over‑optimization for demo metrics; keep fun/clarity first.

---

_Last updated: 2025‑09‑25_

