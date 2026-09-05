import { existsSync } from 'fs';
import { glob, readFile, writeFile } from 'fs/promises';
import path from 'path';
import { build, Plugin } from 'vite';
import { collectMockFiles, MockFile } from './collectMockFiles';

const demoMockApiDir = import.meta.dirname;

export interface DemoMockApiPluginOptions {
  /** The app's own base path */
  base: string;
}

/**
 * Emits `demo-mock-api/` as static files for the public build (see `vite.config.public.ts`).
 *
 * This is a build-only plugin. To test this locally, you must run
 * `npx vite build --config vite.config.public.ts` and then
 * `npx vite preview --config vite.config.public.ts --outDir dist`.
 */
export function demoMockApiPlugin({ base }: DemoMockApiPluginOptions): Plugin {
  /**
   * An absolute-path equivalent of `base` (e.g. `/` even when `base` is relative). Webfeed XML URLs
   * get resolved against the response's origin only (no path), so a relative URL there would always
   * land on site root - they need this instead of the relative `base`.
   */
  const absoluteBase = base === './' ? '/' : base;

  /**
   * Substitutes the base path placeholders used in the demo-mock-api template files
   * with the actual paths.
   */
  const applyBaseTemplating = (content: string) => {
    return content
      .replaceAll('__RAWEB_PUBLIC_BASE_ABS__', absoluteBase)
      .replaceAll('__RAWEB_PUBLIC_BASE__', base);
  };

  /**
   * Resolves a mock file's content.
   *
   * If the content is UTF-8 text, any included base path placeholders are replaced with the actual paths.
   */
  const readMockFileContent = async (file: MockFile): Promise<string | Buffer> => {
    if (file.content !== undefined) {
      return applyBaseTemplating(file.content);
    }

    if (!file.filePath) {
      throw new Error(`Mock file for ${file.urlPath} has neither content nor filePath set.`);
    }

    if (file.isText) {
      return applyBaseTemplating(await readFile(file.filePath, 'utf-8'));
    }
    return await readFile(file.filePath);
  };

  // populated by generateBundle, read by writeBundle
  let collectionRedirects: string[] = [];
  let iconFixScript = '';

  return {
    name: 'raweb:serve-demo-mock-api',

    async generateBundle() {
      const mockFiles = await collectMockFiles(import.meta.dirname);

      // A mock urlPath that's also the prefix of another (e.g. ".../registered" alongside
      // ".../registered/{identifier}") needs to exist as both a file and a directory on a real
      // filesystem, which a static build can't do. These are all fetch()-based JSON/download
      // endpoints, so the collection's content is emitted at a non-colliding `{urlPath}.json` sibling
      // instead of its real path, and `writeBundle` below injects a script that transparently
      // redirects fetches for the real path to that sibling.
      const urlPaths = mockFiles.map((file) => file.urlPath);
      const collectionsNeedingRedirect = urlPaths.filter((urlPath) =>
        urlPaths.some((other) => other !== urlPath && other.startsWith(`${urlPath}/`))
      );

      for (const file of mockFiles) {
        const fileName = collectionsNeedingRedirect.includes(file.urlPath)
          ? `${file.urlPath}.json`
          : file.urlPath;
        this.emitFile({
          type: 'asset',
          fileName,
          source: await readMockFileContent(file),
        });
      }

      // expose tools for generating .tsresource and .tsresourcebundle files on-demand
      this.emitFile({
        type: 'asset',
        fileName: 'demo-vendor/resource-export-tools.js',
        source: await buildResourceExportToolsBundle(),
      });

      iconFixScript = await buildIconFixScript(mockFiles, absoluteBase);
      collectionRedirects = collectionsNeedingRedirect;
    },

    /**
     * Injects scripts required for a functional read-only demo build
     * into every HTML file. This includes:
     * - a fetch() redirect for any collection endpoints that would otherwise collide with their
     *   child endpoints on a real filesystem
     * - a MutationObserver that rewrites any <img> targeting the resource-management icon endpoint
     *   to the actual file that would have been returned by the same logic
     * - an inline fetch() interceptor that powers resource exports
     */
    async writeBundle(options) {
      const distDir = options.dir;
      if (!distDir) {
        return;
      }

      const injected =
        buildFetchRedirectScript(collectionRedirects) +
        iconFixScript +
        buildExportInterceptScript(absoluteBase);

      const htmlFiles = glob('**/*.html', { cwd: distDir });
      for await (const file of htmlFiles) {
        const filePath = `${distDir}/${file}`;
        const html = await readFile(filePath, 'utf-8');
        if (html.includes('</head>')) {
          await writeFile(filePath, html.replace('</head>', `${injected}</head>`), 'utf-8');
        }
      }
    },
  } satisfies Plugin;
}

/**
 * Builds an inline script that redirects `fetch()` calls for each of `collectionUrlPaths` to that
 * same path with `.json` appended.
 */
function buildFetchRedirectScript(collectionUrlPaths: string[]): string {
  if (collectionUrlPaths.length === 0) return '';
  const suffixes = JSON.stringify(collectionUrlPaths.map((urlPath) => `/${urlPath}`));
  return `<script>(() => {
  const suffixes = ${suffixes};
  const originalFetch = window.fetch.bind(window);
  
  window.fetch = (input, init) => {
    const url = typeof input === "string" ? input : input.url;
    const resolved = new URL(url, document.baseURI);
    for (let i = 0; i < suffixes.length; i++) {
      if (resolved.pathname.endsWith(suffixes[i])) {
        resolved.pathname += ".json";
        return originalFetch(resolved.href, init);
      }
    }
    return originalFetch(input, init);
  };
})();</script>`;
}

/**
 * Attaches a MutationObserver to the document that rewrites any `<img>` targeting
 * the resource management icon endpoont to the actual file image file in the
 * staic build.
 *
 * `api/management/resources/icon` is an endpoint that returns the correct icon
 * for a managed resource or system icon entirely based on the query parameters.
 * Since the demo build is based on static files that need their own unique path,
 * we must re-write any `<img>` targeting that endpoint to the actual file on the
 * filesystem.
 */
async function buildIconFixScript(mockFiles: MockFile[], absoluteBase: string): Promise<string> {
  // get the plain and optional framed icon URL paths for each managed resource
  const managedResourceIcons: Record<string, { plain: string; framed?: string }> = {};
  for (const file of mockFiles) {
    const plainMatch = /^api\/resources\/image\/managed-resources\/(.+)$/.exec(file.urlPath);
    if (plainMatch) {
      managedResourceIcons[plainMatch[1]] ??= { plain: '' };
      managedResourceIcons[plainMatch[1]].plain = file.urlPath;
      continue;
    }
    const framedMatch = /^api\/resources\/image\/managed-resources-framed\/(.+)$/.exec(file.urlPath);
    if (framedMatch) {
      managedResourceIcons[framedMatch[1]] ??= { plain: '' };
      managedResourceIcons[framedMatch[1]].framed = file.urlPath;
    }
  }

  // resolves the path to each system icon stored in the system icons map file
  const systemIconMapPath = path.join(demoMockApiDir, 'system-icons/map.json');
  const rawSystemIconMap = existsSync(systemIconMapPath)
    ? (JSON.parse(await readFile(systemIconMapPath, 'utf-8')) as Record<string, string>)
    : {};
  const systemIconMap: Record<string, string> = {};
  for (const [exePath, value] of Object.entries(rawSystemIconMap)) {
    const webfeedMatch = /^webfeed:(.+)$/.exec(value); // some icons refer to an icon that is already used for a resource
    if (webfeedMatch && !managedResourceIcons[webfeedMatch[1]]) {
      console.warn(
        `System icon ${exePath} references webfeed resource ${webfeedMatch[1]}, but no such resource icon was found in the mock API. It will be ignored.`
      );
      continue;
    }

    const resolved = webfeedMatch ? managedResourceIcons[webfeedMatch[1]]?.plain : `demo-icons/${value}`;
    if (resolved) systemIconMap[exePath.toLowerCase()] = resolved;
  }

  return `<script>(() => {
const BASE = ${JSON.stringify(absoluteBase)};
const MANAGED_RESOURCE_ICONS = ${JSON.stringify(managedResourceIcons)};
const SYSTEM_ICON_MAP = ${JSON.stringify(systemIconMap)};
const ICON_ENDPOINT_SUFFIX = '/api/management/resources/icon';

function resolveIconTarget(url) {
  const requestedPath = url.searchParams.get('path') || '';
  const managedMatch = /^managed-resources\\/(.+)$/.exec(requestedPath);
  if (managedMatch && MANAGED_RESOURCE_ICONS[managedMatch[1]]) {
    const entry = MANAGED_RESOURCE_ICONS[managedMatch[1]];
    if (url.searchParams.get('frame') === 'pc' && entry.framed) return BASE + entry.framed;
    if (entry.plain) return BASE + entry.plain;
  }

  const systemIconTarget = SYSTEM_ICON_MAP[requestedPath.toLowerCase()];
  if (systemIconTarget) return BASE + systemIconTarget;

  const fallback = url.searchParams.get('fallback') || '';
  const fallbackMatch = /^resource:\\/\\/static\\/lib\\/assets\\/(.+)$/.exec(fallback);
  return BASE + 'lib/assets/' + (fallbackMatch ? fallbackMatch[1] : 'default.ico');
}

function fixImg(img) {
  const src = img.getAttribute('src');
  // do not process for empty src or already-correct src
  if (!src || img.dataset.demoIconFixed === src) {
    return;
  }

  let url;
  try {
    url = new URL(src, document.baseURI);
  } catch (e) {
    return;
  }

  if (!url.pathname.endsWith(ICON_ENDPOINT_SUFFIX)) {
    return;
  }

  const target = resolveIconTarget(url);
  img.dataset.demoIconFixed = target; // store the corrected target as an indicator
  if (target !== src) {
    img.setAttribute('src', target);
  }
}

function fixTree(root) {
  if (root.tagName === 'IMG') {
    fixImg(root);
  }

  if (root.querySelectorAll) {
    const imgs = root.querySelectorAll('img');
    for (let i = 0; i < imgs.length; i++) {
      fixImg(imgs[i]);
    }
  }
}

fixTree(document.documentElement);

const observer = new MutationObserver((mutations) => {
  for (let i = 0; i < mutations.length; i++) {
    const m = mutations[i];
    // fix the src for every <img>
    if (m.type === 'attributes' && m.target.tagName === 'IMG') {
      fixImg(m.target);
    }
    // look for newly added nodes that might contain <img> elements and fix them too  
    else if (m.type === 'childList') {
      for (let j = 0; j < m.addedNodes.length; j++) {
        const node = m.addedNodes[j];
        if (node.nodeType === 1) fixTree(node);
      }
    }
  }
});

observer.observe(
  document.documentElement,
  { subtree: true, childList: true, attributes: true, attributeFilter: ['src'] }
);

})();</script>`;
}

/**
 * Bundles `resourceExportTools.ts` into into a small module.
 */
async function buildResourceExportToolsBundle(): Promise<string> {
  const result = await build({
    configFile: false,
    logLevel: 'silent',
    build: {
      write: false,
      target: 'es2020',
      rolldownOptions: {
        input: path.join(demoMockApiDir, 'resourceExportTools.ts'),
        output: { format: 'es', entryFileNames: 'resource-export-tools.js' },
        preserveEntrySignatures: 'strict',
      },
    },
  });

  if (Array.isArray(result) || !('output' in result)) {
    throw new Error('Failed to bundle resourceExportTools.ts: unexpected build() result shape.');
  }

  const chunk = result.output.find((file) => file.type === 'chunk');
  if (!chunk) {
    throw new Error('Failed to bundle resourceExportTools.ts: no output chunk produced.');
  }

  return chunk.code;
}

/**
 * Modifies `window.fetch` to intercept requests for resource exports and return the appropriate
 * `.tsresource` or `.tsresourcebundle` zip generated on-demand from the registered resources.
 */
function buildExportInterceptScript(absoluteBase: string): string {
  return `<script>(() => {
const BASE = ${JSON.stringify(absoluteBase)};
const SINGLE_RE = /\\/api\\/management\\/resources\\/export-registered\\/([^/?]+)$/;
const BUNDLE_RE = /\\/api\\/management\\/resources\\/export-registered\\/?$/;
const originalFetch = window.fetch.bind(window);
let toolsPromise;
function loadTools() {
  if (!toolsPromise) {
    toolsPromise = import(BASE + 'demo-vendor/resource-export-tools.js');
  }
  return toolsPromise;
}

window.fetch = function (input, init) {
  const url = typeof input === 'string' ? input : input.url;
  const pathname = new URL(url, document.baseURI).pathname;

  const singleMatch = SINGLE_RE.exec(pathname);
  if (singleMatch) {
    const identifier = decodeURIComponent(singleMatch[1]);
    return loadTools().then(function ({ resourceExportTools }) { return resourceExportTools.buildSingleExportResponse(identifier); });
  }
  if (BUNDLE_RE.test(pathname)) {
    return loadTools().then(function ({ resourceExportTools }) { return resourceExportTools.buildBundleExportResponse(); });
  }

  return originalFetch(input, init);
};
})();</script>`;
}
