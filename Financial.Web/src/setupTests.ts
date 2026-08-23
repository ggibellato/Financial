import { createRequire } from 'node:module'
import '@testing-library/jest-dom/vitest'
import { cleanup } from '@testing-library/react'
import { afterEach, vi } from 'vitest'

afterEach(() => {
  cleanup()
})

// Fluent UI React v9 (see docs/ui/decisions/ADR-004-fluent-component-library-adoption.md)
// transitively depends on tabster for focus management. tabster's CJS build
// declares its named exports through a runtime getter loop that no static
// CJS-interop analysis can detect — not Node's native ESM loader, not Vite/
// Rolldown's SSR resolution — so under Vitest, `@fluentui/react-components`'s
// internal `import { createTabster } from 'tabster'` fails with "does not
// provide an export named...". This does not affect the real app: `npm run
// build`/`npm run dev` use Vite's client bundling, a different code path that
// does not hit this bug. It is specific to Vitest's SSR-style module runner.
//
// vi.mock cannot intercept the failing import directly (it only reaches
// package-to-package imports inside node_modules if Vite also chooses to
// bundle rather than externalize them, which is inconsistent here). It does
// reliably intercept our own direct imports of `@fluentui/react-components`/
// `@fluentui/react-icons`, so mock those two entry points instead, each
// resolving to the real published package via a genuine `require()` (which
// needs no static export analysis and works correctly) rather than a stub.
const require = createRequire(import.meta.url)
vi.mock('@fluentui/react-components', () => require('@fluentui/react-components'))
vi.mock('@fluentui/react-icons', () => require('@fluentui/react-icons'))

if (!('ResizeObserver' in globalThis)) {
  class ResizeObserverMock {
    observe() {}
    unobserve() {}
    disconnect() {}
  }

  ;(globalThis as typeof globalThis & { ResizeObserver: typeof ResizeObserverMock }).ResizeObserver =
    ResizeObserverMock
}

if (!window.matchMedia) {
  window.matchMedia = (query: string) =>
    ({
      matches: false,
      media: query,
      onchange: null,
      addEventListener: () => {},
      removeEventListener: () => {},
      addListener: () => {},
      removeListener: () => {},
      dispatchEvent: () => false,
    }) as MediaQueryList
}
