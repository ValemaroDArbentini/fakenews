// 📄 Назначение: Каркас UI для Telegram‑миниаппа «Блок‑Башня»; Путь: /ui/README.md

# TelegramBlock UI (Vite + React + Tailwind)

Дата сборки: 2025-08-21T12:30:17.469291Z

## Быстрый старт
```bash
# 1) Установить зависимости
npm ci

# 2) Запуск дев‑сервера
VITE_API_BASE_URL=http://localhost:8081 npm run dev

# 3) Превью прод‑сборки
npm run build && npm run preview
```

## Docker
```bash
# локальный билд и запуск
docker build -t telegramblock-ui:dev .
docker run --rm -p 8080:80 -e VITE_API_BASE_URL=http://localhost:8081 telegramblock-ui:dev

# через docker compose
docker compose up --build
```
Переменная `VITE_API_BASE_URL` должна указывать на ваш API (см. backend контроллеры).
