import vue from '@vitejs/plugin-vue';
import { DOMParser, XMLSerializer } from '@xmldom/xmldom';
import { HttpAgent } from 'agentkeepalive';
import readFrontmatter from 'front-matter';
import { existsSync, readdirSync, readFileSync, realpathSync } from 'fs';
import { cp, mkdir, readdir, readFile, rm, writeFile } from 'fs/promises';
import i18next from 'i18next';
import Backend from 'i18next-fs-backend';
import { imageSize } from 'image-size';
import { Features } from 'lightningcss';
import markdownItAttrs from 'markdown-it-attrs';
import markdownItFootnotes from 'markdown-it-footnote';
import forge from 'node-forge';
import { randomUUID } from 'node:crypto';
import { hostname } from 'os';
import * as pagefind from 'pagefind';
import path from 'path';
import { MarkdownExit } from 'unplugin-vue-markdown/types';
import markdown from 'unplugin-vue-markdown/vite';
import {
  createLogger,
  createServer,
  defineConfig,
  type HttpProxy,
  loadEnv,
  type Plugin,
  type ResolvedConfig,
  type UserConfig,
} from 'vite';

const __dirname = import.meta.dirname;

const appSettingsPath = path.resolve(
  __dirname,
  '../dotnet/RAWeb.Server/.raweb/server/App_Data/appSettings.config'
);

let iisBase: string | null = null;
let envFQDN: string | null = null;

const keepAliveAgent = new HttpAgent({
  maxSockets: 100,
  keepAlive: true,
  maxFreeSockets: 10,
  keepAliveMsecs: 1000,
  timeout: 60000,
  freeSocketTimeout: 30000,
});
const configure = (proxy: HttpProxy.ProxyServer) => {
  proxy.on('proxyRes', (proxyRes, req, res) => {
    // WWW-Authenticate headers need to be split into multiple headers for the browser
    // to handle NTLM, Kerbos, and Negotiate correctly. Otherwise, the browser will
    // just show a 401 error and not prompt for credentials.
    var key = 'www-authenticate';
    proxyRes.headers[key] = proxyRes.headers[key]
      ?.toString()
      .split(',')
      .map((value) => value.trim());
  });
};

export default defineConfig(async ({ mode }) => {
  process.env = { ...process.env, ...loadEnv(mode, process.cwd(), 'RAWEB_') };

  if (!process.env.RAWEB_SERVER_ORIGIN && mode === 'development') {
    process.env.RAWEB_SERVER_ORIGIN = 'http://localhost:5135';
    console.warn(
      '\nWarning: RAWEB_SERVER_ORIGIN is not set. Defaulting to ' +
        process.env.RAWEB_SERVER_ORIGIN +
        '.\n' +
        'Please set RAWEB_SERVER_ORIGIN in your .env file to point to the RAWeb server.\n'
    );
  }

  if (iisBase === null && mode === 'development') {
    const logger = createLogger(undefined, { prefix: '[raweb]' });
    logger.info('Waiting for RAWeb server to start...', { timestamp: true });

    const { _iisBase, _envFQDN } = await fetchWithRetry(
      `${process.env.RAWEB_SERVER_ORIGIN}${process.env.RAWEB_SERVER_PATH ?? ''}/api/app-init-details`.replaceAll(
        '//',
        '/'
      ),
      {
        headers: {
          Accept: 'application/json',
        },
      }
    )
      .then((res) => res.json())
      .then((data) => {
        return {
          _iisBase: data.iisBase as string,
          _envFQDN: data.envFQDN as string | undefined,
        };
      })
      .catch((error) => {
        if (
          error instanceof SyntaxError &&
          error.message.includes(`Unexpected token '<', "<!DOCTYPE "... is not valid JSON`)
        ) {
          throw new Error(
            `\n\nFailed to fetch IIS base path from server: The server responded with HTML instead of JSON. \n\nThis ususally indicates that the server is not installed to specified path or there was a \ncomplilation error on the server. Please check that the RAWeb server is running and that \nRAWEB_SERVER_ORIGIN and RAWEB_SERVER_PATH are set correctly in the .env file.\n\n- RAWEB_SERVER_ORIGIN=${process.env.RAWEB_SERVER_ORIGIN}\n- RAWEB_SERVER_PATH=${process.env.RAWEB_SERVER_PATH}\n`
          );
        }
        throw new Error(`\n\nFailed to fetch IIS base path from server: ${error}\n`);
      });
    iisBase = _iisBase;
    envFQDN = _envFQDN || null;
  }

  const https = mode === 'development' ? await generateCertificate() : undefined;

  // we do not use the IIS base in production mode because the IIS app could be at any path
  const base = mode === 'development' && iisBase !== null ? iisBase : './';
  const resolvedBase = base.endsWith('/') ? base.slice(0, -1) : base;

  // since the docs can significantly increase the application size, we allow excluding them from builds via an env var
  const excludeDocs = process.env.RAWEB_EXCLUDE_DOCS === 'true' || process.env.RAWEB_EXCLUDE_DOCS === '1';

  const buildId = randomUUID();

  const defaultLogger = createLogger();
  const customLogger = {
    ...defaultLogger,
    warn(msg: string, options?: Parameters<typeof defaultLogger.warn>[1]) {
      // These error messages occur due to outdated interpretations of the HTML spec.
      // Because they should not be errors, we must supress them with the custom logger.
      const outdatedErrorMessages = [
        '<span> cannot be child of <option>, according to HTML specifications.',
        '<button> cannot be child of <select>, according to HTML specifications.',
      ];
      if (outdatedErrorMessages.some((errorMsg) => msg.includes(errorMsg))) {
        return;
      }

      defaultLogger.warn(msg, options);
    },
  };

  return {
    customLogger,
    define: {
      __APP_INIT_DETAILS_API_PATH__: JSON.stringify(`./api/app-init-details`),
      __DOCS_EXCLUDED__: JSON.stringify(excludeDocs),
      __BUILD_DATE__: JSON.stringify(new Date().toISOString()),
      __BUILD_ID__: JSON.stringify(buildId),
    },
    ssr: {
      noExternal: ['vue', 'vue-router', 'pinia'],
      external: [],
    },
    plugins: [
      (() => {
        const virtualModuleId = 'virtual:locales';
        const resolvedVirtualModuleId = '\0' + virtualModuleId;
        return {
          name: 'raweb:locales',
          resolveId(id) {
            if (id === virtualModuleId) return resolvedVirtualModuleId;
          },
          load(id) {
            if (id === resolvedVirtualModuleId) {
              const localesDir = path.resolve(__dirname, 'lib/public/locales');
              if (!existsSync(localesDir)) return 'export const availableLocales = [];';
              const locales = readdirSync(localesDir)
                .filter((f: string) => f.endsWith('.json'))
                .map((f: string) => f.replace('.json', ''));
              return 'export const availableLocales = ' + JSON.stringify(locales) + ';';
            }
          },
        } satisfies Plugin;
      })(),
      markdown({
        frontmatter: true,
        exportFrontmatter: true,
        markdownItSetup(md) {
          type PluginSimple = (md: MarkdownExit) => void;
          md.use(markdownItAttrs as unknown as PluginSimple); // allow setting attributes on markdown elements via {#id .class key=val}
          md.use(markdownItFootnotes as unknown as PluginSimple); // support footnotes

          // customize link rendering to use RouterLink for internal links
          // and to open external links in a new tab/window

          const defaultLinkOpen =
            md.renderer.rules.link_open ||
            function (tokens, idx, options, env, self) {
              return self.renderToken(tokens, idx, options);
            };

          const defaultLinkClose =
            md.renderer.rules.link_close ||
            function (tokens, idx, options, env, self) {
              return self.renderToken(tokens, idx, options);
            };

          md.renderer.rules.link_open = function (tokens, idx, options, env, self) {
            const token = tokens[idx];
            const hrefAttr = token.attrs?.find(([name]) => name === 'href');

            if (hrefAttr) {
              const href = hrefAttr[1];
              const isInternal = href.startsWith('/') && !href.startsWith('//');

              // use RouterLink (from vue-router) for internal links
              if (isInternal) {
                token.tag = 'RouterLink';

                // remove href and replace with :to
                token.attrs = token.attrs?.filter(([name]) => name !== 'href') || [];
                token.attrPush([':to', JSON.stringify(href)]);
              }

              // open external links in a new tab/window
              if (href.startsWith('http')) {
                token.attrPush(['target', '_blank']);
                token.attrPush(['rel', 'noopener noreferrer']);
              }

              // remove trailing dots from links, which may occur when sentences end with a plain-text
              // URL followed by a period that is then converted to a link.
              if (href.endsWith('.')) {
                const newHref = href.slice(0, -1);
                token.attrSet('href', newHref);
              }
            }
            return defaultLinkOpen(tokens, idx, options, env, self);
          };

          md.renderer.rules.link_close = function (tokens, idx, options, env, self) {
            // use the same tag name that was opened
            const openToken = tokens.findLast((t, i) => i < idx && t.type === 'link_open');
            const tag = openToken?.tag || 'a';
            tokens[idx].tag = tag;
            return defaultLinkClose(tokens, idx, options, env, self);
          };

          // override the footnote links, too
          md.renderer.rules.footnote_ref = function (tokens, idx, options, env, self) {
            const id = self.rules.footnote_anchor_name?.(tokens, idx, options, env, self);
            const caption = self.rules.footnote_caption?.(tokens, idx, options, env, self);
            let refid = id;

            if (tokens[idx].meta.subId > 0) refid += `:${tokens[idx].meta.subId}`;

            const relativeMarkdownFilePath = path.relative(__dirname, env.id).replaceAll('\\', '/');
            const routePath = relativeMarkdownFilePath.replace(/\([^)]*\)\//g, '').replace('/index.md', '/');

            return `<sup class="footnote-ref"><a href="${routePath}#fn${id}" id="fnref${refid}">${caption}</a></sup>`;
          };
          md.renderer.rules.footnote_anchor = (tokens, idx, options, env, self) => {
            let id = self.rules.footnote_anchor_name?.(tokens, idx, options, env, self);

            if (tokens[idx].meta.subId > 0) id += `:${tokens[idx].meta.subId}`;

            const relativeMarkdownFilePath = path.relative(__dirname, env.id).replaceAll('\\', '/');
            const routePath = relativeMarkdownFilePath.replace(/\([^)]*\)\//g, '').replace('/index.md', '/');

            /* ↩ with escape code to prevent display as Apple Emoji on iOS */
            return ` <a href="${routePath}#fnref${id}" class="footnote-backref">\u21a9\uFE0E</a>`;
          };

          // implemet a custom code block renderer that adds a copy button to code blocks
          md.renderer.rules.code_block = (tokens, idx, options, env, self) => {
            const attrs = self.renderAttrs(tokens[idx]);
            const content = tokens[idx].content;
            return `<CodeBlock${attrs} code="${md.utils.escapeHtml(content)}"></CodeBlock>\n`;
          };
          const originalFence = md.renderer.rules.fence;
          md.renderer.rules.fence = async (tokens, idx, options, env, self) => {
            let value = originalFence ? originalFence(tokens, idx, options, env, self) : '';
            const dom = new DOMParser().parseFromString(await value, 'text/html');
            const codeElem = dom.firstChild?.firstChild;

            if (!codeElem || !('attributes' in codeElem)) {
              return value;
            }

            // extract the attributes from the <code> element generated by the default fence renderer
            let attrs = '';
            if (codeElem?.attributes) {
              const elemAttrs = codeElem.attributes as NamedNodeMap;
              for (let i = 0; i < elemAttrs.length; i++) {
                const attr = elemAttrs.item(i);
                if (attr) {
                  attrs += ` ${attr.name}="${attr.value}"`;
                }
              }
            }

            const codeContent = codeElem?.textContent || '';

            return `<CodeBlock${attrs} code="${md.utils.escapeHtml(codeContent)}"></CodeBlock>\n`;
          };
        },
        transforms: {
          before(code, id) {
            const relativeMarkdownFilePath = path.relative(__dirname, id).replaceAll('\\', '/');
            const routePath = relativeMarkdownFilePath.replace(/\([^)]*\)\//g, '').replace('/index.md', '/');

            // prepend markdown links to ids (hash links) with the current route path
            // e.g. [Section 1](#section-1) becomes [Section 1](/docs/tutorial/#section-1)
            code = code.replace(/\[([^\]]+)]\(#([^)]+)\)/g, `[$1](${routePath}#$2)`);

            // also handle href="#some-id" in raw HTML
            code = code.replace(/href="#([^"]+)"/g, `href="${routePath}#$1"`);

            // calculate the correct height attribute for images with a width attribute set
            // based on the image aspect ratio
            const imageTagRegex = /<img\s+([^>]*?)\/?>/g;
            code = code.replace(imageTagRegex, (match, attrs) => {
              const node = new DOMParser().parseFromString(match, 'text/html').getElementsByTagName('img')[0];
              if (!node) {
                return match;
              }

              const width = node.attributes.getNamedItem('width')?.value;
              const src = node.attributes.getNamedItem('src')?.value;

              if (src && width) {
                const imgPath = path.resolve(path.dirname(id), src);

                // read the image aspect ratio
                const file = readFileSync(imgPath);
                const dimensions = imageSize(file);
                const aspectRatio =
                  dimensions.width && dimensions.height ? dimensions.width / dimensions.height : null;

                // calculate the height based on the width and aspect ratio
                if (aspectRatio) {
                  const calculatedHeight = Math.round(parseInt(width, 10) / aspectRatio);
                  node.setAttribute('height', calculatedHeight.toString());
                }
              }

              // convert the node to a string
              return new XMLSerializer().serializeToString(node);
            });

            return code;
          },
        },
      }),
      vue({
        include: [/\.vue$/, /\.md$/],
        script: {
          // typescript@7's package no longer exposes `ts.sys`, which @vue/compiler-sfc
          // uses by default to resolve types imported in SFC macros (e.g. defineProps<Import>()).
          // Provide a Node fs-backed implementation directly instead.
          fs: {
            fileExists: existsSync,
            readFile: (file) => {
              try {
                return readFileSync(file, 'utf-8');
              } catch {
                return undefined;
              }
            },
            realpath: realpathSync,
          },
        },
        template: {
          compilerOptions: {
            isCustomElement: (tag) => {
              const customElements = ['ms-store-badge'];

              // Vue recognizes most native elements, but not all of the newest onces
              const nativeElements = ['selectedcontent'];

              return customElements.includes(tag) || nativeElements.includes(tag);
            },
          },
        },
      }),
      (() => {
        let viteConfig: ResolvedConfig;
        const pluginName = 'raweb:generate-docs-search-index';

        const logger = createLogger(undefined, { prefix: '[pagefind]' });

        return {
          name: pluginName,

          configResolved(config) {
            viteConfig = config;
          },

          // when the dev server is running, build the search index for the documentation and serve the files
          configureServer(server) {
            let indexPromise: Promise<pagefind.PagefindIndex | null | undefined> | null = null;
            let index: pagefind.PagefindIndex | null | undefined = null;

            if (mode === 'production') {
              return;
            }

            server.httpServer?.once('listening', async () => {
              try {
                logger.info('Generating search index...', { timestamp: true });
                indexPromise = getDocsPagefindIndex(server);
                indexPromise.then((indexResult) => {
                  index = indexResult;
                  logger.info('Search index generated', { timestamp: true });
                });
              } catch (error) {
                if (error instanceof Error && error.message.includes('transport was disconnected')) {
                  return;
                }
                logger.error('Failed to generate search index:', {
                  error: error instanceof Error ? error : new Error(`${error}`),
                  timestamp: true,
                });
              }
            });

            server.watcher.on('change', async (file) => {
              if (file.endsWith('.md')) {
                try {
                  logger.info('Generating search index...', { timestamp: true });
                  indexPromise = getDocsPagefindIndex(server);
                  indexPromise.then((indexResult) => {
                    index = indexResult;
                    logger.info('Search index generated', { timestamp: true });
                  });
                  logger.info('Search index generated', { timestamp: true });
                } catch (error) {
                  if (error instanceof Error && error.message.includes('transport was disconnected')) {
                    return;
                  }
                  logger.error('Failed to generate search index:', {
                    error: error instanceof Error ? error : new Error(`${error}`),
                    timestamp: true,
                  });
                }
              }
            });

            server.middlewares.use(async (req, res, next) => {
              if (!req.url || !indexPromise) {
                return next();
              }

              const cleanUrl = req.url.split('?')[0].split('#')[0];

              // skip requests that are not for pagefind assets
              if (!req.url.startsWith(`${resolvedBase}/lib/assets/pagefind/`)) {
                return next();
              }

              // wait for the index promise to resolve
              await indexPromise;
              if (!index) {
                return next();
              }

              const indexFiles = await index.getFiles().then((res) => {
                return res.files.map((file) => {
                  const fileExtension = path.extname(file.path).toLowerCase();
                  const pathWithoutExtension = file.path.slice(0, -fileExtension.length);
                  const mimeType =
                    fileExtension === '.json'
                      ? 'application/json'
                      : fileExtension === '.js'
                        ? 'text/javascript'
                        : fileExtension === '.css'
                          ? 'text/css'
                          : 'application/octet-stream';

                  return {
                    path: 'lib/assets/pagefind/' + pathWithoutExtension + fileExtension,
                    mimeType,
                    content: file.content,
                  };
                });
              });
              const matchingFile = indexFiles.find(
                ({ path }) => `${resolvedBase}${path}` === cleanUrl.slice(1)
              );
              if (!matchingFile) {
                res.statusCode = 404;
                return res.end('Not found');
              }

              res.setHeader('Content-Type', matchingFile.mimeType);
              res.end(matchingFile.content);
            });
          },

          async generateBundle(_, bundle) {
            if (excludeDocs) {
              return;
            }

            const configFile =
              process.env.VITE_CONFIG_FILE ||
              (process.argv.includes('--config')
                ? process.argv[process.argv.indexOf('--config') + 1]
                : undefined);

            // create a server that we can use for SSR
            const server = await createServer({
              mode: 'production',
              server: { watch: null, middlewareMode: true },
              configFile,
            });

            let index: pagefind.PagefindIndex | null | undefined = null;
            try {
              // generate the search index using SSR
              logger.info('Generating search index...', { timestamp: true });
              index = await getDocsPagefindIndex(server);
              if (!index) {
                throw new Error('Failed to generate Pagefind index');
              }
            } finally {
              await server.close();
            }

            // add the search index assets to the build output
            const indexFiles = await index.getFiles();
            for (const file of indexFiles.files) {
              this.emitFile({
                type: 'asset',
                fileName: `lib/assets/pagefind/${file.path}`,
                source: file.content,
              });
            }
            logger.info('Search index generated', { timestamp: true });
          },
        } satisfies Plugin;
      })(),
      (() => {
        return {
          name: 'raweb:icon-policy-override',

          configureServer(server) {
            // check for an icon override policy (App.Icon.<filename>)
            // before serving an icon asset
            server.middlewares.use((req, res, next) => {
              if (!req.url) return next();
              const [cleanUrl, queryString] = req.url.split('?');
              if (!cleanUrl.includes('/lib/assets/')) return next();
              if (new URLSearchParams(queryString).get('ignoreOverride') === 'true') return next();

              const fileName = path.basename(cleanUrl);
              const policyValue = readIconPolicy(fileName);
              if (!policyValue || !policyValue.startsWith('data:')) return next();

              const semicolonIdx = policyValue.indexOf(';');
              const commaIdx = policyValue.indexOf(',');
              if (semicolonIdx <= 5 || commaIdx <= semicolonIdx) return next();

              const mimeType = policyValue.slice(5, semicolonIdx);
              const base64Data = policyValue.slice(commaIdx + 1);
              try {
                const bytes = Buffer.from(base64Data, 'base64');
                res.setHeader('Content-Type', mimeType);
                res.end(bytes);
              } catch {
                next();
              }
            });
          },
        } satisfies Plugin;
      })(),
      (() => {
        let viteConfig: ResolvedConfig;

        return {
          name: 'raweb:generate-entry-html',
          enforce: 'post',

          configResolved(config) {
            viteConfig = config;
          },

          configureServer(server) {
            // re-write requests for static files in lib/public to the correct location
            const libPublicDir = path.resolve(__dirname, 'lib/public');
            server.middlewares.use((req, _res, next) => {
              if (!req.url) return next();

              const relativeRequestUrl = req.url.startsWith(resolvedBase)
                ? req.url.replace(resolvedBase, '')
                : req.url;

              const candidate = path.join(libPublicDir, relativeRequestUrl);
              if (existsSync(candidate)) {
                req.url = resolvedBase + '/lib/public' + relativeRequestUrl;
              }
              next();
            });

            // capture entry definitions from vite config (e.g. { index: 'path/to/index.ts', login: 'path/to/login.ts' })
            const entryPoints = Object.entries(viteConfig.build?.rollupOptions?.input ?? {}).flatMap(
              ([label, entry]) => {
                const possibleEntryNames = [`${resolvedBase}/${label}.html`, `${resolvedBase}/${label}`];
                if (label === 'index') {
                  possibleEntryNames.push(`${resolvedBase}/`);
                  possibleEntryNames.push('*');
                }

                return possibleEntryNames.map((name) => [name, entry] as const);
              }
            );

            // handle HTML requests for each entry point
            server.middlewares.use(async (req, res, next) => {
              if (!req.url) {
                return next();
              }

              const cleanUrl = req.url.split('?')[0].split('#')[0];

              // find a matching entry point for the requested URL
              let matchingEntry = entryPoints.find(([name]) => name === cleanUrl);

              // If the entry point is not found, but the request is for an HTML page (not API or webfeed),
              // serve the default entry point (index).
              // NOTE: Routes like /client/:resourceId/:hostId may contain dots in the hostname
              // so we cannot rely of checking for a file extension. Instead, we check for known static asset
              // extensions and skip those.
              const hasStaticAssetExtension =
                /\.(js|mjs|css|map|json|png|jpe?g|gif|svg|webp|ico|woff2?|ttf|eot|wasm|webmanifest)$/i.test(
                  cleanUrl
                );
              if (
                !matchingEntry &&
                !hasStaticAssetExtension &&
                req.headers.accept?.includes('text/html') &&
                !cleanUrl.startsWith(`${resolvedBase}/api`) &&
                !cleanUrl.startsWith(`${resolvedBase}/webfeed.aspx`) &&
                !cleanUrl.startsWith(`${resolvedBase}/RDWebService.asmx`)
              ) {
                if (cleanUrl.startsWith(`${resolvedBase}/docs`)) {
                  matchingEntry = entryPoints.find(([name]) => name === `${resolvedBase}/docs`);
                } else {
                  matchingEntry = entryPoints.find(([name]) => name === '*');
                }
              }

              // otherwise, if no matching entry point is found, continue to the next middleware
              if (!matchingEntry) {
                return next();
              }

              // get the path to the entry javascript file relative to the base url of the web app
              const entryRelativePath = path.relative(viteConfig.root, matchingEntry[1]).replaceAll('\\', '/');

              // search for overrides files
              const overridesDir = path.resolve(
                __dirname,
                '../dotnet/RAWeb.Server/.raweb/server/App_Data/inject'
              );
              const overridesCssPath = path.join(overridesDir, 'index.css');
              const overridesJsPath = path.join(overridesDir, 'index.js');
              let overrides = '';
              if (existsSync(overridesCssPath)) {
                overrides += `<link rel="stylesheet" href="${resolvedBase}/api/inject/file/index.css">\n`;
              }
              if (existsSync(overridesJsPath)) {
                overrides += `<script type="module" src="${resolvedBase}/api/inject/file/index.js"></script>\n`;
              }

              // read the HTML template file
              const template = await readFile('app.html', 'utf-8');

              // inject the entry script tag and base tag into the HTML template
              const html = template
                .replace('%raweb.basetag%', `<base href="${resolvedBase}/">`)
                .replace('%raweb.overrides%', overrides)
                .replace('%raweb.head%', '')
                .replace(
                  '%raweb.scripts%',
                  `<script type="module" src="${resolvedBase}/${entryRelativePath}"></script>`
                )
                .replace('%raweb.servername%', 'Development')
                .replaceAll('%raweb.base%', resolvedBase)
                .replace(
                  '%raweb.splashlogoimg%',
                  readIconPolicy('icon-192x192.webp')
                    ? `<img src="${resolvedBase}/lib/assets/icon-192x192.webp" class="root-splash-app-logo" alt="" />`
                    : ''
                );

              // serve the generated HTML
              res.setHeader('Content-Type', 'text/html');
              res.end(html);
              return;
            });
          },

          async generateBundle(_, bundle) {
            // read the HTML template file
            const template = await readFile('app.html', 'utf-8');

            // Collect entry chunks + css
            const entryPoints: Record<string, { scripts: string[]; css: string[] }> = {};
            for (const [fileName, output] of Object.entries(bundle)) {
              if (fileName.endsWith('.map')) {
                continue; // skip sourcemap files
              }

              if (output.type !== 'chunk' || !output.isEntry) {
                continue; // only process entry chunks
              }

              // add the entry point
              entryPoints[output.name] = { scripts: [fileName], css: [] };

              // collect any CSS files imported by this entry
              if (output.viteMetadata) {
                const cssFiles = Array.from(output.viteMetadata.importedCss || []);
                entryPoints[output.name].css.push(...cssFiles);
              }
            }

            // include the shared (common code) scripts and css to all entries
            const sharedScripts = Object.keys(bundle).filter((f) => f.includes('shared-') && f.endsWith('.js'));
            const sharedCss = Object.keys(bundle).filter((f) => f.includes('shared-') && f.endsWith('.css'));
            for (const entry of Object.values(entryPoints)) {
              entry.scripts.push(...sharedScripts);
              entry.css.push(...sharedCss);
            }

            // create "virtual" entry points for each route within each entry point
            // e.g. "docs" entry point will also have "docs/some-page" and "docs/another-page" entries
            for (const [entryName, assets] of Object.entries(entryPoints)) {
              const mainEntryScript = assets.scripts.find((s) => s.includes(`${entryName}-`));
              if (!mainEntryScript) {
                continue;
              }

              if (entryName === 'index') {
                entryPoints['apps'] = assets;
                entryPoints['devices'] = assets;
                entryPoints['favorites'] = assets;
                entryPoints['policies'] = assets;
                entryPoints['settings'] = assets;
                entryPoints['simple'] = assets;
              }

              if (entryName === 'docs') {
                // build a list of all markdown files in the docs directory
                // and create an entry point for each
                const docsDir = path.resolve(__dirname, 'docs');
                (await findMarkdownFiles(docsDir))
                  // only include index.md files
                  .filter((filePath) => filePath.endsWith('index.md'))
                  // convert the file path to a route path
                  .map(converMarkdownFilePathToRoutePath)
                  // omit empty paths
                  .filter((x): x is string => !!x)
                  // add entry for each markdown file
                  .map((relativePath) => {
                    const routePath = relativePath.startsWith('/') ? relativePath.slice(1) : relativePath;
                    entryPoints[`docs/${routePath}`] = assets;
                  });

                // also add entries for redirects defined in the markdown frontmatter
                for (const { attributes } of await readMarkdownFiles<{
                  title?: string;
                  nav_title?: string;
                  redirects?: string[];
                }>(docsDir)) {
                  const redirects = attributes.redirects;
                  if (redirects && Array.isArray(redirects)) {
                    for (const redirect of redirects) {
                      const cleanRedirect = redirect.replace(/^\//, ''); // remove leading slash
                      entryPoints[`docs/${cleanRedirect}`] = assets;
                    }
                  }
                }

                // add an entry for the /docs/search route
                entryPoints['docs/search'] = assets;
              }
            }

            // determine the generated html file name
            const shouldUsePrettyPaths = process.env.RAWEB_USE_PRETTY_HTML_PATHS === '1';

            // generate HTML for each entry point
            for (const [entryName, assets] of Object.entries(entryPoints)) {
              const htmlFileName = shouldUsePrettyPaths
                ? entryName.endsWith('index')
                  ? entryName
                  : `${entryName}/index`
                : entryName;
              const directoriesCount = htmlFileName.split('/').length - 1;
              const assetsPrefix = directoriesCount > 0 ? '../'.repeat(directoriesCount).slice(0, -1) : '.';

              const cssTags = assets.css
                .map((c) => `<link rel="stylesheet" href="%raweb.base%/${c}">`)
                .join('\n');
              const scriptTags = assets.scripts
                .filter((s) => !s.endsWith('.map')) // exclude sourcemap files
                .map((s) => `<script type="module" src="%raweb.base%/${s}"></script>`)
                .join('\n');

              const html = template
                .replace('%raweb.head%', cssTags)
                .replace('%raweb.scripts%', scriptTags)
                .replace('%raweb.title%', entryName)
                .replaceAll(
                  '%raweb.base%',
                  // relative path mode - we need to prepend asset paths with ../ as needed
                  viteConfig.base === './'
                    ? assetsPrefix
                    : // absolute path mode - use the base directly (e.g. /raweb/deploy-preview/pr-1234)
                      viteConfig.base
                );

              // replace double (or more) slashes in paths with single slashes (except for the // in https://)
              const dom = new DOMParser().parseFromString(html, 'text/html');
              for (const elem of [
                ...Array.from(dom.getElementsByTagName('link')),
                ...Array.from(dom.getElementsByTagName('script')),
                ...Array.from(dom.getElementsByTagName('img')),
              ]) {
                const urlAttr = elem.tagName === 'link' ? 'href' : 'src';
                const urlValue = elem.getAttribute(urlAttr);
                if (urlValue) {
                  try {
                    const startsWithProtocol = urlValue.match(/^[a-zA-Z]+:\/\//);
                    if (startsWithProtocol) {
                      continue; // skip URLs that start with a protocol (e.g. https://)
                    }

                    const newUrlValue = urlValue.replace(/\/\/+/g, '/');
                    elem.setAttribute(urlAttr, newUrlValue);
                  } catch {}
                }
              }
              const serialized = new XMLSerializer().serializeToString(dom);

              this.emitFile({
                type: 'asset',
                fileName: `${htmlFileName}.html`,
                source: serialized,
              });
            }
          },
        } satisfies Plugin;
      })(),
      (() => {
        let viteConfig: ResolvedConfig;

        return {
          name: 'raweb:clean-assets-dir',
          apply: 'build',
          enforce: 'pre',
          configResolved(config) {
            viteConfig = config;
          },
          async buildStart() {
            const distDir = viteConfig.build.outDir;
            if (!distDir) {
              throw new Error('distDir is not defined');
            }
            if (!existsSync(distDir)) {
              return;
            }

            const libAssetsDir = path.resolve(distDir, 'lib/assets');

            // also remove any existing html files in the dist directory
            const distFiles = await readdir(distDir, { withFileTypes: true, recursive: true });
            for (const file of distFiles) {
              if (file.isFile() && file.name.endsWith('.html')) {
                await rm(path.join(file.parentPath, file.name), { force: true });
              }
            }

            // remove empty folders in the dist directory
            for (const file of await readdir(distDir, { withFileTypes: true, recursive: true })) {
              if (file.isDirectory() && file.name !== 'lib') {
                const dirPath = path.join(file.parentPath, file.name);
                const isEmpty = (await readdir(dirPath)).length === 0;
                if (isEmpty) {
                  await rm(dirPath, { recursive: true, force: true });
                }
              }
            }

            await rm(libAssetsDir, { recursive: true, force: true });
          },
        } satisfies Plugin;
      })(),
      {
        name: 'raweb:copy-assets',
        writeBundle: async (options) => {
          const distDir = options.dir;
          if (!distDir) {
            throw new Error('distDir is not defined');
          }
          const libDir = path.resolve(__dirname, 'lib');

          // move lib/assets to dist/lib/assets
          const libAssetsDir = path.resolve(libDir, 'assets');
          const distAssetsDir = path.resolve(distDir, 'lib/assets');
          await cp(libAssetsDir, distAssetsDir, { recursive: true, force: true });

          // move lib/winui.css to dist/lib/winui.css
          const libWinuiCssPath = path.resolve(libDir, 'winui.css');
          const distWinuiCssPath = path.resolve(distDir, 'lib/winui.css');
          await cp(libWinuiCssPath, distWinuiCssPath, { force: true });

          // move lib/public to dist/
          const libPublicDir = path.resolve(libDir, 'public');
          const distPublicDir = path.resolve(distDir, '');
          await cp(libPublicDir, distPublicDir, { recursive: true, force: true });

          // create a timestamp file
          const timestampFilePath = path.resolve(distDir, 'lib/build.timestamp');
          const timestamp = new Date().toISOString();
          await writeFile(timestampFilePath, timestamp, { encoding: 'utf-8' });

          // preprend the service-worker.js file with the timestamp as a comment
          // so that a new version is created every time the app is built
          const serviceWorkerPath = path.resolve(distDir, 'service-worker.js');
          const serviceWorkerContent = await readFile(serviceWorkerPath, { encoding: 'utf-8' });
          const newServiceWorkerContent = `// Built timestamp: ${timestamp}\n${serviceWorkerContent}`;
          await writeFile(serviceWorkerPath, newServiceWorkerContent, { encoding: 'utf-8' });
        },
        closeBundle: () => {
          if (mode !== 'production') {
            return;
          }

          const isWatchMode = process.argv.includes('--watch');
          if (isWatchMode) {
            console.log('\nApp ready. Watching for changes...\n');
            console.log(`Local: https://localhost${iisBase || '/raweb/'}`);
            console.log(`Network: https://localhost${iisBase || '/raweb/'}`);
          } else {
            console.log('\nFrontend app installed.\n');
          }
        },
      } satisfies Plugin,
    ],
    resolve: {
      alias: [
        { find: '$components', replacement: path.resolve(__dirname, './lib/components') },
        { find: '$icons', replacement: path.resolve(__dirname, './lib/assets/icons.ts') },
        { find: '$utils', replacement: path.resolve(__dirname, './lib/utils') },
        { find: '$stores', replacement: path.resolve(__dirname, './lib/stores') },
        { find: '$dialogs', replacement: path.resolve(__dirname, './lib/dialogs') },

        // ensure vue is always the ESM build
        {
          find: /^vue$/,
          replacement: path.resolve(__dirname, 'node_modules/vue/dist/vue.runtime.esm-bundler.js'),
        },
      ],
    },
    base,
    build: {
      outDir: path.resolve(__dirname, '../dotnet/RAWeb.Server/.raweb/client'),
      emptyOutDir: false,
      sourcemap: mode === 'development',
      target: 'es2023',
      rollupOptions: {
        input: {
          index: path.resolve(__dirname, './lib/entry.dist.mjs'),
          login: path.resolve(__dirname, './lib/login-entry.dist.mjs'),
          logoff: path.resolve(__dirname, './lib/logoff-entry.dist.mjs'),
          password: path.resolve(__dirname, './lib/password-entry.dist.mjs'),
          ...(excludeDocs ? {} : { docs: path.resolve(__dirname, './lib/docs-entry.dist.mjs') }),
        },
        output: {
          entryFileNames: 'lib/assets/[name]-[hash].js',
          chunkFileNames: 'lib/assets/[name]-[hash].js',
          assetFileNames: (info: any) => {
            const skipHashFIles = ['thinking_face_animated.webp'];
            if (skipHashFIles.includes(info.name)) {
              return 'lib/assets/[name].[ext]';
            }
            return 'lib/assets/[name]-[hash].[ext]';
          },
          manualChunks: (id: any) => {
            const shared = [
              'node_modules',
              '/lib/assets/',
              '/lib/components/',
              '/lib/pages/',
              '/lib/public/',
              '/lib/utils/',
              '/lib/i18n.ts',
              '/lib/winui.css',
            ];
            if (shared.some((dir) => id.includes(dir))) {
              return 'shared';
            }
          },
        },
      },
    },
    server: {
      // note: you may need to open the firewall: netsh advfirewall firewall add rule name="RAWeb Dev Server" dir=in action=allow protocol=TCP localport=5174
      host: true,
      https: https,
      allowedHosts: [
        'localhost',
        '794693d8-4d0e-4a0b-b0d7-5a5f0a957091-rawebdev.local',
        hostname(),
        hostname().toLowerCase(),
        envFQDN,
        envFQDN?.toLowerCase(),
      ].filter((x): x is string => !!x),

      // proxy API, authentication, and injection requests to the backend server
      proxy: {
        [`${resolvedBase}/api`]: {
          target: process.env.RAWEB_SERVER_ORIGIN,
          changeOrigin: true, // set Host header to match target
          agent: keepAliveAgent, // required for NTLM and Kerberos authentication to work properly
          configure,
        },
        [`${resolvedBase}/RDWebService.asmx`]: {
          target: process.env.RAWEB_SERVER_ORIGIN,
          changeOrigin: true,
          agent: keepAliveAgent,
          configure,
        },
        [`${resolvedBase}/webfeed.aspx`]: {
          target: process.env.RAWEB_SERVER_ORIGIN,
          changeOrigin: true,
          agent: keepAliveAgent,
          configure,
        },
        [`${resolvedBase}/auth/login.aspx`]: {
          target: process.env.RAWEB_SERVER_ORIGIN,
          changeOrigin: true,
          configure,
          agent: keepAliveAgent,
        },
        [`${resolvedBase}/inject`]: {
          target: process.env.RAWEB_SERVER_ORIGIN,
          changeOrigin: true,
          agent: keepAliveAgent,
          configure,
        },
        '/guacd-tunnel': {
          target: process.env.RAWEB_SERVER_ORIGIN,
          ws: true,
          changeOrigin: true,
        },
      },
    },
    css: {
      transformer: 'lightningcss',
      lightningcss: {
        exclude: Features.LightDark,
      },
    },
  } satisfies UserConfig;
});

function readIconPolicy(fileName: string): string | null {
  if (!existsSync(appSettingsPath)) {
    return null;
  }

  try {
    const xml = readFileSync(appSettingsPath, 'utf-8');
    const doc = new DOMParser().parseFromString(xml, 'text/xml');
    const adds = doc.getElementsByTagName('add');
    const policyKey = `App.Icon.${fileName}`;
    for (let i = 0; i < adds.length; i++) {
      const node = adds.item(i);
      if (node?.getAttribute('key') === policyKey) {
        return node.getAttribute('value') ?? null;
      }
    }
  } catch {}

  return null;
}

const MAX_WAIT_MS = 60_000;
const RETRY_INTERVAL_MS = 2000;
async function fetchWithRetry(url: string, init: RequestInit = {}): Promise<Response> {
  const start = Date.now();

  while (Date.now() - start < MAX_WAIT_MS) {
    try {
      const res = await fetch(url, init);
      if (res.ok) return res;
    } catch (err: any) {
      if (err.name === 'AbortError') throw err; // external cancel / timeout
      // ignore connection errors → retry
    }
    await new Promise((r) => setTimeout(r, RETRY_INTERVAL_MS));
  }

  throw new Error(`Timed out waiting for server after ${MAX_WAIT_MS / 1000}s`);
}

async function findMarkdownFiles(directory: string): Promise<string[]> {
  const markdownFiles: string[] = [];

  async function walk(currentPath: string) {
    const entries = await readdir(currentPath, { withFileTypes: true });

    for (const entry of entries) {
      const fullPath = path.join(currentPath, entry.name);
      if (entry.isFile() && entry.name.endsWith('.md')) {
        markdownFiles.push(fullPath);
      } else if (entry.isDirectory()) {
        await walk(fullPath);
      }
    }
  }

  await walk(directory);
  return markdownFiles;
}

async function readMarkdownFiles<T>(directory: string) {
  const markdownFiles = await findMarkdownFiles(directory);

  const results: { filePath: string; attributes: T; body: string }[] = [];

  for await (const filePath of markdownFiles) {
    const fileContent = await readFile(filePath, { encoding: 'utf-8' });
    const { attributes, body } = readFrontmatter<T>(fileContent);
    results.push({ filePath, attributes, body });
  }

  return results;
}

function converMarkdownFilePathToRoutePath(filePath: string) {
  // convert to relative path
  let routePath = path.relative(path.resolve(__dirname, 'docs'), filePath).replaceAll('\\', '/');

  // omit sections (folders wthat start and end with parentheses) from the path
  routePath = routePath.replace(/\([^)]*\)\//g, '');

  // convert trailing index.md to "" (no slash)
  const baseName = path.basename(routePath);
  if (baseName === 'index.md') {
    routePath = routePath.slice(0, -9);
  }

  return routePath;
}

let docsIndexRunning = false;
let docsIndexPromise: Promise<pagefind.PagefindIndex | undefined> | null = null;
async function getDocsPagefindIndex(server: import('vite').ViteDevServer) {
  if (docsIndexRunning) {
    return docsIndexPromise;
  }
  docsIndexRunning = true;
  docsIndexPromise = internal_getDocsPagefindIndex(server);
  const result = await docsIndexPromise.finally(() => {
    docsIndexRunning = false;
  });
  return result;
}

async function internal_getDocsPagefindIndex(server: import('vite').ViteDevServer) {
  // load required modules
  const { createDocsApp } = (await server.ssrLoadModule('./lib/docs-entry.dist.mjs', {
    fixStacktrace: true,
  })) as { createDocsApp: typeof import('./lib/docs-entry.dist.mjs').createDocsApp };
  const { renderToString } = (await server.ssrLoadModule('vue/server-renderer', {
    fixStacktrace: true,
  })) as { renderToString: typeof import('vue/server-renderer').renderToString };

  // get a list of all markdown files in the docs directory
  const docsDir = path.resolve(__dirname, 'docs');
  type PageAttributes = { title?: string; nav_title?: string };
  const markdownFiles = await readMarkdownFiles<PageAttributes>(docsDir);

  // render each markdown file to HTML
  const htmlRecords: { url: string; content: string; attributes: PageAttributes }[] = [];
  for (const { filePath, attributes } of markdownFiles) {
    const routePath = `/docs/${converMarkdownFilePathToRoutePath(filePath)}`;

    // create a ssr app for the route
    const { app, router } = await createDocsApp({
      ssr: true,
      initialPath: routePath,
    });
    await router.isReady();

    // the $t{{ some.key }} syntax may appear multiple times in a string,
    // so extract and replace all matches
    const t = await i18next
      .use(new Backend(null, { loadPath: path.resolve(__dirname, 'lib/public/locales/{{lng}}.json') }))
      .init({ lng: 'en' });
    const regex = /\$t\{\{\s*([^\}]+)\s*\}\}/g;
    attributes.title = attributes.title?.replaceAll(regex, (_, translationKey) => {
      const translation = t(translationKey.trim(), { lng: 'en-US' });
      return translation;
    });

    // render the app to HTML
    let html = (await renderToString(app))
      .replaceAll('<!--[-->', '')
      .replaceAll('<!--]-->', '')
      .replaceAll('<!---->', '');
    const document = new DOMParser({
      onError: (level, message) => {
        if (level === 'warning' || level === 'error') {
          return; // ignore warnings and errors
        }
        throw new Error(`DOMParser fatal error: ${message}`);
      },
    }).parseFromString(`<html>${html}</html>`, 'text/html');

    // if there is a #page element, use its content only
    const pageElement = document.getElementById('page');
    if (pageElement) {
      html = new XMLSerializer().serializeToString(pageElement);
    }

    htmlRecords.push({
      url: routePath,
      content: html,
      attributes: {
        title: String(attributes.title ?? ''),
        nav_title: String(attributes.nav_title ?? ''),
      },
    });
  }

  // create the pagefind index
  const { index } = await pagefind.createIndex({ forceLanguage: 'en' });
  if (!index) {
    console.error('Failed to create Pagefind index for documentation');
    return;
  }

  // add each rendered HTML file to the index
  await Promise.all(
    htmlRecords.map(async (record) => {
      await index?.addCustomRecord({
        url: record.url,
        content: record.content,
        language: 'en-US',
        meta: record.attributes,
      });
    })
  );

  return index;
}

const CERT_FOLDER = path.resolve(__dirname, 'certs');

export async function generateCertificate() {
  const commonName = envFQDN ? envFQDN.toLowerCase() : 'localhost';

  await mkdir(path.join(CERT_FOLDER, commonName), { recursive: true });

  const caKeyPath = path.join(CERT_FOLDER, commonName, 'ca-key.pem');
  const caCertPath = path.join(CERT_FOLDER, commonName, 'ca-cert.pem');
  const caCertDerPath = path.join(CERT_FOLDER, commonName, 'ca-cert.crt');
  const serverKeyPath = path.join(CERT_FOLDER, commonName, 'private-key.pem');
  const serverCertPath = path.join(CERT_FOLDER, commonName, 'cert.pem');
  const serverCertDerPath = path.join(CERT_FOLDER, commonName, 'cert.crt');

  if (existsSync(serverKeyPath) && existsSync(serverCertPath) && existsSync(caCertPath)) {
    return {
      key: await readFile(serverKeyPath, { encoding: 'utf-8' }),
      cert: await readFile(serverCertPath, { encoding: 'utf-8' }),
    };
  }

  let caKeys: { privateKey: forge.pki.rsa.PrivateKey; publicKey: forge.pki.PublicKey };
  let caCert: forge.pki.Certificate;
  let caAttrs: forge.pki.CertificateField[];

  // if the CA key or certificate does not exist, generate a new one
  if (!existsSync(caKeyPath) || !existsSync(caCertPath)) {
    caKeys = forge.pki.rsa.generateKeyPair(2048);
    caCert = forge.pki.createCertificate();
    caCert.publicKey = caKeys.publicKey;
    caCert.serialNumber = '01';
    caCert.validity.notBefore = new Date();
    caCert.validity.notAfter = new Date();
    caCert.validity.notAfter.setFullYear(caCert.validity.notBefore.getFullYear() + 10);
    caAttrs = [{ name: 'commonName', value: `${commonName} Development CA` }];
    caCert.setSubject(caAttrs);
    caCert.setIssuer(caAttrs);
    caCert.setExtensions([
      { name: 'basicConstraints', cA: true, critical: true },
      { name: 'keyUsage', keyCertSign: true, cRLSign: true, critical: true },
      { name: 'subjectKeyIdentifier' },
    ]);
    caCert.sign(caKeys.privateKey, forge.md.sha256.create());

    await writeFile(caKeyPath, forge.pki.privateKeyToPem(caKeys.privateKey), { encoding: 'utf-8' });
    await writeFile(caCertPath, forge.pki.certificateToPem(caCert), { encoding: 'utf-8' });
    await writeFile(
      caCertDerPath,
      Buffer.from(forge.asn1.toDer(forge.pki.certificateToAsn1(caCert)).getBytes(), 'binary')
    );
  }

  // otherwise, read the existing CA key and certificate from disk
  else {
    const caKeyPem = await readFile(caKeyPath, { encoding: 'utf-8' });
    const caCertPem = await readFile(caCertPath, { encoding: 'utf-8' });
    caKeys = {
      privateKey: forge.pki.privateKeyFromPem(caKeyPem),
      publicKey: forge.pki.certificateFromPem(caCertPem).publicKey,
    };
    caCert = forge.pki.certificateFromPem(caCertPem);
    caAttrs = caCert.subject.attributes;
  }

  // create a new server key and certificate signed by the CA
  const serverKeys = forge.pki.rsa.generateKeyPair(2048);
  const serverCert = forge.pki.createCertificate();
  serverCert.publicKey = serverKeys.publicKey;
  serverCert.serialNumber = '02';
  serverCert.validity.notBefore = new Date();
  serverCert.validity.notAfter = new Date();
  serverCert.validity.notAfter.setFullYear(serverCert.validity.notBefore.getFullYear() + 2);
  serverCert.setSubject([{ name: 'commonName', value: commonName }]);
  serverCert.setIssuer(caAttrs);
  serverCert.setExtensions([
    {
      name: 'subjectAltName',
      altNames: [
        ...(envFQDN
          ? [
              { type: 2, value: envFQDN.toLowerCase() },
              { type: 2, value: hostname().toLowerCase() },
            ]
          : [{ type: 2, value: hostname().toLowerCase() }]),
        { type: 7, ip: '127.0.0.1' },
        { type: 2, value: 'localhost' },
        { type: 2, value: '794693d8-4d0e-4a0b-b0d7-5a5f0a957091-rawebdev.local' },
      ],
    },
    { name: 'extKeyUsage', serverAuth: true },
  ]);
  serverCert.sign(caKeys.privateKey, forge.md.sha256.create());

  await writeFile(serverKeyPath, forge.pki.privateKeyToPem(serverKeys.privateKey), { encoding: 'utf-8' });
  await writeFile(serverCertPath, forge.pki.certificateToPem(serverCert), { encoding: 'utf-8' });
  await writeFile(
    serverCertDerPath,
    Buffer.from(forge.asn1.toDer(forge.pki.certificateToAsn1(serverCert)).getBytes(), 'binary')
  );

  return {
    key: forge.pki.privateKeyToPem(serverKeys.privateKey),
    cert: forge.pki.certificateToPem(serverCert),
  };
}
