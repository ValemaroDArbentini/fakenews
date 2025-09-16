// 📄 Назначение: Общие типы; Путь: /ui/src/lib/types.ts
export type Coord = { x: number, y: number } // 1..11, 1..17

export interface FigureVm {
  id: string
  word: string
  head: string
  blocks: string[]
  isFixed: boolean
}

export interface BroadcastVm {
  phrase: string
  sourceRow: number
  wasWinMoment: boolean
  reaction: string
  sentAt: string
}

export interface GameStateDto {
  id: string
  score: number
  isWin: boolean
  locale: string
  figures: { id:string, word:string, headCoord:string, blockCoords:string[], isFixed:boolean }[]
  broadcast: BroadcastVm[]
}
