import { webDarkTheme, webLightTheme } from '@fluentui/react-components'

/**
 * Brand/status colors per docs/ui/decisions/ADR-005-brand-and-status-colors.md:
 * Fluent's default web theme ramp is close enough to the app's existing de facto
 * accent (#007acc) to adopt as-is; a custom ramp is only worth generating if a
 * full rollout later shows a noticeable visual gap.
 */
export const financialLightTheme = webLightTheme
export const financialDarkTheme = webDarkTheme
