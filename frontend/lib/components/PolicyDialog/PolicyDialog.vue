<script setup lang="ts">
  import { Button, IconButton, RadioButton, TextBlock, TextBox } from '$components';
  import { raw } from '$utils';
  import { unproxify } from '$utils/unproxify';
  import { computed, ref, useTemplateRef, watchEffect } from 'vue';
  import ContentDialog from '../ContentDialog/ContentDialog.vue';

  interface ExtraFieldSpecCore {
    key: string;
    label?: string;
    type: 'key-value' | 'string';
    multiple?: boolean;
    keyValueLabels?: [string, string];
  }

  interface ExtraFieldSpecSingle extends ExtraFieldSpecCore {
    multiple?: false;
    interpret?: (value: string) => string;
  }

  interface ExtraFieldSpecMultiple extends ExtraFieldSpecCore {
    multiple: true;
    interpret: (value: string) => [string, string][];
  }

  type ExtraFieldSpec = ExtraFieldSpecSingle | ExtraFieldSpecMultiple;

  const { title, name, extraFields, stringValue, appliesTo, initialState } = defineProps<{
    title: string;
    name: string;
    extraFields?: ExtraFieldSpec[];
    stringValue?: string;
    appliesTo: string[];
    initialState?: 'disabled' | 'enabled' | 'unset';
  }>();

  const state = defineModel<'disabled' | 'enabled' | 'unset'>('state', {
    default: 'unset',
  });
  watchEffect(() => {
    if (initialState) {
      state.value = initialState;
    }
  });

  const extraFieldsState = ref<Record<string, string | [string, string][]>>({});
  watchEffect(() => {
    extraFields?.forEach((field) => {
      extraFieldsState.value[field.key] = field.interpret?.(stringValue || '') ?? (stringValue || '');
    });
  });

  const emit = defineEmits<{
    (e: 'save', state: boolean | null, extra?: Record<string, string | [string, string][]>): void;
  }>();

  function handleSave() {
    if (state.value === 'unset') {
      emit('save', null);
    } else if (state.value === 'enabled') {
      emit('save', true, unproxify(extraFieldsState.value));
    } else {
      emit('save', false, unproxify(extraFieldsState.value));
    }

    closeDialog.value?.();
  }

  const dialog = useTemplateRef<typeof ContentDialog>('dialog');
  const popoverId = computed(() => raw(dialog.value)?.popoverId as string | undefined);
  const openDialog = computed(() => raw(dialog.value)?.open as () => void);
  const closeDialog = computed(() => raw(dialog.value)?.close as () => void);

  function resetState() {
    if (initialState) {
      state.value = initialState;
      extraFieldsState.value = {};
      extraFields?.forEach((field) => {
        extraFieldsState.value[field.key] = field.interpret?.(stringValue || '') ?? (stringValue || '');
      });
    }
  }
</script>

<template>
  <slot :popoverId :openDialog></slot>
  <ContentDialog :title ref="dialog" size="max" style="max-inline-size: 800px" @afterClose="resetState">
    <div class="grid">
      <section style="grid-area: help">
        <div>
          <TextBlock variant="bodyStrong" style="font-size: 16px; margin: 8px 0 16px 0">
            {{ $t('policies.dialog.help') }}
          </TextBlock>
        </div>
        <TextBlock variant="body" style="white-space: break-spaces; overflow-wrap: anywhere">
          {{ $t(`policies.${name}.help`) }}
          <br />
          <br />Applies to: {{ appliesTo.join(', ') }}
        </TextBlock>
      </section>
      <section v-if="popoverId">
        <TextBlock variant="bodyStrong" style="font-size: 16px; margin: 8px 0">
          {{ $t('policies.dialog.state') }}
        </TextBlock>
        <RadioButton :name="'state' + popoverId" value="unset" v-model:state="state">
          {{ $t('policies.state.unset') }}
        </RadioButton>
        <RadioButton :name="'state' + popoverId" value="enabled" v-model:state="state">
          {{ $t('policies.state.enabled') }}
        </RadioButton>
        <RadioButton :name="'state' + popoverId" value="disabled" v-model:state="state">
          {{ $t('policies.state.disabled') }}
        </RadioButton>
      </section>
      <section v-if="extraFields && extraFields.length > 0">
        <TextBlock variant="bodyStrong" style="font-size: 16px; margin: 8px 0">
          {{ $t('policies.dialog.options') }}
        </TextBlock>
        <div v-for="field in extraFields" :key="field.key">
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
                      if (field.multiple && Array.isArray(values)) {
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
                      if (field.multiple && Array.isArray(values)) {
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
        </div>
      </section>
    </div>

    <template v-slot:footer>
      <Button @click="handleSave">{{ $t('dialog.ok') }}</Button>
      <Button @click="closeDialog">{{ $t('dialog.cancel') }}</Button>
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
</style>
