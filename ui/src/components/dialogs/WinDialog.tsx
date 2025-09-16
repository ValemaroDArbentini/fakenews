// 📄 Назначение: Диалог win‑момента; Путь: /ui/src/components/dialogs/WinDialog.tsx
import React from 'react'
import { useTranslation } from 'react-i18next'

export function WinDialog({ open, tier, onContinue, onExit }:{ open:boolean, tier:number, onContinue:()=>void, onExit:()=>void }) {
  const { t } = useTranslation()
  if (!open) return null
  return (
    <div className="fixed inset-0 bg-black/30 backdrop-blur flex items-center justify-center">
      <div className="bg-white rounded-2xl shadow-xl p-5 w-96">
        <h2 className="text-lg font-semibold mb-2">WIN x{tier}</h2>
        <div className="flex justify-end gap-2">
          <button onClick={onContinue} className="px-3 py-1 rounded-xl border">{t('continue')}</button>
          <button onClick={onExit} className="px-3 py-1 rounded-xl border">{t('enough_for_today')}</button>
        </div>
      </div>
    </div>
  )
}
