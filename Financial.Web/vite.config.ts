import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'
import { configDefaults, coverageConfigDefaults } from 'vitest/config'

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), 'API_')
  const apiBaseUrl = env.API_BASE_URL ?? '/api/v1/financial'
  const apiProxyTarget = env.API_PROXY_TARGET ?? 'http://localhost:5190'
  return {
    plugins: [react()],
    define: {
      'import.meta.env.API_BASE_URL': JSON.stringify(apiBaseUrl),
    },
    server: {
      proxy: {
        '/api': { target: apiProxyTarget, changeOrigin: true },
      },
    },
    test: {
      environment: 'jsdom',
      exclude: [...configDefaults.exclude, 'tests/e2e/**'],
      setupFiles: './src/setupTests.ts',
      coverage: {
        provider: 'v8',
        all: true,
        reporter: ['text', 'json-summary', 'lcov'],
        exclude: [
          ...coverageConfigDefaults.exclude,
          'src/api/generated/**',
          'src/main.tsx',
          'src/setupTests.ts',
          'src/test/**',
          'src/test-utils/**',
        ],
      },
    },
  }
})
