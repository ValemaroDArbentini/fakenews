// 📄 Назначение: Клиент API; Путь: /ui/src/api/client.ts
import type { StartDto, MoveDto, SpawnDto, EndDto } from './dto'

const baseUrl = '/api'; // без localhost и без http://api

async function http<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${baseUrl}${path}`, {
    headers: { 'Content-Type': 'application/json', ...(init?.headers||{}) },
    ...init
  })
  if (!res.ok) throw new Error(await res.text())
  return await res.json() as T
}

export const api = {
  start(): Promise<StartDto> {
    return http('/game/session', { method: 'POST' })
  },
  state(id: string) {
    return http(`/game/session/${id}`)
  },
  move(id: string, figureId: string, dir: 'left'|'right'): Promise<MoveDto> {
    return http(`/game/session/${id}/move`, {
      method: 'POST',
      body: JSON.stringify({ figureId, direction: dir })
    })
  },
  spawnLayer(id: string): Promise<SpawnDto> {
    return http(`/game/session/${id}/figures/spawn-layer`, { method: 'POST' })
  },
  end(id: string): Promise<EndDto> {
    return http(`/game/session/${id}/end`, { method: 'POST' })
  }
}
