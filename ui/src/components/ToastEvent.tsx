// 📄 Назначение: Тост очков/комбо; Путь: /ui/src/components/ToastEvent.tsx
import React, { useEffect, useState } from 'react'

export function ToastEvent({ text }:{ text:string }) {
  const [show, setShow] = useState(true)
  useEffect(()=>{
    const t = setTimeout(()=>setShow(false), 700)
    return ()=>clearTimeout(t)
  },[])
  if (!show) return null
  return (
    <div className="absolute -translate-y-6 animate-bounce text-emerald-600 font-semibold">
      {text}
    </div>
  )
}
