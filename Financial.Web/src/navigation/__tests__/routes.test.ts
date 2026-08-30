import { describe, expect, it } from 'vitest'
import { NAV_TREE } from '../navTree'
import { PAGE_ROUTES } from '../routes'

// The sidebar and the router are two independent lists that have to describe the same set of
// pages. Nothing in the type system connects them, and neither failure mode is visible at build
// time: a NAV_TREE entry with no route renders a sidebar link that 404s, and a route missing from
// NAV_TREE is unreachable except by typing the URL. These tests are the only thing that notices.

const navRoutes = NAV_TREE.flatMap((category) => [
  ...category.children.map((child) => child.route),
  ...(category.groups ?? []).flatMap((group) => group.children.map((child) => child.route)),
])
const declaredRoutes = PAGE_ROUTES.map((route) => `/${route.path}`)

describe('route and sidebar agreement', () => {
  it('every sidebar destination has a route declared for it', () => {
    const missing = navRoutes.filter((route) => !declaredRoutes.includes(route))

    expect(missing, `NAV_TREE lists these routes, but PAGE_ROUTES does not declare them: ${missing.join(', ')}`).toEqual([])
  })

  it('every declared route is reachable from the sidebar', () => {
    const unreachable = declaredRoutes.filter((route) => !navRoutes.includes(route))

    expect(unreachable, `PAGE_ROUTES declares these routes, but no NAV_TREE entry links to them: ${unreachable.join(', ')}`).toEqual([])
  })

  it('declares no duplicate route paths', () => {
    expect(new Set(declaredRoutes).size).toBe(declaredRoutes.length)
  })

  it('declares every route relative to the root route, with no leading slash', () => {
    const absolute = PAGE_ROUTES.filter((route) => route.path.startsWith('/'))

    expect(absolute.map((route) => route.path)).toEqual([])
  })
})
