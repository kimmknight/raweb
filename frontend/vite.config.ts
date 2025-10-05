import vue from '@vitejs/plugin-vue';
import { DOMParser } from '@xmldom/xmldom';
import { existsSync } from 'fs';
import { cp, readFile, rm, writeFile } from 'fs/promises';
import markdownItAttrs from 'markdown-it-attrs';
import markdownItFootnotes from 'markdown-it-footnote';
import path from 'path';
import markdown from 'unplugin-vue-markdown/vite';
import { defineConfig, loadEnv, Plugin, ResolvedConfig } from 'vite';

let iisBase: string | null = null;

export default defineConfig(async ({ mode }) => {
  process.env = { ...process.env, ...loadEnv(mode, process.cwd(), 'RAWEB_') };

  if (iisBase === null) {
    iisBase = await fetch(
      `${process.env.RAWEB_SERVER_ORIGIN}${process.env.RAWEB_SERVER_PATH}/api/app-init-details`
    )
      .then((res) => res.json())
      .then((data): string => data.iisBase)
      .catch((error) => {
        if (mode === 'production') {
          return null; // in production mode, knowing the IIS base is not required for a successful build
        }

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
  }

  // we do not use the IIS base in production mode because the IIS app could be at any path
  const base = mode === 'development' && iisBase !== null ? iisBase : './';
  const resolvedBase = base.endsWith('/') ? base.slice(0, -1) : base;

  // since the docs can significantly increase the application size, we allow excluding them from builds via an env var
  const excludeDocs = process.env.RAWEB_EXCLUDE_DOCS === 'true' || process.env.RAWEB_EXCLUDE_DOCS === '1';

  return {
    define: {
      __APP_INIT_DETAILS_API_PATH__: JSON.stringify(`./api/app-init-details`),
      __DOCS_EXCLUDED__: JSON.stringify(excludeDocs),
    },
    plugins: [
      markdown({
        frontmatter: true,
        exportFrontmatter: true,
        markdownItSetup(md) {
          md.use(markdownItAttrs); // allow setting attributes on markdown elements via {#id .class key=val}
          md.use(markdownItFootnotes); // support footnotes

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
          md.renderer.rules.fence = (tokens, idx, options, env, self) => {
            let value = originalFence ? originalFence(tokens, idx, options, env, self) : '';
            const dom = new DOMParser().parseFromString(value, 'text/html');
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

            return code;
          },
        },
      }),
      vue({
        include: [/\.vue$/, /\.md$/],
      }),
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

              // if the entry point is not found, but the request is for an HTML page (not API or webfeed),
              // serve the default entry point (index)
              if (
                !matchingEntry &&
                req.headers.accept?.includes('text/html') &&
                !cleanUrl.startsWith(`${resolvedBase}/api`) &&
                !cleanUrl.startsWith(`${resolvedBase}/webfeed.aspx`)
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

              // read the HTML template file
              const template = await readFile('app.html', 'utf-8');

              // inject the entry script tag and base tag into the HTML template
              const html = template
                .replace('%raweb.basetag%', `<base href="${resolvedBase}/">`)
                .replace('%raweb.head%', '')
                .replace(
                  '%raweb.scripts%',
                  `<script type="module" src="${resolvedBase}/${entryRelativePath}"></script>`
                )
                .replace('%raweb.servername%', 'Development')
                .replaceAll('%raweb.base%', resolvedBase);

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

              if (output.type === 'chunk' && output.isEntry) {
                entryPoints[output.name] = { scripts: [fileName], css: [] };

                if (output.viteMetadata) {
                  const cssFiles = Array.from(output.viteMetadata.importedCss || []);
                  entryPoints[output.name].css.push(...cssFiles);
                }
              }
            }

            // include the shared (common code) scripts and css to all entries
            const sharedScripts = Object.keys(bundle).filter((f) => f.includes('shared-') && f.endsWith('.js'));
            const sharedCss = Object.keys(bundle).filter((f) => f.includes('shared-') && f.endsWith('.css'));
            for (const entry of Object.values(entryPoints)) {
              entry.scripts.push(...sharedScripts);
              entry.css.push(...sharedCss);
            }

            // generate HTML for each entry point
            for (const [entryName, assets] of Object.entries(entryPoints)) {
              const cssTags = assets.css.map((c) => `<link rel="stylesheet" href="./${c}">`).join('\n');
              const scriptTags = assets.scripts
                .filter((s) => !s.endsWith('.map')) // exclude sourcemap files
                .map((s) => `<script type="module" src="./${s}"></script>`)
                .join('\n');

              const html = template
                .replace('%raweb.head%', cssTags)
                .replace('%raweb.scripts%', scriptTags)
                .replace('%raweb.title%', entryName)
                .replaceAll('%raweb.base%', resolvedBase);

              this.emitFile({
                type: 'asset',
                fileName: `${entryName}.html`,
                source: html,
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
            const libAssetsDir = path.resolve(distDir, 'lib/assets');

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
          } else {
            console.log('\nFrontend app installed.\n');
          }
          console.log(`Local: https://localhost${iisBase || '/raweb/'}`);
          console.log(`Network: https://localhost${iisBase || '/raweb/'}`);
        },
      } satisfies Plugin,
    ],
    resolve: {
      alias: {
        $components: path.resolve(__dirname, './lib/components'),
        $icons: path.resolve(__dirname, './lib/assets/icons.ts'),
        $utils: path.resolve(__dirname, './lib/utils'),
        $stores: path.resolve(__dirname, './lib/stores'),
      },
    },
    base,
    build: {
      outDir: path.resolve(__dirname, '../aspx/wwwroot'),
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
      // proxy API and authentication requests to the backend server
      proxy: {
        [`${resolvedBase}/api`]: {
          target: process.env.RAWEB_SERVER_ORIGIN,
          changeOrigin: true, // set Host header to match target
        },
        [`${resolvedBase}/webfeed.aspx`]: {
          target: process.env.RAWEB_SERVER_ORIGIN,
          changeOrigin: true,
        },
        [`${resolvedBase}/auth/login.aspx`]: {
          target: process.env.RAWEB_SERVER_ORIGIN,
          changeOrigin: true,
        },
      },
    },
  };
});
