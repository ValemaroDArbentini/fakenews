// 📄 Назначение: Инициализация i18n; Путь: /ui/src/i18n/index.ts
import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'
import ru from './ru.json'
import en from './en.json'

const resources = { ru: { translation: ru }, en: { translation: en } }
const lang = (window.navigator.language || 'ru').slice(0,2)

i18n.use(initReactI18next).init({
  resources,
  lng: ['ru','en'].includes(lang) ? lang : 'ru',
  fallbackLng: 'ru',
  interpolation: { escapeValue: false }
})

export default i18n
