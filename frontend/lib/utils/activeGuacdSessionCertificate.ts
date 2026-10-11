import { ref } from 'vue';

/**
 * The certificate of the active guacd web client session that RAWeb was able to inspect, as
 * base64-encoded DER. This is the gateway server's certificate when a gateway is used (the
 * terminal server is not directly reachable then) and the terminal server's certificate otherwise.
 *
 * This is set by the web client page so the titlebar can show a button for viewing it.
 */
export const activeGuacdSessionCertificate = ref<{
  /** The base64-encoded DER certificate. */
  value: string;
  /** Whether the certificate belongs to the gateway server instead of the terminal server. */
  isGateway: boolean;
} | null>(null);
