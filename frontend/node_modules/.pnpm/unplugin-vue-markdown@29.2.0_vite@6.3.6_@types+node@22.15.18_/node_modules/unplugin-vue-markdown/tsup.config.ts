import type { Options } from 'tsup'

export default <Options>{
  entryPoints: [
    'src/*.ts',
  ],
  clean: true,
  format: ['cjs', 'esm'],
  dts: true,
  shims: true,
  splitting: true,
  onSuccess: 'npm run build:fix',
}
