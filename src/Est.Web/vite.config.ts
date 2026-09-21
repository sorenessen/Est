import { defineConfig } from 'vite'
import { cpSync, mkdirSync } from 'node:fs'
import { resolve } from 'node:path'

const cesiumSource = resolve('node_modules/cesium/Build/Cesium')

function copyCesiumAssets(destination: string): void {
  mkdirSync(destination, { recursive: true })

  for (const directory of [
    'Workers',
    'Assets',
    'Widgets',
    'ThirdParty',
  ]) {
    cpSync(
      resolve(cesiumSource, directory),
      resolve(destination, directory),
      { recursive: true },
    )
  }
}

export default defineConfig({
  define: {
    CESIUM_BASE_URL: JSON.stringify('/cesium-assets'),
  },
  server: {
    proxy: {
      '/api': {
        target:
          process.env.EST_API_PROXY_TARGET ??
          'http://localhost:5026',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/api/, ''),
      },
    },
  },
  build: {
    rolldownOptions: {
      input: {
        main: resolve('index.html'),
        cesium: resolve('cesium.html'),
      },
    },
  },
  plugins: [
    {
      name: 'copy-cesium-assets',
      configureServer() {
        copyCesiumAssets(resolve('public/cesium-assets'))
      },
      closeBundle() {
        copyCesiumAssets(resolve('dist/cesium-assets'))
      },
    },
  ],
})
