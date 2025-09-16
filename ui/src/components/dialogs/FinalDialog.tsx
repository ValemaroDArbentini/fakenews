// 📄 Назначение: Диалог финала (проигрыш); Путь: /ui/src/components/dialogs/FinalDialog.tsx
import React from 'react'
import { useTranslation } from 'react-i18next'

export function FinalDialog({ open, score, onAgain, onExit }:{ open:boolean, score:number, onAgain:()=>void, onExit:()=>void }) {
  const { t } = useTranslation()
  if (!open) return null
  return (
    <div className="fixed inset-0 bg-black/30 backdrop-blur flex items-center justify-center">
      <div className="bg-white rounded-2xl shadow-xl p-5 w-96">
        <h2 className="text-lg font-semibold mb-2">{t('game_over')}</h2>
        <div className="mb-4 text-sm opacity-70">Score: <b>{score}</b></div>
        <div className="flex justify-end gap-2">
          <button onClick={onAgain} className="px-3 py-1 rounded-xl border">{t('play_again')}</button>
          <button onClick={onExit} className="px-3 py-1 rounded-xl border">{t('enough_for_today')}</button>
        </div>
      </div>
    </div>
  )
}
