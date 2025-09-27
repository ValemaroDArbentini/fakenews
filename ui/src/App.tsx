import React, { useEffect, useMemo, useRef, useState } from 'react'
import { Toolbar } from './components/Toolbar'
import { MovePad } from './components/MovePad'
import { FigureCapsule } from './components/Grid'

import api from './api/api'

// === Типы и константы ===
type Fig = { id:string, word:string, headCoord:string, blockCoords:string[], isFixed:boolean }
const COLS = 11
const ROWS = 17
const letters = ['A','B','C','D','E','F','G','H','I','J','K']
const GUID_RE = /^[0-9a-fA-F-]{36}$/
const CELL_PX = 24 // ширина клетки в px — влияет на шаг драг-сдвига

function xFrom(coord: string) {
  return coord.charCodeAt(0) - 64; // 'A'->1
}

function letterAt(fig: Fig, coord: string) {
  const i = xFrom(coord) - xFrom(fig.headCoord); // позиция буквы слева направо
  return fig.word?.[i] ?? '';                     // защитимся, если длина не совпала
}

function lenColorClass(len:number){
  switch(len){
    case 1: return 'text-amber-600 dark:text-amber-300'
    case 2: return 'text-sky-600 dark:text-sky-300'
    case 3: return 'text-emerald-600 dark:text-emerald-300'
    case 4: return 'text-violet-600 dark:text-violet-300'
    default: return 'text-rose-600 dark:text-rose-300'
  }
}

export function App(){
  // === Состояния ===
  const [sessionId, setSessionId] = useState('')
  const [figures, setFigures] = useState<Fig[]>([])
  const [score, setScore] = useState(0)
  const [combo, setCombo] = useState(1)
  const [selected, setSelected] = useState<string|undefined>()
  const [ghostPath, setGhostPath] = useState<string[]>([])

  // === refs для drag ===
  const dragStartX = useRef<number|undefined>()
  const dragAccumSteps = useRef<number>(0)
  const dragging = useRef<boolean>(false)
  const previewTimer = useRef<number|undefined>()

  // === Получение стартового состояния сессии ===
  useEffect(()=>{ (async()=>{
    const s = await api.createSession();       // was: api.start()
    setSessionId(s.sessionId)

    await api.spawnLayer(s.sessionId)          // тот же метод
    const st:any = await api.getSession(s.sessionId) // was: api.state(...)
    setFigures(st.figures || [])
  })() },[])

  // === Подсчёт заполнения строк ===
  const rowFill = useMemo(()=>{
    const byRow: Record<number, Set<string>> = {}
    for(let y=1;y<=ROWS;y++) byRow[y] = new Set<string>()
    for(const f of figures){
      for(const c of f.blockCoords){
        const y = parseInt(c.slice(1),10)
        if(y>=1&&y<=ROWS) byRow[y].add(c)
      }
    }
    const res: Record<number, number> = {}
    for(let y=1;y<=ROWS;y++) res[y] = Math.min(COLS, byRow[y].size)
    return res
  },[figures])

  // === Утилиты выбора клетки/фигуры под курсором ===
  function getCellFromEvent(target: Element | null): string | null {
    let el: Element | null = target
    while (el && !(el instanceof HTMLElement)) el = el.parentElement
    while (el && !(el as HTMLElement).getAttribute?.('data-coord')) el = el.parentElement
    return (el as HTMLElement | null)?.getAttribute?.('data-coord') ?? null
  }
  function getFigureIdAtCoord(figs: Fig[], coord: string | null): string | undefined {
    if (!coord) return undefined
    const f = figs.find(f => f.blockCoords.includes(coord))
    return f?.id
  }

  function pickFirst(){ if(figures[0]) setSelected(figures[0].id) }
  function isGhost(coord:string){ return ghostPath.includes(coord) }
  const gridRows = Array.from({length:ROWS}, (_,i)=>ROWS-i)

  // === API действия ===
  async function doPreview(dir:'left'|'right', steps:number){
    if(!sessionId || !selected || steps<=0 || !GUID_RE.test(selected)) { setGhostPath([]); return }
    const r:any = await api.move(sessionId, { figureId: selected, direction: dir, steps, preview: true })
    setGhostPath(r?.path || [])
  }
  async function commit(dir:'left'|'right', steps:number){
    if(!sessionId || !selected || steps<=0 || !GUID_RE.test(selected)) return
    const r:any = await api.move(sessionId, { figureId: selected, direction: dir, steps, preview: false })
    const st:any = await api.getSession(sessionId) // was: api.state(...)
    setFigures(st.figures || [])
    setGhostPath([])
    setScore(p=>p + (r?.scoreGained||0))
    setCombo(r?.cascades>1 ? r.cascades : 1)
  }

  // === Drag handlers (один POST на отпускание) ===
  const onPointerDown = (e: React.PointerEvent<HTMLDivElement>) =>{
    // авто-выбор фигуры под курсором (если не было явного выбора)
    const coord = getCellFromEvent(e.target as Element)
    const idUnderPointer = getFigureIdAtCoord(figures, coord)
    if (idUnderPointer) setSelected(idUnderPointer)
    if (!idUnderPointer && !selected) return // без фигуры drag не стартуем

    dragging.current = true
    dragStartX.current = e.clientX
    dragAccumSteps.current = 0
    window.clearTimeout(previewTimer.current)
  }

  const onPointerMove = (e: React.PointerEvent<HTMLDivElement>) =>{
    if(!dragging.current || dragStartX.current===undefined) return
    if(!selected || !GUID_RE.test(selected)) return

    const dx = e.clientX - dragStartX.current
    const steps = Math.floor(Math.abs(dx) / CELL_PX)
    if(steps === dragAccumSteps.current) return

    dragAccumSteps.current = steps
    const dir = dx>0?'right':'left'

    window.clearTimeout(previewTimer.current)
    if (steps > 0) {
      previewTimer.current = window.setTimeout(()=>{ void doPreview(dir, steps) }, 80)
    } else {
      setGhostPath([])
    }
  }

  const onPointerUp = (e: React.PointerEvent<HTMLDivElement>) =>{
    if(!dragging.current || dragStartX.current===undefined) return

    const dx = e.clientX - dragStartX.current
    const steps = Math.floor(Math.abs(dx) / CELL_PX)
    const dir = dx>0?'right':'left'

    dragging.current = false
    dragStartX.current = undefined
    window.clearTimeout(previewTimer.current)
    setGhostPath([])

    if(steps>0 && selected && GUID_RE.test(selected)) {
      void commit(dir, steps)
    }
  }

  return (
    <div className="min-h-dvh flex flex-col">
      <Toolbar score={score} combo={combo} onPause={()=>{}} />

      <div className="flex-1 p-3 flex flex-col items-center">
        <div className="flex items-start gap-2" onPointerDown={onPointerDown} onPointerMove={onPointerMove} onPointerUp={onPointerUp}>
          {/* Левый метр (номера строк) */}
          <div className="flex flex-col gap-[2px] mr-2 select-none text-[11px] text-neutral-500">
            {gridRows.map(y=> (<div key={`ln-${y}`} className="tile w-6 flex items-center justify-center">{y}</div>))}
          </div>

          {/* Игровая сетка */}
          <div className="grid grid-cols-11 gap-[2px]">
            {gridRows.map(y => (
              <React.Fragment key={y}>
                {letters.map((L)=>{
                  const coord = `${L}${y}`
                  const figHere = figures.find(f=>f.blockCoords.includes(coord))
                  const isSel = figHere?.id === selected
                  const colorClass = figHere ? lenColorClass(figHere.word?.length||1) : ''
                  const ghost = isGhost(coord)
                  return (
                    <div key={`${L}-${y}`}
                         data-coord={coord}
                         className={`tile border aspect-square select-none ${figHere?'bg-sky-50 border-sky-200':'bg-white'}`}
                         onPointerDown={(e)=>{ if(figHere) setSelected(figHere.id) }}
                         onClick={()=>{ if(figHere) setSelected(figHere.id) }}>
                      <div className={`w-full h-full relative ${colorClass}`}>
                        {figHere ? <span>{letterAt(figHere, coord)}</span> : null}
                        {ghost ? <span className="absolute inset-0 opacity-30 bg-amber-300"/> : null}
                        {isSel ? <span className="absolute inset-0 ring-2 ring-amber-400 pointer-events-none"/>: null}
                      </div>
                    </div>
                  )
                })}
              </React.Fragment>
            ))}
          </div>

          {/* Правый метр (заполненность строк) */}
          <div className="flex flex-col gap-[2px] ml-2 select-none">
            {gridRows.map(y => (
              <div key={`m-${y}`} className={`tile w-12 flex items-center justify-end pr-1 text-xs ${rowFill[y]===COLS?'animate-pulse font-semibold text-emerald-600':'text-neutral-500'}`}>{rowFill[y]}/{COLS}</div>
            ))}
          </div>
        </div>

        {/* Подписи колонок */}
        <div className="grid grid-cols-11 gap-[2px] mt-1 select-none text-[11px] text-neutral-500">
          {letters.map(L => (<div key={`col-${L}`} className="tile w-6 h-6 flex items-center justify-center">{L}</div>))}
        </div>
      </div>

      <MovePad
        onLeft ={()=>{ if(!selected) pickFirst(); void commit('left',1) }}
        onRight={()=>{ if(!selected) pickFirst(); void commit('right',1) }}
        onAuto ={()=>{ if(!selected) pickFirst() }}
      />
    </div>
  )
}

// Экспорт и по имени, и по умолчанию
export default App
