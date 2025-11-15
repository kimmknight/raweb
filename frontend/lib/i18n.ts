import i18next from 'i18next';
import Backend from 'i18next-http-backend';
import I18NextVue from 'i18next-vue';
import { App } from 'vue';

const baseElement = document.querySelector('base');
const base = baseElement ? baseElement.getAttribute('href') ?? '' : '';

export const i18nextPromise = i18next
  .use(
    new Backend(null, {
      loadPath: base + 'locales/{{lng}}.json',
    })
  )
  .init({
    debug: false,
    lng: (() => {
      if (typeof window === 'undefined' || !('language' in navigator)) {
        return undefined;
      }
      return navigator.language;
    })(),
    fallbackLng: 'en',
  });

export default function (app: App) {
  app.use(I18NextVue, { i18next });
  return app;
}
