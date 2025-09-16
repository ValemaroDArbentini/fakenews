// 📄 Назначение: Контракты API; Путь: /ui/src/api/dto.ts
export type StartDto = { sessionId: string, locale: string, startTime: string }
export type MoveDto = {
  success: boolean
  newCoords?: string[]
  rowsBurned?: number
  cascades?: number
  scoreGained?: number
  totalFigures?: number
  gameOver?: boolean
  error?: string
}
export type SpawnDto = {
  gameOver: boolean
  added?: { id:string, word:string, length:number, headCoord:string, blockCoords:string[] }[]
  totalFigures?: number
  reason?: string
}
export type EndDto = { sessionId: string, endedAt: string, score: number, isWin: boolean }
