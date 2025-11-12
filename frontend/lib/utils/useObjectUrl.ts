import { onBeforeUnmount, Ref, ref, watch } from 'vue';

/**
 * A helper that handles creating and revoking an object URL
 * for a given Blob. If the Blob changes, the previous URL is revoked
 * and a new one is created.
 */
export function useObjectUrl(sourceBlob: Ref<Blob | null>) {
  const objectUrl = ref<string | null>(null);

  watch(sourceBlob, (newBlob) => {
    // revoke previous URL
    if (objectUrl.value) {
      URL.revokeObjectURL(objectUrl.value);
      objectUrl.value = null;
    }

    if (newBlob) {
      objectUrl.value = URL.createObjectURL(newBlob);
    }
  });

  onBeforeUnmount(() => {
    if (objectUrl.value) URL.revokeObjectURL(objectUrl.value);
  });

  return objectUrl;
}
