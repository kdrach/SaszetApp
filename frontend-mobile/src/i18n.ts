import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import plTranslation from './locales/pl/translation.json';
import enTranslation from './locales/en/translation.json';

const resources = {
  pl: {
    translation: plTranslation
  },
  en: {
    translation: enTranslation
  }
};

i18n
  .use(initReactI18next)
  .init({
    resources,
    lng: 'pl',
    fallbackLng: 'en',
    interpolation: {
      escapeValue: false
    }
  });

export default i18n;
