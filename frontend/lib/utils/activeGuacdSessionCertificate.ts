import { ref } from 'vue';

/**
 * The terminal server certificate of the active guacd web client session, as base64-encoded DER.
 *
 * This is set by the web client page while a session is connected so the titlebar can show
 * a button for viewing it.
 */
export const activeGuacdSessionCertificate = ref<string | null>(null);
