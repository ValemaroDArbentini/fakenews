// 📄 Назначение: Tailwind конфиг; Путь: /ui/tailwind.config.ts
import type { Config } from 'tailwindcss'
export default {
  content: ['./index.html','./src/**/*.{ts,tsx}'],
  theme: {
    extend: {
      gridTemplateColumns: {
        '11': 'repeat(11, minmax(0, 1fr))'
      }
    }
  },
  plugins: []
} satisfies Config
