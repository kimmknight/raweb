import vue from '@vitejs/plugin-vue';
import { cp, readFile, rm, writeFile } from 'fs/promises';
import path from 'path';
import { defineConfig, Plugin, ResolvedConfig } from 'vite';

export default defineConfig(() => {
  const base = './';
  const resolvedBase = base.endsWith('/') ? base.slice(0, -1) : base;

  return {
    plugins: [
      vue(),
      {
        name: 'raweb:generate-entry-html',
        apply: 'build',
        enforce: 'post',
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
      } satisfies Plugin,
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
          const isWatchMode = process.argv.includes('--watch');
          if (isWatchMode) {
            console.log('\nApp ready. Watching for changes...\n');
            console.log('Local: https://localhost/raweb/');
            console.log('Network: https://localhost/raweb/');
          } else {
            console.log('\nFrontend app installed.');
          }
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
          manualChunks: (id) => {
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
  };
});
