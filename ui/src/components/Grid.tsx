// 📄 Назначение: Игровое поле 11×17 (примитивные ячейки/капсулы); 📍 Путь: /ui/src/components/Grid.tsx
import React from 'react'

type CellState = 'empty'|'occupied'|'ghost'

export function Cell({ state, highlight, children }:{ state:CellState, highlight?:boolean, children?:React.ReactNode }) {
  return (
    <div className={`tile border aspect-square flex items-center justify-center select-none
      ${state==='empty' ? 'bg-white' : 'bg-sky-50 border-sky-200'}
      ${highlight ? 'ring-2 ring-amber-400' : ''}`}>
      {children}
    </div>
  )
}

export function FigureCapsule({ word, selected }:{ word:string, selected?:boolean }) {
  return (
    <div className={`w-full h-full rounded-xl flex items-center justify-center font-mono text-sm tracking-widest
      ${selected ? 'animate-pulse ring-2 ring-amber-400' : ''}`}>{word}</div>
  )
}

export function RowMeter({ filled, full }:{ filled:number, full:boolean }) {
  return (
    <div className={`text-xs w-12 text-right pr-1 ${full ? 'animate-pulse font-semibold text-emerald-600' : 'text-neutral-500'}`}>
      {filled}/11
    </div>
  )
}