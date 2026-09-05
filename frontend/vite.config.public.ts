import { existsSync, statSync } from 'fs';
import { glob, readFile, writeFile } from 'fs/promises';
import path from 'path';
import { defineConfig, mergeConfig, Plugin, ResolvedConfig } from 'vite';
import { demoMockApiPlugin } from './demo-mock-api/plugin.ts';
import baseConfig from './vite.config.ts';

export default defineConfig(async ({ command, mode }) => {
  process.env.RAWEB_USE_PRETTY_HTML_PATHS = '1';
  process.env.RAWEB_PUBLIC_BUILD = '1';

  const resolvedBaseConfig =
    typeof baseConfig === 'function' ? await baseConfig({ command, mode }) : baseConfig;

  const base = process.env.RAWEB_PUBLIC_BASE || '/';

  const merged = mergeConfig(resolvedBaseConfig, {
    define: {
      __APP_INIT_DETAILS_API_PATH__: JSON.stringify(`${base || '/'}api/app-init-details.json`),
    },
    base,
    plugins: [
      demoMockApiPlugin({ base }),
      {
        // `vite preview` falls back to the root index.html for any path it can't resolve to a
        // literal file, which is right for a single-page app but wrong here - this build has real
        // nested pages (docs/, password/, etc.) whose URL has no trailing slash, e.g. `/docs` rather
        // than `/docs/`. Real static hosts (GitHub Pages, Netlify, ...) resolve that to
        // `docs/index.html` themselves; this middleware makes `vite preview` do the same, so local
        // testing matches how the deployed build actually behaves. Checking the filesystem directly
        // (rather than guessing from the URL's extension) matters here: a docs route can legitimately
        // contain dots with no trailing slash, e.g. `/docs/policies/App.Foo.Enabled`, which
        // `path.extname` would otherwise mistake for a static asset request and skip.
        name: 'raweb:preview-clean-urls',
        configurePreviewServer(server) {
          server.middlewares.use((req, res, next) => {
            const cleanUrl = req.url?.split('?')[0].split('#')[0];
            if (!cleanUrl || cleanUrl.endsWith('/')) return next();

            const literalPath = path.join(server.config.build.outDir, cleanUrl);
            if (existsSync(literalPath) && statSync(literalPath).isFile()) return next();

            const indexPath = path.join(literalPath, 'index.html');
            if (existsSync(indexPath)) req.url = `${cleanUrl}/index.html`;
            next();
          });
        },
      } satisfies Plugin,
      (() => {
        let viteConfig: ResolvedConfig;

        return {
          name: 'raweb:set-base-tag',
          enforce: 'post',

          configResolved(config) {
            viteConfig = config;
          },

          async writeBundle(options) {
            const distDir = options.dir;
            if (!distDir) {
              throw new Error('distDir is not defined');
            }

            const htmlFiles = glob('**/*.html', { cwd: distDir });
            for await (const file of htmlFiles) {
              const filePath = `${distDir}/${file}`;
              const html = (await readFile(filePath, 'utf-8'))
                .replace('%raweb.basetag%', `<base href="${viteConfig.base}" />`)
                .replace('%raweb.overrides%', '')
                .replace('%raweb.splashlogoimg%', '');
              await writeFile(filePath, html, 'utf-8');
            }
          },
        } satisfies Plugin;
      })(),
    ],
    build: {
      outDir: 'dist',
      emptyOutDir: true,
    },
  });

  // mergeConfig deep-merges plain objects, so it can't be used to clear the base config's
  // server.proxy entries. The demo/public build never has a real backend to talk to (and
  // RAWEB_SERVER_ORIGIN may not even be set), so replace the proxy config outright.
  merged.server = { ...merged.server, proxy: {} };

  return merged;
});
