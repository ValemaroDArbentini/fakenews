// 📄 Назначение: Диалог паузы; Путь: /ui/src/components/dialogs/PauseDialog.tsx
import React from 'react'
import { useTranslation } from 'react-i18next'

export function PauseDialog({ open, onClose }:{ open:boolean, onClose:()=>void }) {
  const { t } = useTranslation()
  if (!open) return null
  return (
    <div className="fixed inset-0 bg-black/30 backdrop-blur flex items-center justify-center">
      <div className="bg-white rounded-2xl shadow-xl p-4 w-80">
        <h2 className="text-lg font-semibold mb-3">{t('pause')}</h2>
        <div className="flex justify-end gap-2">
          <button onClick={onClose} className="px-3 py-1 rounded-xl border">{t('continue')}</button>
        </div>
      </div>
    </div>
  )
}
