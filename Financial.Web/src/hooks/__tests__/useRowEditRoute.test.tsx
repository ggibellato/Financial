import { act, renderHook } from '@testing-library/react'
import type { ReactNode } from 'react'
import { MemoryRouter, useNavigationType, useSearchParams } from 'react-router-dom'
import { describe, expect, it, vi } from 'vitest'
import { useRowEditRoute } from '../useRowEditRoute'

interface Row {
  id: string
}

const ROWS: Row[] = [{ id: 'r1' }, { id: 'r2' }]

function renderRoute(initialEntry: string, open = vi.fn()) {
  const wrapper = ({ children }: { children: ReactNode }) => <MemoryRouter initialEntries={[initialEntry]}>{children}</MemoryRouter>
  const rendered = renderHook(
    () => ({
      route: useRowEditRoute({ rows: ROWS, keyOf: (row: Row) => row.id, isLoaded: true, open }),
      params: useSearchParams(),
      navigationType: useNavigationType(),
    }),
    { wrapper },
  )
  return { ...rendered, open }
}

describe('useRowEditRoute', () => {
  it('opens the row named in the address once and removes it from the address in place', () => {
    const { result, open } = renderRoute('/?edit=r2&month=9')

    expect(open).toHaveBeenCalledTimes(1)
    expect(open).toHaveBeenCalledWith(ROWS[1])
    expect(result.current.params[0].toString()).toBe('month=9')
    expect(result.current.navigationType).toBe('REPLACE')
  })

  it('opens the same row again when the address names it again later', () => {
    const { result, open } = renderRoute('/?edit=r1')

    act(() => result.current.params[1]('edit=r1'))

    expect(open).toHaveBeenCalledTimes(2)
  })

  it('reports a row the address names but the list does not hold', () => {
    const { result, open } = renderRoute('/?edit=missing')

    expect(result.current.route.isUnavailable).toBe(true)
    expect(open).not.toHaveBeenCalled()
  })

  it('opens nothing without an edit parameter', () => {
    const { result, open } = renderRoute('/?month=9')

    expect(result.current.route.isUnavailable).toBe(false)
    expect(open).not.toHaveBeenCalled()
  })
})
