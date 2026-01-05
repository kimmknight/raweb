<script setup lang="ts">
  import { ProgressRing, TextBlock } from '$components';
  import { isBrowser } from '$utils/environment.ts';
  import DOMPurify from 'dompurify';
  import { useTranslation } from 'i18next-vue';
  import { ref, watchEffect } from 'vue';
  import { useRouter } from 'vue-router';

  const { t } = useTranslation();
  const router = useRouter();

  /**
   * Escapes special characters in a string that would normally have special meaning in a regular expression.
   */
  function escapeRegExp(str: string) {
    return str.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  }

  /**
   * Highlights all occurrences of a substring within a string by wrapping them in <mark> tags.
   */
  function highlightAll(str: string, term: string) {
    if (!term) return str;
    const re = new RegExp(`(${escapeRegExp(term)})`, 'gi');
    return str.replace(re, '<mark>$1</mark>');
  }

  const searchResults = ref<PagefindSearchFragment[]>([]);
  const searching = ref(false);
  watchEffect(() => {
    router.currentRoute.value.params.query;
    if (
      !isBrowser ||
      !window.pagefind ||
      !router.currentRoute.value.params.query ||
      typeof router.currentRoute.value.params.query !== 'string'
    ) {
      return;
    }

    searching.value = true;
    window.pagefind.debouncedSearch(router.currentRoute.value.params.query).then(async (results) => {
      const topResults = await (
        await Promise.all((results?.results || []).slice(0, 10).map((res) => res.data()))
      ).map((res) => {
        // sanitize excerpt
        res.excerpt = DOMPurify.sanitize(
          res.excerpt
            .replaceAll('&gt;', '>')
            .replaceAll('&lt;', '<')
            .replaceAll('<mark>', '')
            .replaceAll('</mark>', ''),
          { ALLOWED_TAGS: ['mark'] }
        );

        // highlight search term in excerpt
        res.excerpt = highlightAll(res.excerpt, router.currentRoute.value.params.query as string);
        return res;
      });
      searchResults.value = topResults;
      searching.value = false;
    });
  });
</script>

<template>
  <div v-if="searching" class="please-wait">
    <ProgressRing />
    <TextBlock variant="bodyStrong">Please wait</TextBlock>
  </div>

  <TextBlock v-if="!searching" variant="title" tag="h1" class="page-title" block>
    {{ t('docs.search.title', { query: router.currentRoute.value.params.query }) }}
  </TextBlock>

  <a
    v-for="searchResult of searchResults"
    :href="searchResult.raw_url"
    @click.prevent="router.push(searchResult.raw_url || '/docs/')"
  >
    <article class="result">
      <TextBlock tag="h1" variant="subtitle" block>
        {{ searchResult.meta.title || searchResult.meta.nav_title }}
      </TextBlock>
      <TextBlock v-html="searchResult.excerpt"></TextBlock>
    </article>
  </a>

  <TextBlock v-if="!searching && searchResults.length === 0" variant="body">
    {{ t('docs.search.noResults') }}
  </TextBlock>
</template>

<style>
  .please-wait {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 16px;
    height: 100%;
  }

  a {
    text-decoration: none;
    color: var(--wui-text-primary) !important;
    -webkit-user-drag: none;
  }

  article {
    width: 100%;
    padding: 16px;
    margin-bottom: 8px;

    user-select: none;
    cursor: default;
    border-radius: var(--wui-control-corner-radius);
    transition: var(--wui-control-faster-duration) ease background;

    box-shadow: inset 0 0 0 1px var(--wui-control-stroke-default),
      inset 0 -1px 0 0 var(--wui-control-stroke-secondary-overlay);
    background-color: var(--wui-control-fill-default);
    color: var(--text-primary);
    background-clip: padding-box;
  }
  article:hover:not(.disabled) {
    background-color: var(--wui-control-fill-secondary);
  }
  article:active:not(.disabled) {
    background-color: var(--wui-control-fill-tertiary);
    color: var(--wui-text-secondary);
  }
</style>
