// 📄 Назначение: Точка входа; Путь: /ui/src/main.tsx
import React from 'react'
import ReactDOM from 'react-dom/client'
import './styles/tailwind.css'
import './i18n'
import { App } from './App'

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
)
