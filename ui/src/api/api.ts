// src/api.ts — единая точка входа к бэку, без хардкодов

// База и префикс задаются через Vite env (во время сборки)
const API_BASE =
  (import.meta as any).env?.VITE_API_BASE_URL?.replace(/\/+$/, '') || ''; // напр. "https://api.example.com" или ""
const API_PREFIX =
  (import.meta as any).env?.VITE_API_PREFIX ?? '/api'; // по умолчанию ходим через nginx-прокси

const u = (path: string) => `${API_BASE}${API_PREFIX}${path}`;

export type MoveDirection = 'left' | 'right';
export type MoveDto = { figureId: string; direction: MoveDirection; steps: number; preview: boolean };

export type MovePreviewResponse = { allowedSteps: number; path: string[]; wouldBlock: boolean };
export type MoveCommitResponse = {
  success: boolean;
  performedSteps: number;
  rowsBurned: number;
  cascades: number;
  scoreGained: number;
  totalFigures: number;
  gameOver: boolean;
};

export type SessionState = {
  id: string;
  startedAt: string;
  endedAt: string | null;
  score: number;
  isWin: boolean;
  locale: string;
  isBusy: boolean;
  figures: Array<{
    id: string;
    word: string;
    headCoord: string;
    blockCoords: string[];
    isFixed: boolean;
    locale?: string;
    length?: number;
  }>;
  broadcast: any[];
};

const GUID_RE = /^[0-9a-fA-F-]{36}$/;

async function ensureOk(r: Response) {
  if (r.ok) return r;
  const ct = r.headers.get('content-type') || '';
  let detail: any = undefined;
  try { detail = ct.includes('application/json') ? await r.json() : await r.text(); } catch {}
  throw new Error(`HTTP ${r.status} ${r.statusText}` + (detail ? ` — ${typeof detail === 'string' ? detail : JSON.stringify(detail)}` : ''));
}
const json = (body: unknown): RequestInit => ({ method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) });

export const api = {
  // ранее называлось start()
  async createSession(): Promise<{ sessionId: string; locale: string; startTime: string }> {
    const r = await fetch(u(`/game/session`), { method: 'POST' }); await ensureOk(r); return r.json();
  },

  // ранее state()/session()
  async getSession(sessionId: string): Promise<SessionState> {
    const r = await fetch(u(`/game/session/${sessionId}`)); await ensureOk(r); return r.json();
  },

  // ранее figures()
  async getFigures(sessionId: string): Promise<SessionState['figures']> {
    const r = await fetch(u(`/game/session/${sessionId}/figures`)); await ensureOk(r); return r.json();
  },

  // ранее spawn()
  async spawnLayer(sessionId: string) {
    const r = await fetch(u(`/game/session/${sessionId}/figures/spawn-layer`), { method: 'POST' }); await ensureOk(r); return r.json();
  },

  async move(sessionId: string, dto: MoveDto): Promise<MovePreviewResponse | MoveCommitResponse> {
    if (!GUID_RE.test(dto.figureId)) throw new Error(`figureId is not a GUID: ${dto.figureId}`);
    if (dto.steps <= 0) throw new Error('steps must be >= 1');
    if (dto.direction !== 'left' && dto.direction !== 'right') throw new Error("direction must be 'left' or 'right'");
    const r = await fetch(u(`/game/session/${sessionId}/move`), json(dto)); await ensureOk(r); return r.json();
  },
};

export default api;
