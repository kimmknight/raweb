<script setup lang="ts">
  import { Button, TextBlock } from '$components';
  import { favoritesEnabled, simpleModeEnabled } from '$utils';

  const {
    variant = 'app',
    title = 'Page not found',
    message = "We couldn't find what you are looking for.",
  } = defineProps<{
    variant?: 'app' | 'docs';
    title?: string;
    message?: string;
  }>();
</script>

<template>
  <article>
    <img src="./assets/thinking_face_animated.webp" alt="" />
    <TextBlock variant="subtitle">{{ title }}</TextBlock>
    <div class="prose">
      <TextBlock block>{{ message }}</TextBlock>
      <TextBlock block>Error code: 404</TextBlock>
    </div>
    <div class="buttons">
      <div class="button-row" v-if="variant === 'docs'">
        <RouterLink to="/docs" custom v-slot="{ href, isActive, navigate }">
          <Button variant="standard" :href="href" @click="navigate">Go to docs home</Button>
        </RouterLink>
      </div>
      <div class="button-row" v-else>
        <RouterLink
          to="/favorites"
          custom
          v-slot="{ href, isActive, navigate }"
          v-if="favoritesEnabled && !simpleModeEnabled"
        >
          <Button variant="standard" :href="href" @click="navigate">Go to favorites</Button>
        </RouterLink>
        <RouterLink to="/simple" custom v-slot="{ href, isActive, navigate }" v-else-if="simpleModeEnabled">
          <Button variant="standard" :href="href" @click="navigate">Go to apps and desktops</Button>
        </RouterLink>
        <RouterLink to="/apps" custom v-slot="{ href, isActive, navigate }" v-else>
          <Button variant="standard" :href="href" @click="navigate">Go to apps</Button>
        </RouterLink>
        <RouterLink to="/settings" custom v-slot="{ href, isActive, navigate }">
          <Button variant="standard" :href="href" @click="navigate">Go to settings</Button>
        </RouterLink>
      </div>
    </div>
  </article>
</template>

<style scoped>
  article {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    block-size: 100%;
    inline-size: 100%;
    gap: 16px;
    padding: 24px 16px;
    background-color: var(--wui-subtle-transparent);
    border-radius: var(--wui-control-corner-radius);
    box-sizing: border-box;
    height: 100%;
    text-align: center;
  }
  article .buttons {
    text-align: center;
  }
  article .prose > * {
    margin-bottom: 4px;
  }
  article .prose > *:last-child {
    margin-bottom: 2px;
  }
  article .button-row {
    display: flex;
    flex-direction: row;
    gap: 8px;
    margin-bottom: 8px;
  }
</style>
