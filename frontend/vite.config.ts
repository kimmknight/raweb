import vue from '@vitejs/plugin-vue';
import { cp, readFile, writeFile } from 'fs/promises';
import path from 'path';
import { defineConfig } from 'vite';

export default defineConfig({
  plugins: [
    vue(),
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
    },
  ],
  resolve: {
    alias: {
      $components: path.resolve(__dirname, './lib/components'),
      $icons: path.resolve(__dirname, './lib/assets/icons.ts'),
      $utils: path.resolve(__dirname, './lib/utils'),
    },
  },
  base: './',
  build: {
    outDir: path.resolve(__dirname, '../aspx/wwwroot'),
    emptyOutDir: false,
    sourcemap: true,
    rollupOptions: {
      input: {
        main: path.resolve(__dirname, './lib/entry.dist.mjs'),
      },
      output: {
        entryFileNames: 'lib/assets/[name].js',
        chunkFileNames: 'lib/assets/[name].js',
        assetFileNames: 'lib/assets/[name].[ext]',
      },
    },
  },
});
