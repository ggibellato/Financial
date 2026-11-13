import { act, renderHook } from '@testing-library/react'
import type { ReactNode } from 'react'
import { MemoryRouter, useLocation, useNavigationType } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { pinDate } from '../../test-utils/pinDate'
import { useOverviewAddress } from '../useOverviewAddress'

const AC_01 = '[AC P61-F01-accounts-overview-tabs-and-monthly-fold-01]'
const TABS = ['accounts', 'summary', 'transactions'] as const

function renderAt(entry: string) {
  const wrapper = ({ children }: { children: ReactNode }) => <MemoryRouter initialEntries={[entry]}>{children}</MemoryRouter>
  return renderHook(() => ({ address: useOverviewAddress(TABS), location: useLocation(), navigationType: useNavigationType() }), { wrapper })
}

describe('useOverviewAddress', () => {
  beforeEach(() => {
    pinDate('2026-10-15T12:00:00+01:00')
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it(`${AC_01} uses the current month when the address has none`, () => {
    const { result } = renderAt('/cashflow/accounts')

    expect(result.current.address).toMatchObject({ year: 2026, month: 10, monthInputValue: '2026-10' })
    expect(result.current.location.search).toBe('')
  })

  it(`${AC_01} falls back to the current month for a malformed month and leaves the address as typed`, () => {
    const { result } = renderAt('/cashflow/accounts?month=2026-13')

    expect(result.current.address).toMatchObject({ year: 2026, month: 10, monthInputValue: '2026-10' })
    expect(result.current.location.search).toBe('?month=2026-13')
  })

  it(`${AC_01} reads the month from the address`, () => {
    const { result } = renderAt('/cashflow/accounts?month=2026-09')

    expect(result.current.address).toMatchObject({ year: 2026, month: 9, monthInputValue: '2026-09' })
  })

  it(`${AC_01} writes a new month to the address, keeping the other parameters and replacing the history entry`, () => {
    const { result } = renderAt('/cashflow/accounts?tab=summary&month=2026-09')

    act(() => result.current.address.setMonthInputValue('2026-07'))

    expect(result.current.address).toMatchObject({ year: 2026, month: 7 })
    const params = new URLSearchParams(result.current.location.search)
    expect(params.get('month')).toBe('2026-07')
    expect(params.get('tab')).toBe('summary')
    expect(result.current.navigationType).toBe('REPLACE')
  })

  it('ignores an empty or malformed month from the picker', () => {
    const { result } = renderAt('/cashflow/accounts?month=2026-09')

    act(() => result.current.address.setMonthInputValue(''))
    act(() => result.current.address.setMonthInputValue('2026-1'))

    expect(result.current.address.monthInputValue).toBe('2026-09')
    expect(result.current.location.search).toBe('?month=2026-09')
    expect(result.current.navigationType).toBe('POP')
  })

  it(`${AC_01} opens the first tab when the address has none`, () => {
    const { result } = renderAt('/cashflow/accounts?month=2026-09')

    expect(result.current.address.tab).toBe('accounts')
  })

  it(`${AC_01} falls back to the first tab for an unknown tab`, () => {
    const { result } = renderAt('/cashflow/accounts?tab=monthly')

    expect(result.current.address.tab).toBe('accounts')
    expect(result.current.location.search).toBe('?tab=monthly')
  })

  it(`${AC_01} reads the tab from the address`, () => {
    const { result } = renderAt('/cashflow/accounts?tab=transactions')

    expect(result.current.address.tab).toBe('transactions')
  })

  it(`${AC_01} writes a new tab to the address, keeping the month and replacing the history entry`, () => {
    const { result } = renderAt('/cashflow/accounts?month=2026-09')

    act(() => result.current.address.setTab('summary'))

    expect(result.current.address).toMatchObject({ tab: 'summary', year: 2026, month: 9 })
    const params = new URLSearchParams(result.current.location.search)
    expect(params.get('tab')).toBe('summary')
    expect(params.get('month')).toBe('2026-09')
    expect(result.current.navigationType).toBe('REPLACE')
  })

  it("drops a tab's own parameters, such as All transactions' filters, on a month or tab change", () => {
    const { result } = renderAt('/cashflow/accounts?month=2026-09&tab=transactions&category=Groceries&kind=')

    act(() => result.current.address.setMonthInputValue('2026-08'))
    expect(result.current.location.search).toBe('?month=2026-08&tab=transactions')

    act(() => result.current.address.setTab('summary'))
    expect(result.current.location.search).toBe('?month=2026-08&tab=summary')
  })
})
