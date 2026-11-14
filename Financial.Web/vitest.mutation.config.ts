import { defineConfig, mergeConfig } from 'vitest/config'
import viteConfig from './vite.config'

const defaultInclude = ['src/utils/**/__tests__/**/*.test.ts', 'src/hooks/**/__tests__/**/*.test.{ts,tsx}']

export default defineConfig((env) =>
  mergeConfig(
    typeof viteConfig === 'function' ? viteConfig(env) : viteConfig,
    defineConfig({
      test: {
        include: process.env.STRYKER_TEST_INCLUDE?.split(',') ?? defaultInclude,
      },
    }),
  ),
)
