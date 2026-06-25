import { defineConfig } from 'vitest/config'
import { loadEnv } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), 'API_')
  const apiTarget = env.API_BASE_URL || 'http://localhost:5190'
  return {
    plugins: [react()],
    define: {
      'import.meta.env.API_BASE_URL': JSON.stringify(''),
    },
    server: {
      proxy: {
        '/api': { target: apiTarget, changeOrigin: true },
      },
    },
    test: {
      environment: 'jsdom',
      setupFiles: './src/setupTests.ts',
    },
  }
})
