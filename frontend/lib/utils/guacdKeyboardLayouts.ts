import i18next from 'i18next';
import { computed, ref } from 'vue';

/**
 * The RDP keyboard layouts ("server-layout" values) supported by the server's guacd.
 *
 * Labels are built from the `language-region-layout` pattern with `Intl.DisplayNames` so they
 * are rendered in the user's language without needing translations for every layout.
 *
 * The last layout in the list is the "failsafe" layout, which serves as a fallback and
 * is not an actual keyboard layout.
 */
const serverLayouts = ref<string[]>([]);
let loadPromise: Promise<{ layout: string; layouts: string[] }> | null = null;

/**
 * Loads the layouts supported by the server.
 */
export function loadServerKeyboardLayouts(iisBase: string) {
  loadPromise ??= fetch(`${iisBase}api/management/server-keyboard-layout`)
    .then(async (res) => {
      if (!res.ok) {
        throw new Error(`Failed to load server keyboard layouts (${res.status})`);
      }
      const result = (await res.json()) as { layout: string; layouts: string[] };
      serverLayouts.value = result.layouts;
      return result;
    })
    .catch((error) => {
      loadPromise = null;
      throw error;
    });
  return loadPromise;
}

function labelFor(value: string): string {
  const [language, region, layout] = value.split('-');
  if (!language || !region || !layout) {
    return i18next.t('client.serverLayout.failsafe');
  }

  const displayNames = new Intl.DisplayNames([i18next.language || 'en'], {
    type: 'language',
    languageDisplay: 'standard',
  });
  // for latin america, guacd uses "latam" but BCP 47 uses code 419
  const locale = `${language}-${region === 'latam' ? '419' : region.toUpperCase()}`;
  return `${displayNames.of(locale) || locale} (${layout.toUpperCase()})`;
}

// the label is a getter so it is resolved with the current language each time it is rendered
export const guacdKeyboardLayouts = computed(() =>
  serverLayouts.value.map((value) => ({
    value,
    get label() {
      return labelFor(value);
    },
  }))
);

/**
 * Translates a guacd keyboard layout value into a human-readable label.
 *
 * For example, a value like "en-US-qwerty" would be translated into a
 * human-readable label based on the user's language. In English, it
 * would be translated into  "English (United States) (QWERTY)".
 */
export function guacdKeyboardLayoutLabel(value: string | undefined): string {
  return value ? labelFor(value) : '';
}
