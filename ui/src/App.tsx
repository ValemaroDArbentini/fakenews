// 📄 Назначение: Главный экран мини‑аппа с анимацией посадки, сгорания рядов и подсветкой новых слоёв; Путь: /ui/src/App.tsx
import React, { useEffect, useMemo, useRef, useState } from 'react'
import { Toolbar } from './components/Toolbar'
import { MovePad } from './components/MovePad'
import api from './api/api'

// === Типы и константы ===
type Fig = { id:string, word:string, headCoord:string, blockCoords:string[], isFixed:boolean }
const COLS = 11
const ROWS = 17
const letters = ['A','B','C','D','E','F','G','H','I','J','K']
const GUID_RE = /^[0-9a-fA-F-]{36}$/
const CELL_PX = 24 // ширина клетки в px — влияет на шаг драг-сдвига

function lenColorClass(len:number){
  switch(len){
    case 1: return 'text-amber-600 dark:text-amber-300'
    case 2: return 'text-sky-600 dark:text-sky-300'
    case 3: return 'text-emerald-600 dark:text-emerald-300'
    case 4: return 'text-violet-600 dark:text-violet-300'
    default: return 'text-rose-600 dark:text-rose-300'
  }
}

// Пастельные подложки и рамки по длине слова
function lenBg(len:number){
  switch(len){
    case 1: return 'bg-amber-100'
    case 2: return 'bg-sky-100'
    case 3: return 'bg-emerald-100'
    case 4: return 'bg-violet-100'
    default: return 'bg-rose-100'
  }
}
function lenBorder(len:number){
  switch(len){
    case 1: return 'border border-amber-300'
    case 2: return 'border border-sky-300'
    case 3: return 'border border-emerald-300'
    case 4: return 'border border-violet-300'
    default: return 'border border-rose-300'
  }
}

export function App(){
  // === Состояния ===
  const [sessionId, setSessionId] = useState('')
  const [figures, setFigures] = useState<Fig[]>([])
  const [selected, setSelected] = useState<string>('')
  const [score, setScore] = useState(0)
  const [combo, setCombo] = useState(1)
  const [ghostPath, setGhostPath] = useState<string[]>([])
  // анимации
  const [burnRows, setBurnRows] = useState<number[]>([])
  const [addedCells, setAddedCells] = useState<Set<string>>(new Set())
  const [settledCells, setSettledCells] = useState<Set<string>>(new Set())
  const [showPalette, setShowPalette] = useState(false)
  // фазовая анимация падения после сгорания
  const [isFalling, setIsFalling] = useState(false)
  const [fallDy, setFallDy] = useState<Map<string, number>>(new Map())
  const figuresBeforeRef = useRef<Fig[]|null>(null)

  // drag helpers
  const dragging = useRef(false)
  const dragStartX = useRef<number | undefined>(undefined)
  const dragAccumSteps = useRef(0)
  const previewTimer = useRef<number>(0)

  useEffect(()=>{ (async()=>{
    const s = await api.createSession()
    setSessionId(s.sessionId)
    await api.spawnLayer(s.sessionId)
    const st:any = await api.getSession(s.sessionId)
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

  // === Утилиты ===
  function getCellFromEvent(target: Element | null): string | null {
    let el: Element | null = target
    while (el && !(el instanceof HTMLElement)) el = el.parentElement
    while (el && !(el as HTMLElement).getAttribute?.('data-coord')) el = el.parentElement
    return (el as HTMLElement | null)?.getAttribute?.('data-coord') ?? null
  }
  function getFigureIdAtCoord(list:Fig[], coord:string|null){
    if(!coord) return ''
    return list.find(f=>f.blockCoords.includes(coord))?.id || ''
  }
  function pickFirst(){ setSelected(p=> p || figures[0]?.id || '') }
  function isGhost(coord:string){ return ghostPath.includes(coord) }
  const gridRows = Array.from({length:ROWS}, (_,i)=>ROWS-i)

  // === API действия ===
  async function doPreview(dir:'left'|'right', steps:number){
    if(!sessionId || !selected || steps<=0 || !GUID_RE.test(selected)) { setGhostPath([]); return }
    const r:any = await api.move(sessionId, { figureId: selected, direction: dir, steps, preview: true })
    if(Array.isArray(r?.path)) setGhostPath(r.path as string[])
  }

  async function commit(dir:'left'|'right', steps:number){
    if(!sessionId || !selected || steps<=0 || !GUID_RE.test(selected)) return

    // снимок «до»
    const beforeAll = new Set<string>()
    const beforeRowCount: Record<number, number> = {}
    for(let y=1;y<=ROWS;y++) beforeRowCount[y] = 0
    for(const f of figures){
      for(const c of f.blockCoords){
        beforeAll.add(c)
        const y = parseInt(c.slice(1),10)
        if(y>=1&&y<=ROWS) beforeRowCount[y]++
      }
    }

    const r:any = await api.move(sessionId, { figureId: selected, direction: dir, steps, preview: false })
    const st:any = await api.getSession(sessionId)
    const newFigures: Fig[] = st.figures || []
    setGhostPath([])
    setScore(p=>p + (r?.scoreGained||0))
    setCombo(r?.cascades>1 ? r.cascades : 1)

    // подготовим «падение» по ДО‑состоянию
    const afterRowCount: Record<number, number> = {}
    for(let y=1;y<=ROWS;y++) afterRowCount[y] = 0
    for(const f of newFigures){
      for(const c of f.blockCoords){
        const y = parseInt(c.slice(1),10)
        if(y>=1&&y<=ROWS) afterRowCount[y]++
      }
    }
    const burned:number[] = []
    for(let y=1;y<=ROWS;y++){
      if (beforeRowCount[y] === COLS && afterRowCount[y] < beforeRowCount[y]) burned.push(y)
    }
    setBurnRows(burned)

    const burnedSet = new Set(burned)
    const dyMap = new Map<string, number>()
    for(const f of figures){
      for(const c of f.blockCoords){
        const y = parseInt(c.slice(1),10)
        if (burnedSet.has(y)) continue
        const dy = burned.filter(by => by < y).length
        if (dy>0) dyMap.set(c, dy)
      }
    }
    figuresBeforeRef.current = figures
    setFallDy(dyMap)
    setIsFalling(true)

    // применим новое состояние после анимации падения
    window.setTimeout(()=>{
      setFigures(newFigures)
      setIsFalling(false)
      setFallDy(new Map())
      // подсветка действительно добавившихся клеток
      const afterAll = new Set<string>()
      for(const f of newFigures){ for(const c of f.blockCoords){ afterAll.add(c) } }
      const added = new Set<string>()
      afterAll.forEach(c => { if(!beforeAll.has(c)) added.add(c) })
      setAddedCells(added)
      window.setTimeout(()=>setAddedCells(new Set()), 800)
      const moved = newFigures.find(f => f.id === selected)
      setSettledCells(new Set(moved?.blockCoords ?? []))
      window.setTimeout(()=>setSettledCells(new Set()), 400)
      setBurnRows([])
    }, 550)

    // duplicate post-phase block removed (handled in falling phase above)
  }

  // === Drag handlers (один POST на отпускание) ===
  const onPointerDown = (e: React.PointerEvent<HTMLDivElement>) =>{
    // авто-выбор фигуры под курсором (если не было явного выбора)
    const coord = getCellFromEvent(e.target as Element)
    const idUnderPointer = getFigureIdAtCoord(figures, coord)
    if (idUnderPointer) setSelected(idUnderPointer)

    dragging.current = true
    dragStartX.current = e.clientX
    dragAccumSteps.current = 0
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
                  const baseFigures = (isFalling && figuresBeforeRef.current) ? figuresBeforeRef.current : figures
                  const figHere = baseFigures.find(f=>f.blockCoords.includes(coord))
                  const isSel = figHere?.id === selected
                  const colorClass = figHere ? lenColorClass(figHere.word?.length||1) : ''
                  const ghost = isGhost(coord)
                  const isBurning = burnRows.includes(y) && !!figHere
                  const isAdded   = addedCells.has(coord)
                  const isSettled = settledCells.has(coord)
                  // вычисляем роль клетки внутри слова для «сплошной плашки»
                  const colIndex = letters.indexOf(L as any)
                  const prevCoord = colIndex>0 ? `${letters[colIndex-1]}${y}` : null
                  const nextCoord = colIndex<COLS-1 ? `${letters[colIndex+1]}${y}` : null
                  const prevSame = !!(figHere && prevCoord && figHere.blockCoords.includes(prevCoord))
                  const nextSame = !!(figHere && nextCoord && figHere.blockCoords.includes(nextCoord))
                  const role = !figHere ? 'none' : (!prevSame && !nextSame ? 'single' : !prevSame ? 'start' : !nextSame ? 'end' : 'middle') as const
                  return (
                    <div key={`${L}-${y}`}
                         data-coord={coord}
                         className={`tile border aspect-square select-none ${figHere?'bg-sky-50 border-sky-200':'bg-white'}`}
                         onPointerDown={(e)=>{ if(figHere) setSelected(figHere.id) }}
                         onClick={()=>{ if(figHere) setSelected(figHere.id) }}>
                      <div className={`w-full h-full relative font-mono text-[13px] font-semibold flex items-center justify-center ${colorClass}`}
                          style={(()=>{ if(!isFalling) return undefined; const dy = fallDy.get(coord); if(!dy) return undefined; const col = letters.indexOf(L as any); return { transform:`translateY(${dy*CELL_PX}px)`, transition:'transform 420ms cubic-bezier(.22,.61,.36,1)', transitionDelay: `${col*35}ms` } })()}>
                        {/* Сплошная плашка слова: перекрываем внутренние зазоры */}
                        {figHere ? (
                          <span
                            className={`absolute inset-y-0 ${lenBg(figHere.word?.length||1)} ${lenBorder(figHere.word?.length||1)} ${role==='single'?'rounded-md':role==='start'?'rounded-l-md':role==='end'?'rounded-r-md':''} ${role==='middle'?'border-l-0 border-r-0': role==='start'?'border-r-0': role==='end'?'border-l-0':''}`}
                            style={{ left: (role==='single'||role==='start')? 0 : -2, right: (role==='single'||role==='end')? 0 : -2 }}
                          />
                        ) : null}
                        {/* Буква поверх плашки */}
                        {figHere ? <span className="relative z-[1]">{letterAt(figHere, coord)}</span> : null}
                        {ghost    ? <span className="absolute inset-0 opacity-30 bg-amber-300"/> : null}
                        {isSel    ? <span className="absolute inset-0 ring-2 ring-amber-400 pointer-events-none"/>: null}
                        <span className="absolute inset-0 pointer-events-none">
                          {isFalling && burnRows.includes(parseInt(coord.slice(1),10)) && figHere ? <span className="absolute inset-0 bg-amber-400/60 animate-[fadeout_500ms_ease-in_forwards]" /> : null}
                          {!isFalling && isAdded   ? <span className="absolute inset-0 bg-emerald-300/40 animate-[fadeout_800ms_ease-out_forwards]" /> : null}
                          {!isFalling && isSettled ? <span className="absolute inset-0 ring-2 ring-amber-400/80 rounded-sm animate-[fadeout_400ms_ease-out_forwards]" /> : null}
                        </span>
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
              <div key={`m-${y}`} className={`tile w-12 flex items-center justify-center text-xs ${rowFill[y]===COLS ? 'animate-pulse font-semibold text-emerald-600':'text-neutral-500'}`}>{rowFill[y]}/{COLS}</div>
            ))}
          </div>
        </div>

        {/* Палитра фигур/подписи — удалена как лишняя для UX */}
      </div>

      <MovePad
        onLeft ={()=>{ if(!selected) pickFirst(); void commit('left',1) }}
        onRight={()=>{ if(!selected) pickFirst(); void commit('right',1) }}
        onAuto ={()=>{ if(!selected) pickFirst() }}
      />
    </div>
  )
}

// Вычисляем символ слова, соответствующий конкретной клетке фигуры
function letterAt(fig: Fig, coord: string): string {
  const i = fig.blockCoords.indexOf(coord)
  if (i < 0) return '·'
  const ch = fig.word?.[i]
  return ch ?? '·'
}

// Экспорт и по имени, и по умолчанию
export default App
