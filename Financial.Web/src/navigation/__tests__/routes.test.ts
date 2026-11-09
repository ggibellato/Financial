import { describe, expect, it } from 'vitest'
import { MovedPageRedirect } from '../lazyPages'
import { NAV_TREE } from '../navTree'
import { ALL_ROUTES, DETAIL_ROUTES, PAGE_ROUTES, REDIRECT_ROUTES } from '../routes'

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

  it('orders the CashFlow menu as the PRD', () => {
    const cashFlowLabels = NAV_TREE.find((category) => category.id === 'cashflow')!.children.map((child) => child.label)

    expect(cashFlowLabels).toEqual(['Accounts', 'Monthly', 'Annual Summary', 'Mensais', 'Investment Snapshots'])
  })

  it('declares no duplicate route paths', () => {
    const allRoutes = ALL_ROUTES.map((route) => route.path)

    expect(new Set(allRoutes).size).toBe(allRoutes.length)
  })

  it('declares a route for every sidebar detail page, and no other detail route', () => {
    const navDetailRoutes = NAV_TREE.flatMap((category) => category.children.flatMap((child) => (child.detail ? [child.detail.route] : [])))

    expect(DETAIL_ROUTES.map((route) => `/${route.path}`).sort()).toEqual(navDetailRoutes.sort())
  })

  it('keeps the retired addresses as redirects, off the sidebar', () => {
    expect(REDIRECT_ROUTES.map((route) => route.path)).toEqual(['admin/cashflow/reserve-buckets', 'cashflow/controle-mae', 'cashflow/reserva'])
    expect(navRoutes).not.toContain('/admin/cashflow/reserve-buckets')
    expect(navRoutes).not.toContain('/cashflow/controle-mae')
    expect(navRoutes).not.toContain('/cashflow/reserva')
    expect(declaredRoutes).not.toContain('/cashflow/controle-mae')
    expect(declaredRoutes).not.toContain('/cashflow/reserva')
  })

  it.each([
    ['admin/cashflow/reserve-buckets', { page: 'reserva', tab: 'pots' }],
    ['cashflow/controle-mae', { page: 'controle-mae' }],
    ['cashflow/reserva', { page: 'reserva', tab: 'pots' }],
  ])('[AC P60-F13-consolidated-navigation-and-retirement-04] sends %s to the account that replaces its page', (path, props) => {
    const redirect = REDIRECT_ROUTES.find((route) => route.path === path)

    expect(redirect?.element.type).toBe(MovedPageRedirect)
    expect(redirect?.element.props).toEqual(props)
  })

  it('declares every route relative to the root route, with no leading slash', () => {
    const absolute = ALL_ROUTES.filter((route) => route.path.startsWith('/'))

    expect(absolute.map((route) => route.path)).toEqual([])
  })
})
