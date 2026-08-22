import type { ReactElement } from 'react'
import { FluentProvider } from '@fluentui/react-components'
import { render as testingLibraryRender, type RenderOptions } from '@testing-library/react'
import { financialLightTheme } from '../theme/fluentTheme'

/**
 * Fluent UI React v9 components need a FluentProvider ancestor to render their
 * real (non-fallback) styles and to resolve `useId()`-based label associations
 * correctly. Any component migrated to Fluent per docs/ui/decisions/ADR-004
 * must be rendered through this wrapper in tests, not the plain
 * `@testing-library/react` render.
 */
export function render(ui: ReactElement, options?: RenderOptions) {
  return testingLibraryRender(<FluentProvider theme={financialLightTheme}>{ui}</FluentProvider>, options)
}

export { fireEvent, screen, waitFor, within } from '@testing-library/react'
