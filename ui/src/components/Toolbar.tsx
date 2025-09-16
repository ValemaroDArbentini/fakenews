// 📄 Назначение: Верхняя панель (счёт/комбо/пауза); Путь: /ui/src/components/Toolbar.tsx
import React from 'react'
import { useTranslation } from 'react-i18next'

export function Toolbar({ score, combo, onPause }:{ score:number, combo:number, onPause:()=>void }) {
  const { t } = useTranslation()
  return (
    <div className="flex items-center justify-between px-3 py-2 border-b bg-white/80 backdrop-blur sticky top-0 z-10">
      <div className="text-sm">{t('score')}: <span className="font-semibold">{score}</span></div>
      <div className="text-sm">{combo>1 ? `${t('combo')}: x${combo}` : ''}</div>
      <button onClick={onPause} className="px-3 py-1 rounded-xl border shadow-sm">{t('pause')}</button>
    </div>
  )
}
