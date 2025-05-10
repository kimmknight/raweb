import vue from '@vitejs/plugin-vue';
import { cp, rm } from 'fs/promises';
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
        await cp(libAssetsDir, distAssetsDir, { recursive: true, force: false });

        // move lib/winui.css to dist/lib/winui.css
        const libWinuiCssPath = path.resolve(libDir, 'winui.css');
        const distWinuiCssPath = path.resolve(distDir, 'lib/winui.css');
        await cp(libWinuiCssPath, distWinuiCssPath, { force: true });

        // move lib/controls to dist/lib/controls
        const libControlsDir = path.resolve(libDir, 'controls');
        const distControlsDir = path.resolve(distDir, 'lib/controls');
        await cp(libControlsDir, distControlsDir, { recursive: true, force: false });

        // replace dist/Default.aspx with dist/Default.dist.aspx
        const distDefaultFilePath = path.resolve(distDir, 'Default.aspx');
        const distDefaultFileDistPath = path.resolve(distDir, 'Default.dist.aspx');
        await cp(distDefaultFileDistPath, distDefaultFilePath, { force: true });
        await rm(distDefaultFileDistPath, { force: true }); // remove dist/Default.aspx
      },
      closeBundle: () => {
        const isWatchMode = process.argv.includes('--watch');
        if (isWatchMode) {
          console.log('\nApp ready. Watching for changes...\n');
          console.log('Local: https://localhost/raweb/app/');
          console.log('Network: https://localhost/raweb/app/');
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
  base: '/raweb/app/',
  build: {
    outDir: path.resolve(__dirname, '../aspx/wwwroot/app'),
    emptyOutDir: true,
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
