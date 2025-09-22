import vue from '@vitejs/plugin-vue';
import { existsSync } from 'fs';
import { cp, readFile, rm, writeFile } from 'fs/promises';
import path from 'path';
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

  return {
    plugins: [
      vue(),
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
                matchingEntry = entryPoints.find(([name]) => name === '*');
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
      sourcemap: true,
      target: 'es2022',
      rollupOptions: {
        input: {
          index: path.resolve(__dirname, './lib/entry.dist.mjs'),
          login: path.resolve(__dirname, './lib/login-entry.dist.mjs'),
          logoff: path.resolve(__dirname, './lib/logoff-entry.dist.mjs'),
          password: path.resolve(__dirname, './lib/password-entry.dist.mjs'),
        },
        output: {
          entryFileNames: 'lib/assets/[name]-[hash].js',
          chunkFileNames: 'lib/assets/[name]-[hash].js',
          assetFileNames: 'lib/assets/[name]-[hash].[ext]',
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
