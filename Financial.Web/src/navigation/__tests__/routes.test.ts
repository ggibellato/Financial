import { describe, expect, it } from 'vitest'
import { Navigate } from 'react-router-dom'
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
    expect(REDIRECT_ROUTES.map((route) => route.path)).toEqual([
      'admin/cashflow/banks',
      'admin/cashflow/credit-cards',
      'admin/cashflow/reserve-buckets',
      'cashflow/controle-mae',
      'cashflow/reserva',
    ])
    for (const route of REDIRECT_ROUTES) {
      expect(navRoutes).not.toContain(`/${route.path}`)
      expect(declaredRoutes).not.toContain(`/${route.path}`)
    }
  })

  it.each([
    ['admin/cashflow/banks', Navigate, { replace: true, to: '/admin/cashflow/accounts' }],
    ['admin/cashflow/credit-cards', Navigate, { replace: true, to: '/admin/cashflow/accounts' }],
    ['admin/cashflow/reserve-buckets', MovedPageRedirect, { page: 'reserva', tab: 'pots' }],
    ['cashflow/controle-mae', MovedPageRedirect, { page: 'controle-mae' }],
    ['cashflow/reserva', MovedPageRedirect, { page: 'reserva', tab: 'pots' }],
  ])('[AC P60-F13-consolidated-navigation-and-retirement-04] redirects every retired page address: %s', (path, element, props) => {
    const redirect = REDIRECT_ROUTES.find((route) => route.path === path)

    expect(redirect?.element.type).toBe(element)
    expect(redirect?.element.props).toEqual(props)
  })

  it('declares every route relative to the root route, with no leading slash', () => {
    const absolute = ALL_ROUTES.filter((route) => route.path.startsWith('/'))

    expect(absolute.map((route) => route.path)).toEqual([])
  })
})
