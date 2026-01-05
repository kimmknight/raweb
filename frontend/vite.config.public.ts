import { glob, readFile, writeFile } from 'fs/promises';
import { defineConfig, mergeConfig, Plugin, ResolvedConfig } from 'vite';
import baseConfig from './vite.config.ts';

export default defineConfig(async ({ command, mode }) => {
  process.env.RAWEB_USE_PRETTY_HTML_PATHS = '1';

  const resolvedBaseConfig =
    typeof baseConfig === 'function' ? await baseConfig({ command, mode }) : baseConfig;

  const base = process.env.RAWEB_PUBLIC_BASE || resolvedBaseConfig.base;

  return mergeConfig(resolvedBaseConfig, {
    define: {
      __APP_INIT_DETAILS_API_PATH__: JSON.stringify(`/api/app-init-details.json`),
    },
    base,
    plugins: [
      (() => {
        let viteConfig: ResolvedConfig;

        return {
          name: 'raweb:generate-public-app-init',
          enforce: 'post',

          configResolved(config) {
            viteConfig = config;
          },

          // generate a placeholder app-init-details.json for the public build
          // so that the docs portion of the app can work without a backend
          async generateBundle(_, bundle) {
            const json = JSON.stringify({
              iisBase: '/',
              appBase: '/',
              authUser: {
                username: 'anonymous',
                domain: 'RAWEB',
                fullName: 'Unauthenticated',
                isLocalAdministrator: false,
              },
              userNamespace: 'PUBLIC:anonymous',
              terminalServerAliases: {},
              policies: {
                combineTerminalServersModeEnabled: null,
                favoritesEnabled: null,
                flatModeEnabled: null,
                hidePortsEnabled: null,
                iconBackgroundsEnabled: null,
                simpleModeEnabled: null,
                passwordChangeEnabled: null,
                anonymousAuthentication: 'allow',
                signedInUserGlobalAlerts: null,
              },
              machineName: 'RAWeb Public (Placeholder)',
              envMachineName: 'RAWEB-PUBLIC-PLACEHOLDER',
              coreVersion: '1.0.0.0',
              webVersion: '2000-01-01T00:00:00.000Z',
              capabilities: {},
            });

            this.emitFile({
              type: 'asset',
              fileName: 'api/app-init-details.json',
              source: json,
            });
          },
        } satisfies Plugin;
      })(),
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
                .replace('%usercontent%', '');
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
});
