// 📄 Назначение: Vite конфиг; Путь: /ui/vite.config.ts
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: { port: 5173, host: true },
  preview: { port: 5174 },
  define: {
    __BUILD_TIME__: JSON.stringify(new Date().toISOString())
  }
})
