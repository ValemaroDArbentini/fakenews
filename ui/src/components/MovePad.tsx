// 📄 Назначение: Нижняя панель действий с авто‑повтором; 📍 Путь: /ui/src/components/MovePad.tsx
import React from 'react'
import { useTranslation } from 'react-i18next'

export function MovePad({ onLeft, onRight, onAuto }:{ onLeft:()=>void, onRight:()=>void, onAuto:()=>void }) {
  const { t } = useTranslation()
  return (
    <div className="flex gap-3 items-center justify-center p-3 border-t sticky bottom-0 bg-white/80 backdrop-blur">
      <HoldButton onFire={onLeft} className="px-4 py-3 rounded-2xl border shadow tile" ariaLabel="Move left">⟵</HoldButton>
      <HoldButton onFire={onRight} className="px-4 py-3 rounded-2xl border shadow tile" ariaLabel="Move right">⟶</HoldButton>
      <button onClick={onAuto} className="px-4 py-3 rounded-2xl border shadow text-sm">{t('autopick')}</button>
    </div>
  )
}

// Локальная кнопка с авто‑повтором
function HoldButton({ onFire, className, children, ariaLabel }:{ onFire:()=>void, className?:string, children:React.ReactNode, ariaLabel?:string }) {
  const start = (el: HTMLButtonElement | null, delay = 110) => {
    if (!el) return
    ;(el as any)._t = setInterval(onFire, delay)
    onFire() // первый «шаг» сразу
  }
  const stop = (el: HTMLButtonElement | null) => {
    if (!el) return
    const t = (el as any)._t
    if (t) clearInterval(t)
  }
  return (
    <button
      aria-label={ariaLabel}
      className={className}
      onMouseDown={e=>start(e.currentTarget, 90)}
      onMouseUp={e=>stop(e.currentTarget)}
      onMouseLeave={e=>stop(e.currentTarget)}
      onTouchStart={e=>start(e.currentTarget)}
      onTouchEnd={e=>stop(e.currentTarget)}
    >{children}</button>
  )
}