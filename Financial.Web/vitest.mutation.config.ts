import { defineConfig, mergeConfig } from 'vitest/config'
import viteConfig from './vite.config'

export default defineConfig((env) =>
  mergeConfig(
    typeof viteConfig === 'function' ? viteConfig(env) : viteConfig,
    defineConfig({
      test: {
        include: ['src/utils/**/__tests__/**/*.test.ts', 'src/hooks/**/__tests__/**/*.test.{ts,tsx}'],
      },
    }),
  ),
)
