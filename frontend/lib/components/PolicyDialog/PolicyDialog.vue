<script setup lang="ts">
  import { Button, IconButton, RadioButton, TextBlock, TextBox } from '$components';
  import { useCoreDataStore } from '$stores';
  import { openHelpPopup, raw, raw as unproxify } from '$utils';
  import { useTranslation } from 'i18next-vue';
  import { computed, reactive, ref, useTemplateRef, watchEffect } from 'vue';
  import ContentDialog from '../ContentDialog/ContentDialog.vue';

  interface ExtraFieldSpecBase {
    key: string;
    label?: string;
  }

  interface ExtraFieldSpecKeyValue extends ExtraFieldSpecBase {
    type: 'key-value';
    multiple: true;
    keyValueLabels?: string[];
    interpret: (value: string) => [string, string][];
  }

  interface ExtraFieldSpecString extends ExtraFieldSpecBase {
    type: 'string';
    multiple?: boolean;
    interpret?: (value: string) => string | [string, string][];
  }

  interface ExtraFieldSpecJson extends ExtraFieldSpecBase {
    type: 'json';
    multiple?: boolean;
    /**
     * An object indicating the field ids and display labels for the json type.
     * If the value is a tuple, the first element is the label and the second is
     * a selection of options that will be presented as a dropdown.
     */
    jsonFields?: Record<string, string | [string, string]>;
    keyValueLabels?: string[];
    interpret?: (value: string) => Record<string, string>[];
  }

  interface ExtraFieldSpecBoolean extends ExtraFieldSpecBase {
    type: 'boolean';
    multiple?: false;
    keyValueLabels?: string[];
    interpret?: (value: string) => string;
  }

  /** Image upload with preview. */
  interface ExtraFieldSpecImage extends ExtraFieldSpecBase {
    type: 'image';
    multiple?: false;
    /** URL of image to display when no image has been uploaded. */
    defaultSrc?: string;
    /** Target pixel dimensions for the preview box and canvas resize on upload. */
    dimensions?: { width: number; height: number };
    interpret?: (value: string) => string;
  }

  type ExtraFieldSpec =
    | ExtraFieldSpecKeyValue
    | ExtraFieldSpecString
    | ExtraFieldSpecJson
    | ExtraFieldSpecBoolean
    | ExtraFieldSpecImage;

  const { title, name, extraFields, stringValue, appliesTo, initialState } = defineProps<{
    title: string;
    name: string;
    extraFields?: ExtraFieldSpec[];
    stringValue?: string;
    appliesTo: string[];
    initialState?: 'disabled' | 'enabled' | 'unset';
  }>();

  const { t } = useTranslation();
  const { docsUrl } = useCoreDataStore();

  const state = defineModel<'disabled' | 'enabled' | 'unset'>('state', {
    default: 'unset',
  });
  watchEffect(() => {
    if (initialState) {
      state.value = initialState;
    }
  });

  const extraFieldsState = ref<Record<string, string | [string, string][] | Record<string, string>[]>>({});
  watchEffect(() => {
    extraFields?.forEach((field) => {
      extraFieldsState.value[field.key] = field.interpret?.(stringValue || '') ?? (stringValue || '');
    });
  });

  const emit = defineEmits<{
    (
      e: 'save',
      /** Closes the dialog, or respond with a command to not close the dialog (set shouldClose to false) */
      closeDialog: (shouldClose?: boolean) => void,
      state: boolean | null,
      extra?: Record<string, string | [string, string][] | Record<string, string>[]>
    ): void;
  }>();

  const saving = ref(false);
  function handleSave() {
    saving.value = true;

    const close = (shouldClose = true) => {
      if (shouldClose) {
        closeDialog.value?.();
      }
      saving.value = false;
    };

    if (state.value === 'unset') {
      emit('save', close, null);
    } else if (state.value === 'enabled') {
      emit('save', close, true, unproxify(extraFieldsState.value));
    } else {
      emit('save', close, false, unproxify(extraFieldsState.value));
    }
  }

  const dialog = useTemplateRef<typeof ContentDialog>('dialog');
  const popoverId = computed(() => raw(dialog.value)?.popoverId as string | undefined);
  const openDialog = computed(() => raw(dialog.value)?.open as () => void);
  const closeDialog = computed(() => raw(dialog.value)?.close as () => void);

  const imageInputRefs = reactive<Record<string, HTMLInputElement | null>>({});

  /**
   * Scales the image dimensions to fit within a 192x192 box while maintaining aspect ratio.
   *
   * If the dimensions are already smaller than 192x192, they will be returned unchanged.
   */
  function getImageDisplaySize(dimensions: { width: number; height: number }): {
    width: number;
    height: number;
  } {
    const maxSize = 192;
    const scale = Math.min(1, maxSize / Math.max(dimensions.width, dimensions.height));
    return { width: Math.round(dimensions.width * scale), height: Math.round(dimensions.height * scale) };
  }

  /**
   * Resizes an image file to fit within the specified width and height while maintaining aspect ratio.
   *
   * The resize process uses a canvas to draw the image at the new size and returns a data URL of the
   * resized image. Any image supported by the browser can be resized.
   *
   * If an image does not match the aspect ratio of the specified dimensions, the input image
   * will be centered on the canvas and the extra space will remain transparent.
   */
  async function resizeImageToFit(file: File, width: number, height: number): Promise<string> {
    return new Promise((resolve, reject) => {
      const img = new Image();
      const objectUrl = URL.createObjectURL(file);

      img.onload = () => {
        const canvas = document.createElement('canvas');
        canvas.width = width;
        canvas.height = height;
        const ctx = canvas.getContext('2d');
        if (!ctx) {
          URL.revokeObjectURL(objectUrl);
          reject(new Error('Canvas context unavailable'));
          return;
        }
        const scale = Math.min(width / img.naturalWidth, height / img.naturalHeight);
        const drawWidth = img.naturalWidth * scale;
        const drawHeight = img.naturalHeight * scale;
        ctx.clearRect(0, 0, width, height);
        ctx.drawImage(img, (width - drawWidth) / 2, (height - drawHeight) / 2, drawWidth, drawHeight);
        URL.revokeObjectURL(objectUrl);
        resolve(canvas.toDataURL('image/png', 0.8)); // we need to use png instead of webp because many clients only support PNG and ICO
      };

      img.onerror = () => {
        URL.revokeObjectURL(objectUrl);
        reject(new Error('Image load failed'));
      };

      img.src = objectUrl;
    });
  }

  /**
   * Handles the change event for an image file input. If dimensions are
   * provided, the image will be resized to fit within those dimensions before
   * being stored in the extraFieldsState.
   *
   * The resized image is stored as a data URL. If resizing fails, the original
   * image is read as a data URL and stored instead.
   */
  async function handleImageChange(key: string, event: Event, dimensions?: { width: number; height: number }) {
    if (!(event.target instanceof HTMLInputElement)) {
      return;
    }

    const file = event.target?.files?.[0];
    if (!file) {
      return;
    }

    // when possible, resize the image to fit within the dimensions
    if (dimensions) {
      try {
        extraFieldsState.value[key] = await resizeImageToFit(file, dimensions.width, dimensions.height);
        return;
      } catch {}
    }

    // fall back to directly returning the file as a data URL
    const reader = new FileReader();
    reader.onload = () => {
      extraFieldsState.value[key] = reader.result as string;
    };
    reader.readAsDataURL(file);
  }

  function resetState() {
    if (initialState) {
      state.value = initialState;
      extraFieldsState.value = {};
      extraFields?.forEach((field) => {
        extraFieldsState.value[field.key] = field.interpret?.(stringValue || '') ?? (stringValue || '');
      });
    }
  }

  // helper that narrows to the object[] case
  function getJsonFieldArray(key: string): Record<string, string>[] {
    const vals = extraFieldsState.value[key];
    if (Array.isArray(vals) && vals.length > 0 && typeof vals[0] === 'object') {
      return vals as Record<string, string>[];
    }
    if (vals && typeof vals === 'object' && !Array.isArray(vals)) {
      // handle the case where a single object was provided for a multiple json field
      return [vals as Record<string, string>];
    }
    return [];
  }
</script>

<template>
  <slot :popoverId :openDialog></slot>
  <ContentDialog
    :title
    ref="dialog"
    size="max"
    style="max-inline-size: 800px"
    @afterClose="resetState"
    :closeOnBackdropClick="false"
    :help-action="() => openHelpPopup(`${docsUrl}/policies/${name}`)"
  >
    <div class="grid">
      <section style="grid-area: help">
        <div>
          <TextBlock variant="bodyStrong" style="font-size: 16px; margin: 8px 0 16px 0">
            {{ t('policies.dialog.help') }}
          </TextBlock>
        </div>
        <TextBlock variant="body" style="white-space: break-spaces; overflow-wrap: anywhere">
          {{ t(`policies.${name}.help`) }}
          <br />
          <br />Applies to: {{ appliesTo.join(', ') }}
        </TextBlock>
      </section>
      <section v-if="popoverId">
        <TextBlock variant="bodyStrong" style="font-size: 16px; margin: 8px 0">
          {{ t('policies.dialog.state') }}
        </TextBlock>
        <RadioButton :name="'state' + popoverId" value="unset" v-model:state="state">
          {{ t('policies.state.unset') }}
        </RadioButton>
        <RadioButton :name="'state' + popoverId" value="enabled" v-model:state="state">
          {{ t('policies.state.enabled') }}
        </RadioButton>
        <RadioButton :name="'state' + popoverId" value="disabled" v-model:state="state">
          {{ t('policies.state.disabled') }}
        </RadioButton>
      </section>
      <section v-if="extraFields && extraFields.length > 0">
        <TextBlock variant="bodyStrong" style="font-size: 16px; margin: 8px 0">
          {{ t('policies.dialog.options') }}
        </TextBlock>
        <div v-for="field in extraFields" :key="field.key" :class="`extra-field-block type-${field.type}`">
          <TextBlock variant="body" style="margin: 6px 0">{{ field.label || field.key }}</TextBlock>
          <template v-if="field.type === 'key-value'">
            <template v-if="field.multiple && extraFieldsState[field.key]">
              <fieldset v-for="(value, index) in extraFieldsState[field.key]" :key="index">
                <IconButton
                  v-if="state === 'enabled'"
                  @click="
                    () => {
                      const values = extraFieldsState[field.key];
                      if (field.multiple && Array.isArray(values)) {
                        values.splice(index, 1);
                      }
                    }
                  "
                  class="remove-button"
                >
                  <svg
                    width="24"
                    height="24"
                    fill="none"
                    viewBox="0 0 24 24"
                    xmlns="http://www.w3.org/2000/svg"
                  >
                    <path
                      d="M12 1.75a3.25 3.25 0 0 1 3.245 3.066L15.25 5h5.25a.75.75 0 0 1 .102 1.493L20.5 6.5h-.796l-1.28 13.02a2.75 2.75 0 0 1-2.561 2.474l-.176.006H8.313a2.75 2.75 0 0 1-2.714-2.307l-.023-.174L4.295 6.5H3.5a.75.75 0 0 1-.743-.648L2.75 5.75a.75.75 0 0 1 .648-.743L3.5 5h5.25A3.25 3.25 0 0 1 12 1.75Zm6.197 4.75H5.802l1.267 12.872a1.25 1.25 0 0 0 1.117 1.122l.127.006h7.374c.6 0 1.109-.425 1.225-1.002l.02-.126L18.196 6.5ZM13.75 9.25a.75.75 0 0 1 .743.648L14.5 10v7a.75.75 0 0 1-1.493.102L13 17v-7a.75.75 0 0 1 .75-.75Zm-3.5 0a.75.75 0 0 1 .743.648L11 10v7a.75.75 0 0 1-1.493.102L9.5 17v-7a.75.75 0 0 1 .75-.75Zm1.75-6a1.75 1.75 0 0 0-1.744 1.606L10.25 5h3.5A1.75 1.75 0 0 0 12 3.25Z"
                      fill="currentColor"
                    />
                  </svg>
                </IconButton>
                <label>
                  <TextBlock variant="body" :disabled="state !== 'enabled'">
                    {{ field.keyValueLabels?.[0] || 'Key' }}
                  </TextBlock>
                  <TextBox
                    v-model:value="extraFieldsState[field.key][index][0]"
                    :placeholder="field.keyValueLabels?.[0]"
                    :disabled="state !== 'enabled'"
                  />
                </label>
                <label>
                  <TextBlock variant="body" :disabled="state !== 'enabled'">
                    {{ field.keyValueLabels?.[1] || 'Value' }}
                  </TextBlock>
                  <TextBox
                    v-model:value="extraFieldsState[field.key][index][1]"
                    :placeholder="field.keyValueLabels?.[1]"
                    :disabled="state !== 'enabled'"
                  />
                </label>
              </fieldset>
              <div class="extra-fields-actions-row">
                <Button
                  :disabled="state !== 'enabled'"
                  @click="
                    () => {
                      const values = extraFieldsState[field.key];
                      if (
                        field.multiple &&
                        Array.isArray(values) &&
                        values.every((val) => Array.isArray(val))
                      ) {
                        values.push(['', '']);
                      }
                    }
                  "
                >
                  Add new
                </Button>
                <Button
                  :disabled="state !== 'enabled' || extraFieldsState[field.key].length === 0"
                  @click="
                    () => {
                      extraFieldsState[field.key] = [];
                    }
                  "
                >
                  Clear all
                </Button>
              </div>
            </template>
          </template>

          <template v-if="field.type === 'json'">
            <template v-if="extraFieldsState[field.key]">
              <fieldset v-for="(value, index) in getJsonFieldArray(field.key)" :key="index">
                <IconButton
                  v-if="field.multiple && state === 'enabled'"
                  @click="
                    () => {
                      const values = extraFieldsState[field.key];
                      if (field.multiple && Array.isArray(values)) {
                        values.splice(index, 1);
                      }
                    }
                  "
                  class="remove-button"
                >
                  <svg
                    width="24"
                    height="24"
                    fill="none"
                    viewBox="0 0 24 24"
                    xmlns="http://www.w3.org/2000/svg"
                  >
                    <path
                      d="M12 1.75a3.25 3.25 0 0 1 3.245 3.066L15.25 5h5.25a.75.75 0 0 1 .102 1.493L20.5 6.5h-.796l-1.28 13.02a2.75 2.75 0 0 1-2.561 2.474l-.176.006H8.313a2.75 2.75 0 0 1-2.714-2.307l-.023-.174L4.295 6.5H3.5a.75.75 0 0 1-.743-.648L2.75 5.75a.75.75 0 0 1 .648-.743L3.5 5h5.25A3.25 3.25 0 0 1 12 1.75Zm6.197 4.75H5.802l1.267 12.872a1.25 1.25 0 0 0 1.117 1.122l.127.006h7.374c.6 0 1.109-.425 1.225-1.002l.02-.126L18.196 6.5ZM13.75 9.25a.75.75 0 0 1 .743.648L14.5 10v7a.75.75 0 0 1-1.493.102L13 17v-7a.75.75 0 0 1 .75-.75Zm-3.5 0a.75.75 0 0 1 .743.648L11 10v7a.75.75 0 0 1-1.493.102L9.5 17v-7a.75.75 0 0 1 .75-.75Zm1.75-6a1.75 1.75 0 0 0-1.744 1.606L10.25 5h3.5A1.75 1.75 0 0 0 12 3.25Z"
                      fill="currentColor"
                    />
                  </svg>
                </IconButton>
                <label v-for="[key, label] in Object.entries(field.jsonFields || {})" :key="key">
                  <TextBlock variant="body" :disabled="state !== 'enabled'">
                    {{ typeof label === 'string' ? label : label[0] }}
                  </TextBlock>
                  <TextBox
                    v-if="typeof label === 'string'"
                    v-model:value="value[key]"
                    :placeholder="field.keyValueLabels?.[0]"
                    :disabled="state !== 'enabled'"
                  />
                  <select v-else v-model="value[key]" :disabled="state !== 'enabled'">
                    <option value="" disabled hidden>Select...</option>
                    <option v-for="option in label[1].split('|')" :key="option" :value="option.trim()">
                      {{ option.trim() }}
                    </option>
                  </select>
                </label>
              </fieldset>
              <div v-if="field.multiple" class="extra-fields-actions-row">
                <Button
                  :disabled="state !== 'enabled'"
                  @click="
                    () => {
                      const values = extraFieldsState[field.key];
                      if (
                        field.multiple &&
                        Array.isArray(values) &&
                        values.every(
                          (val): val is Record<string, string> => typeof val === 'object' && !Array.isArray(val)
                        )
                      ) {
                        values.push({});
                      }
                    }
                  "
                >
                  Add new
                </Button>
                <Button
                  :disabled="state !== 'enabled' || extraFieldsState[field.key].length === 0"
                  @click="
                    () => {
                      extraFieldsState[field.key] = [];
                    }
                  "
                >
                  Clear all
                </Button>
              </div>
            </template>
          </template>

          <template v-if="field.type === 'string'">
            <template v-if="!field.multiple">
              <TextBox v-model:value="extraFieldsState[field.key] as string" :disabled="state !== 'enabled'" />
            </template>
            <template v-if="field.multiple && extraFieldsState[field.key]">
              <fieldset v-for="(value, index) in extraFieldsState[field.key]" :key="index">
                <IconButton
                  v-if="state === 'enabled'"
                  @click="
                    () => {
                      const values = extraFieldsState[field.key];
                      if (field.multiple && Array.isArray(values)) {
                        values.splice(index, 1);
                      }
                    }
                  "
                  class="remove-button"
                >
                  <svg
                    width="24"
                    height="24"
                    fill="none"
                    viewBox="0 0 24 24"
                    xmlns="http://www.w3.org/2000/svg"
                  >
                    <path
                      d="M12 1.75a3.25 3.25 0 0 1 3.245 3.066L15.25 5h5.25a.75.75 0 0 1 .102 1.493L20.5 6.5h-.796l-1.28 13.02a2.75 2.75 0 0 1-2.561 2.474l-.176.006H8.313a2.75 2.75 0 0 1-2.714-2.307l-.023-.174L4.295 6.5H3.5a.75.75 0 0 1-.743-.648L2.75 5.75a.75.75 0 0 1 .648-.743L3.5 5h5.25A3.25 3.25 0 0 1 12 1.75Zm6.197 4.75H5.802l1.267 12.872a1.25 1.25 0 0 0 1.117 1.122l.127.006h7.374c.6 0 1.109-.425 1.225-1.002l.02-.126L18.196 6.5ZM13.75 9.25a.75.75 0 0 1 .743.648L14.5 10v7a.75.75 0 0 1-1.493.102L13 17v-7a.75.75 0 0 1 .75-.75Zm-3.5 0a.75.75 0 0 1 .743.648L11 10v7a.75.75 0 0 1-1.493.102L9.5 17v-7a.75.75 0 0 1 .75-.75Zm1.75-6a1.75 1.75 0 0 0-1.744 1.606L10.25 5h3.5A1.75 1.75 0 0 0 12 3.25Z"
                      fill="currentColor"
                    />
                  </svg>
                </IconButton>
                <label>
                  <TextBlock variant="body" :disabled="state !== 'enabled'"> Property </TextBlock>
                  <TextBox
                    v-model:value="extraFieldsState[field.key][index][0]"
                    :disabled="state !== 'enabled'"
                  />
                </label>
              </fieldset>
              <div class="extra-fields-actions-row">
                <Button
                  :disabled="state !== 'enabled'"
                  @click="
                    () => {
                      const values = extraFieldsState[field.key];
                      if (
                        field.multiple &&
                        Array.isArray(values) &&
                        values.every((val) => Array.isArray(val))
                      ) {
                        values.push(['', '']);
                      }
                    }
                  "
                >
                  Add new
                </Button>
                <Button
                  :disabled="state !== 'enabled' || extraFieldsState[field.key].length === 0"
                  @click="
                    () => {
                      extraFieldsState[field.key] = [];
                    }
                  "
                >
                  Clear all
                </Button>
              </div>
            </template>
          </template>

          <template v-if="field.type === 'boolean' && !field.multiple">
            <RadioButton
              :name="field.key + popoverId"
              value="true"
              v-model:state="extraFieldsState[field.key] as unknown as string"
            >
              {{ field.keyValueLabels?.[0] || t('policies.state.enabled') }}
            </RadioButton>
            <RadioButton
              :name="field.key + popoverId"
              value="false"
              v-model:state="extraFieldsState[field.key] as unknown as string"
            >
              {{ field.keyValueLabels?.[1] || t('policies.state.disabled') }}
            </RadioButton>
          </template>

          <template v-if="field.type === 'image'">
            <div
              class="image-preview-container"
              :style="
                field.dimensions
                  ? {
                      width: `${getImageDisplaySize(field.dimensions).width}px`,
                      height: `${getImageDisplaySize(field.dimensions).height}px`,
                    }
                  : {}
              "
            >
              <img
                v-if="extraFieldsState[field.key] || field.defaultSrc"
                :src="(extraFieldsState[field.key] as string) || field.defaultSrc"
                alt="Icon preview"
                class="image-preview"
              />
              <span v-else class="image-placeholder">
                <TextBlock variant="caption" style="color: var(--wui-text-secondary)">
                  {{ $t('policies.dialog.image.none') }}
                </TextBlock>
              </span>
            </div>
            <div class="image-actions-row">
              <Button :disabled="state !== 'enabled'" @click="() => imageInputRefs[field.key]?.click()">
                {{ $t('policies.dialog.image.upload') }}
              </Button>
              <Button
                :disabled="state !== 'enabled' || !extraFieldsState[field.key]"
                @click="
                  () => {
                    extraFieldsState[field.key] = '';
                    if (imageInputRefs[field.key]) imageInputRefs[field.key]!.value = '';
                  }
                "
              >
                {{ $t('policies.dialog.image.clear') }}
              </Button>
              <input
                type="file"
                accept="image/*"
                style="display: none"
                :disabled="state !== 'enabled'"
                :ref="
                  (el) => {
                    imageInputRefs[field.key] = el as HTMLInputElement | null;
                  }
                "
                @change="handleImageChange(field.key, $event, field.dimensions)"
              />
            </div>
          </template>
        </div>
      </section>
    </div>

    <template #footer>
      <Button @click="handleSave" :loading="saving">{{ t('dialog.ok') }}</Button>
      <Button @click="closeDialog">{{ t('dialog.cancel') }}</Button>
    </template>

    <template #footer-left>
      <Button @click="openHelpPopup(`${docsUrl}/policies/${name}`)">{{ t('dialog.help') }}</Button>
    </template>
  </ContentDialog>
</template>

<style scoped>
  .grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    grid-template-rows: auto 1fr;
    grid-template-areas:
      'state help'
      'options help';
    gap: 1em;
  }

  @media (max-width: 600px) {
    .grid {
      grid-template-columns: 1fr;
      grid-template-rows: auto auto auto;
      grid-template-areas:
        'state'
        'options'
        'help';
    }
  }

  section {
    min-width: 0;
    min-height: 0;
  }

  input[type='radio'] {
    margin-right: 0.5em;
  }

  label {
    display: block;
  }

  label > span.text-block {
    margin-bottom: 4px;
  }

  label:not(:first-of-type) > span.text-block {
    margin-top: 8px;
  }

  fieldset {
    position: relative;
    padding: 9px 10px 13px;
    margin: 6px 0 0 0;
    background-color: var(--wui-card-background-default);
    border: none;
    box-shadow: inset 0 0 0 1px var(--wui-control-stroke-default);
    border-radius: var(--wui-control-corner-radius);
  }
  fieldset:first-of-type {
    margin-top: 0px;
  }

  fieldset .remove-button {
    position: absolute;
    top: 0;
    right: 0;
    opacity: 0;
    transition: var(--wui-control-faster-duration) ease opacity;
    z-index: 1;
  }

  .extra-field-block.type-json {
    padding-bottom: 8px;
  }

  .extra-field-block + .extra-field-block {
    margin-top: 8px;
  }

  fieldset:hover .remove-button,
  fieldset:focus-within .remove-button {
    opacity: 1;
  }

  .extra-fields-actions-row {
    display: flex;
    flex-direction: row;
    gap: 6px;
    margin-top: 6px;
  }

  .image-preview-container {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 128px;
    height: 128px;
    background-color: var(--wui-card-background-default);
    border: 1px solid var(--wui-control-stroke-default);
    border-radius: var(--wui-control-corner-radius);
    overflow: hidden;
    margin: 6px 0;
  }

  .image-preview {
    max-width: 100%;
    max-height: 100%;
    object-fit: contain;
  }

  .image-placeholder {
    display: flex;
    align-items: center;
    justify-content: center;
    text-align: center;
    width: 100%;
    height: 100%;
  }

  .image-actions-row {
    display: flex;
    flex-direction: row;
    gap: 6px;
    margin-top: 4px;
  }
</style>
