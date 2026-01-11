import vue from '@vitejs/plugin-vue';
import { DOMParser, XMLSerializer } from '@xmldom/xmldom';
import readFrontmatter from 'front-matter';
import { existsSync, readFileSync } from 'fs';
import { cp, mkdir, readdir, readFile, rm, writeFile } from 'fs/promises';
import i18next from 'i18next';
import Backend from 'i18next-fs-backend';
import { imageSize } from 'image-size';
import markdownItAttrs from 'markdown-it-attrs';
import markdownItFootnotes from 'markdown-it-footnote';
import { randomUUID } from 'node:crypto';
import { hostname } from 'os';
import * as pagefind from 'pagefind';
import path from 'path';
import selfsigned from 'selfsigned';
import markdown from 'unplugin-vue-markdown/vite';
import { createServer, defineConfig, loadEnv, Plugin, ResolvedConfig, UserConfig } from 'vite';

let iisBase: string | null = null;
let envFQDN: string | null = null;

export default defineConfig(async ({ mode }) => {
  process.env = { ...process.env, ...loadEnv(mode, process.cwd(), 'RAWEB_') };

  if (!process.env.RAWEB_SERVER_ORIGIN) {
    console.warn(
      '\nWarning: RAWEB_SERVER_ORIGIN is not set. Defaulting to http://localhost:8080. ' +
        'Please set RAWEB_SERVER_ORIGIN in your .env file to point to the RAWeb server.\n'
    );
    process.env.RAWEB_SERVER_ORIGIN = 'http://localhost:8080';
  }

  if (iisBase === null && mode === 'development') {
    const { _iisBase, _envFQDN } = await fetchWithRetry(
      `${process.env.RAWEB_SERVER_ORIGIN}${process.env.RAWEB_SERVER_PATH ?? ''}/api/app-init-details`
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

  return {
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
      }),
      (() => {
        let viteConfig: ResolvedConfig;
        const pluginName = 'raweb:generate-docs-search-index';

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
                console.log('[vite] Generating Pagefind search index...');
                indexPromise = getDocsPagefindIndex(server);
                indexPromise.then((indexResult) => (index = indexResult));
                console.log('[vite] Pagefind search index generated.');
              } catch (error) {
                if (error instanceof Error && error.message.includes('transport was disconnected')) {
                  return;
                }
                console.error('[vite] Failed to generate Pagefind search index:', error);
              }
            });

            server.watcher.on('change', async (file) => {
              if (file.endsWith('.md')) {
                try {
                  console.log('[vite] Generating Pagefind search index...');
                  indexPromise = getDocsPagefindIndex(server);
                  indexPromise.then((indexResult) => (index = indexResult));
                  console.log('[vite] Pagefind search index generated.');
                } catch (error) {
                  if (error instanceof Error && error.message.includes('transport was disconnected')) {
                    return;
                  }
                  console.error('[vite] Failed to generate Pagefind search index:', error);
                }
              }
            });

            server.middlewares.use(async (req, res, next) => {
              if (!req.url || !indexPromise) {
                return next();
              }

              const cleanUrl = req.url.split('?')[0].split('#')[0];

              // skip requests that are not for pagefind assets
              if (!req.url.startsWith(`/lib/assets/pagefind/`)) {
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
              const matchingFile = indexFiles.find(({ path }) => path === cleanUrl.slice(1));
              if (!matchingFile) {
                res.statusCode = 404;
                return res.end('Not found');
              }

              res.setHeader('Content-Type', matchingFile.mimeType);
              res.end(matchingFile.content);
            });
          },

          async generateBundle(_, bundle) {
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

            // generate the search index using SSR
            console.log('[vite] Generating Pagefind search index...');
            const index = await getDocsPagefindIndex(server);
            if (!index) {
              await server.close();
              throw new Error('Failed to generate Pagefind index');
            }
            await server.close();

            // add the search index assets to the build output
            const indexFiles = await index.getFiles();
            for (const file of indexFiles.files) {
              this.emitFile({
                type: 'asset',
                fileName: `lib/assets/pagefind/${file.path}`,
                source: file.content,
              });
            }
            console.log('[vite] Pagefind search index generated.');
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

              // if the entry point is not found, but the request is for an HTML page (not API or webfeed),
              // serve the default entry point (index)
              if (
                !matchingEntry &&
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
              const overridesDir = path.resolve(__dirname, '../dotnet/RAWebServer/build/App_Data/inject');
              const overridesCssPath = path.join(overridesDir, 'index.css');
              const overridesJsPath = path.join(overridesDir, 'index.js');
              let overrides = '';
              if (existsSync(overridesCssPath)) {
                overrides += `<link rel="stylesheet" href="${resolvedBase}/inject/index.css">\n`;
              }
              if (existsSync(overridesJsPath)) {
                overrides += `<script type="module" src="${resolvedBase}/inject/index.js"></script>\n`;
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
          } else {
            console.log('\nFrontend app installed.\n');
          }
          console.log(`Local: https://localhost${iisBase || '/raweb/'}`);
          console.log(`Network: https://localhost${iisBase || '/raweb/'}`);
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

        // ensure pinia is always the ESM build
        {
          find: /^pinia$/,
          replacement: path.resolve(__dirname, 'node_modules/pinia/dist/pinia.mjs'),
        },
      ],
    },
    base,
    esbuild: {
      target: 'es2023',
      supported: { 'top-level-await': true },
    },
    optimizeDeps: {
      esbuildOptions: {
        target: 'es2023',
        supported: { 'top-level-await': true },
      },
    },
    build: {
      outDir: path.resolve(__dirname, '../dotnet/RAWebServer'),
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
      host: true,
      https: https,
      allowedHosts: ['localhost', hostname(), hostname().toLowerCase(), envFQDN, envFQDN?.toLowerCase()].filter(
        (x): x is string => !!x
      ),

      // proxy API, authentication, and injection requests to the backend server
      proxy: {
        [`${resolvedBase}/api`]: {
          target: process.env.RAWEB_SERVER_ORIGIN,
          changeOrigin: true, // set Host header to match target
        },
        [`${resolvedBase}/RDWebService.asmx`]: {
          target: process.env.RAWEB_SERVER_ORIGIN,
          changeOrigin: true,
        },
        [`${resolvedBase}/webfeed.aspx`]: {
          target: process.env.RAWEB_SERVER_ORIGIN,
          changeOrigin: true,
        },
        [`${resolvedBase}/auth/login.aspx`]: {
          target: process.env.RAWEB_SERVER_ORIGIN,
          changeOrigin: true,
        },
        [`${resolvedBase}/inject`]: {
          target: process.env.RAWEB_SERVER_ORIGIN,
          changeOrigin: true,
        },
      },
    },
  } satisfies UserConfig;
});

const MAX_WAIT_MS = 60_000;
const RETRY_INTERVAL_MS = 2000;
async function fetchWithRetry(url: string, signal?: AbortSignal) {
  const start = Date.now();

  while (Date.now() - start < MAX_WAIT_MS) {
    try {
      const res = await fetch(url, { signal });
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
  const result = await docsIndexPromise;
  docsIndexRunning = false;
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
      errorHandler: {
        warning: () => {},
        error: () => {},
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
    htmlRecords.map((record) => {
      index?.addCustomRecord({
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

async function generateCertificate() {
  const commonName = envFQDN ? envFQDN.toLowerCase() : 'localhost';

  await mkdir(path.join(CERT_FOLDER, commonName), { recursive: true });

  const privateKeyPath = path.join(CERT_FOLDER, commonName, 'private-key.pem');
  const publicKeyPath = path.join(CERT_FOLDER, commonName, 'public-key.pem');
  const certPath = path.join(CERT_FOLDER, commonName, 'cert.pem');
  const fingerprintPath = path.join(CERT_FOLDER, commonName, 'fingerprint.txt');

  // if the cert files already exist, do not regenerate
  if (
    existsSync(privateKeyPath) &&
    existsSync(publicKeyPath) &&
    existsSync(certPath) &&
    existsSync(fingerprintPath)
  ) {
    return {
      key: await readFile(privateKeyPath, { encoding: 'utf-8' }),
      cert: await readFile(certPath, { encoding: 'utf-8' }),
    };
  }

  // generate certificate authority cert
  const pems = await selfsigned.generate(
    [{ name: 'commonName', value: envFQDN ? envFQDN.toLowerCase() : 'localhost' }],
    {
      extensions: [
        {
          name: 'subjectAltName',
          altNames: [
            ...((envFQDN
              ? [
                  { type: 2, value: envFQDN.toLowerCase() },
                  { type: 2, value: hostname().toLowerCase() },
                ]
              : [{ type: 2, value: hostname().toLowerCase() }]) satisfies selfsigned.SubjectAltNameEntry[]),
            { type: 7, ip: '127.0.0.1' },
            { type: 2, value: 'localhost' },
          ],
        },
      ],
    }
  );

  // write the cert files
  await writeFile(privateKeyPath, pems.private, { encoding: 'utf-8' });
  await writeFile(publicKeyPath, pems.public, { encoding: 'utf-8' });
  await writeFile(certPath, pems.cert, { encoding: 'utf-8' });
  await writeFile(fingerprintPath, pems.fingerprint, { encoding: 'utf-8' });

  return {
    key: pems.private,
    cert: pems.cert,
  };
}
