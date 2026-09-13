<script setup lang="ts">
  import { Button, ContentDialog, Field, NavigationPane, TextBlock } from '$components';
  import { TreeItem } from '$components/NavigationView/NavigationTypes';
  import { certificate, info } from '$icons';
  import { extractRdpSignatureCertificate, getRdpSignatureCertificateDetails, raw } from '$utils';
  import type { CertificateDetails } from '$utils/getRdpSignatureCertificateDetails';
  import { useTranslation } from 'i18next-vue';
  import { computed, ref, useTemplateRef, watch } from 'vue';

  const { t } = useTranslation();

  const { signatureValue, fileName = 'certificate' } = defineProps<{
    /** The raw `signature:s:` value (without the property prefix) of a signed RDP file. */
    signatureValue: string | undefined;
    /** The name of the certificate file when it is downloaded. Do not include the file extension. */
    fileName?: string;
  }>();

  const dialog = useTemplateRef<typeof ContentDialog>('dialog');
  const openDialog = computed(() => raw(dialog.value)?.open);
  const isOpen = computed(() => raw(dialog.value)?.isOpen ?? false);

  const loading = ref(false);
  const error = ref<Error | null>(null);
  const details = ref<CertificateDetails | null>(null);
  const currentSection = ref<'overview' | 'details'>('overview');

  async function load() {
    if (!signatureValue) {
      error.value = new Error(t('resource.certificateViewer.noSignature'));
      return;
    }

    loading.value = true;
    error.value = null;
    try {
      details.value = await getRdpSignatureCertificateDetails(signatureValue);
    } catch (err) {
      error.value = err instanceof Error ? err : new Error(String(err));
    } finally {
      loading.value = false;
    }
  }

  // always open to the overview section when the dialog is first opened
  // and then load the certificate details
  watch(isOpen, ($isOpen) => {
    if ($isOpen) {
      currentSection.value = 'overview';
      load();
    }
  });

  async function downloadCertificate() {
    if (!signatureValue) {
      return;
    }

    try {
      const certificateBytes = await extractRdpSignatureCertificate(signatureValue);
      const blob = new Blob([new Uint8Array(certificateBytes)], { type: 'application/x-x509-ca-cert' });
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `${fileName}.cer`;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      URL.revokeObjectURL(url);
    } catch (err) {
      error.value = err instanceof Error ? err : new Error(String(err));
    }
  }

  const menuItems = computed<TreeItem[]>(() => [
    {
      name: t('resource.certificateViewer.tabs.overview'),
      icon: certificate,
      onClick: () => (currentSection.value = 'overview'),
      selected: currentSection.value === 'overview',
    },
    {
      name: t('resource.certificateViewer.tabs.details'),
      icon: info,
      onClick: () => (currentSection.value = 'details'),
      selected: currentSection.value === 'details',
    },
  ]);

  defineExpose({ openDialog: () => openDialog.value?.() });
</script>

<template>
  <ContentDialog
    ref="dialog"
    size="maxer"
    max-height="680px"
    fill-height
    :loading="loading"
    :error="error || false"
    class="certificate-viewer-dialog"
  >
    <template #default>
      <div class="wrapper">
        <div class="nav-area">
          <NavigationPane :menu-items="menuItems" :header-text="t('resource.certificateViewer.titlebar')" />
        </div>
        <div class="content-area" v-if="details">
          <!-- Overview -->
          <div class="overview-section" v-if="currentSection === 'overview'">
            <div class="section-heading">
              <span class="section-heading-icon" v-html="certificate"></span>
              <TextBlock variant="subtitle">{{
                t('resource.certificateViewer.certificateInformation')
              }}</TextBlock>
            </div>

            <TextBlock variant="body" class="purposes-intro">
              {{ t('resource.certificateViewer.purposesIntro') }}
            </TextBlock>
            <ul class="purposes-list">
              <li v-for="purpose in details.purposes.flatMap((str) => str.split(', '))" :key="purpose">
                {{ purpose }}
              </li>
            </ul>

            <hr />

            <div class="issued-rows">
              <div class="issued-row">
                <TextBlock variant="bodyStrong">{{ t('resource.certificateViewer.issuedTo') }}</TextBlock>
                <TextBlock>{{ details.issuedTo }}</TextBlock>
              </div>
              <div class="issued-row">
                <TextBlock variant="bodyStrong">{{ t('resource.certificateViewer.issuedBy') }}</TextBlock>
                <TextBlock>{{ details.issuedBy }}</TextBlock>
              </div>
              <div class="issued-row">
                <TextBlock variant="bodyStrong">{{ t('resource.certificateViewer.validFrom') }}</TextBlock>
                <TextBlock
                  >{{ details.validFrom.toLocaleDateString() }}
                  {{ t('resource.certificateViewer.validFromTo') }}
                  {{ details.validTo.toLocaleDateString() }}</TextBlock
                >
              </div>
            </div>

            <div class="overview-section-buttons">
              <Button @click="downloadCertificate">
                {{ t('resource.certificateViewer.downloadCertificate') }}
                <template v-slot:icon>
                  <svg
                    width="24"
                    height="24"
                    fill="none"
                    viewBox="0 0 24 24"
                    xmlns="http://www.w3.org/2000/svg"
                  >
                    <path
                      d="M18.25 20.5a.75.75 0 1 1 0 1.5l-13 .004a.75.75 0 1 1 0-1.5l13-.004ZM11.648 2.012l.102-.007a.75.75 0 0 1 .743.648l.007.102-.001 13.685 3.722-3.72a.75.75 0 0 1 .976-.073l.085.073a.75.75 0 0 1 .072.976l-.073.084-4.997 4.997a.75.75 0 0 1-.976.073l-.085-.073-5.003-4.996a.75.75 0 0 1 .976-1.134l.084.072 3.719 3.714L11 2.755a.75.75 0 0 1 .648-.743l.102-.007-.102.007Z"
                      fill="currentColor"
                    />
                  </svg>
                </template>
              </Button>
            </div>
          </div>

          <!-- Details -->
          <div class="details-section" v-else-if="currentSection === 'details'">
            <TextBlock variant="subtitle" class="section-title">{{
              t('resource.certificateViewer.tabs.details')
            }}</TextBlock>

            <Field v-for="field in details.fields" :key="field.label" no-label-focus>
              <TextBlock variant="bodyStrong">{{ field.label }}</TextBlock>
              <p class="field-value">{{ field.value }}</p>
            </Field>
          </div>
        </div>
      </div>
    </template>

    <template #footer="{ close }">
      <Button @click="close">{{ t('dialog.ok') }}</Button>
    </template>
  </ContentDialog>
</template>

<style>
  .certificate-viewer-dialog > .content-dialog-inner > .content-dialog-body-background > .content-dialog-body {
    padding: 0 !important;
    overflow: hidden !important;
  }
</style>

<style scoped>
  .wrapper {
    display: flex;
    flex-direction: row;
    height: 100%;
    width: 100%;
    min-width: 26.25rem;
    position: relative;
  }

  .nav-area {
    background-color: var(--wui-solid-background-base);
    flex-shrink: 0;
  }
  .nav-area :deep(aside) {
    height: 100%;
  }
  .nav-area :deep(aside:not(.collapsed)) {
    width: 10rem;
  }

  .content-area {
    display: flex;
    flex-direction: column;
    padding: var(--inner-padding);
    width: 0;
    flex-grow: 1;
    overflow: auto;
  }

  .overview-section,
  .details-section {
    display: flex;
    flex-direction: column;
    padding-bottom: var(--inner-padding);
  }

  .section-heading {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    color: var(--wui-accent-default);
    margin-bottom: 1rem;
  }
  .section-heading .type-subtitle {
    color: var(--wui-text-primary);
  }
  .section-heading-icon {
    display: flex;
    flex-shrink: 0;
  }
  .section-heading-icon :deep(svg) {
    width: 2.5rem;
    height: 2.5rem;
  }
  .section-title {
    display: block;
    color: var(--wui-text-primary);
    margin-bottom: 1rem;
  }

  .purposes-intro {
    display: block;
    margin-bottom: 0.25rem;
  }
  .purposes-list {
    margin: 0 0 0 0;
    padding-left: 1.25rem;
  }
  .purposes-list li {
    font-size: 13px;
    color: var(--wui-text-primary);
  }

  hr {
    border: none;
    border-top: 1px solid var(--wui-divider-stroke-default);
    margin: 1rem 0;
  }

  .issued-rows {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
    margin-bottom: 1.25rem;
  }
  .issued-row {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
  }

  .overview-section-buttons {
    display: flex;
    justify-content: flex-end;
    gap: 0.5rem;
  }

  .field-value {
    color: var(--wui-text-primary);
    font-family: var(--wui-font-family-text);
    font-size: 13px;
    margin: 0;
    user-select: all;
    opacity: 0.84;
    word-break: break-word;
    white-space: pre-wrap;
  }
</style>
