# Shared Design Token Contract

## Rule

React and WPF may expose tokens differently, but both must use equivalent
semantic concepts. Actual token values belong in the React theme and WPF
ResourceDictionaries built on the libraries adopted in
`decisions/ADR-004-fluent-component-library-adoption.md`. The brand and status
color values specifically are decided in
`decisions/ADR-005-brand-and-status-colors.md`; this file defines the
semantic token names and spacing/layout scale only, not literal color
values.

Feature code must not introduce repeated raw colors, spacing values, typography
definitions, radii, shadows, or z-index values.

## Spacing

Use a 4px rhythm.

| Semantic token | Logical value | Intended use |
|---|---:|---|
| `space.xs` | 4px | Icon and micro spacing |
| `space.s` | 8px | Tightly related elements |
| `space.m` | 12px | Compact groups and label/control relation |
| `space.l` | 16px | Standard control and field gaps |
| `space.xl` | 24px | Section separation |
| `space.xxl` | 32px | Major page separation |
| `space.xxxl` | 40px+ | Deliberate major separation |

## Layout

| Semantic token | Default |
|---|---|
| `layout.pagePadding` | 24px on wide layouts, reduced responsively |
| `layout.sectionGap` | 24px |
| `layout.componentGap` | 16px |
| `layout.fieldGap` | 16px |
| `layout.labelGap` | 4px–8px |
| `layout.formColumns.wide` | 4 |
| `layout.formColumns.medium` | 2 |
| `layout.formColumns.narrow` | 1 |

## Typography

| Semantic token | Use |
|---|---|
| `text.pageTitle` | Primary page title |
| `text.sectionTitle` | Major section heading |
| `text.subsectionTitle` | Group heading |
| `text.body` | Normal reading content |
| `text.bodyStrong` | Important supporting information |
| `text.caption` | Low-priority metadata |
| `text.fieldLabel` | Input labels |
| `text.numeric` | Financial/numeric values; tabular figures where supported |

Use sentence case by default. Use actual Fluent typography tokens/resources
rather than hard-coded styles.

## Colours and surfaces

| Semantic token | Use |
|---|---|
| `color.text.primary` | Main text |
| `color.text.secondary` | Supporting text |
| `color.background.canvas` | Page background |
| `color.background.surface` | Main content surface |
| `color.background.subtle` | Secondary grouping |
| `color.border.neutral` | Structural separation |
| `color.action.primary` | Primary action |
| `color.status.info` | Information |
| `color.status.success` | Success/completed |
| `color.status.warning` | Needs attention |
| `color.status.danger` | Error/destructive/invalid |
| `color.focus` | Keyboard focus |

Do not rely on colour alone. Pair status colour with text, icon, structure, or
an accessible state.

## Surfaces and elevation

| Semantic token | Use |
|---|---|
| `surface.page` | Page background |
| `surface.card` | Distinct grouped content only |
| `surface.overlay` | Dialogs, menus, and drawers |
| `border.subtle` | Light grouping/separation |
| `border.strong` | Important separation |
| `elevation.card` | Restrained card elevation |
| `elevation.overlay` | Temporary layered surfaces |

Avoid excessive cards, nested containers, shadows, and borders.

## Component states

Interactive controls must account for applicable:

- Default
- Hover
- Focus-visible
- Pressed
- Selected
- Disabled
- Read-only
- Loading
- Error
- Warning
- Success