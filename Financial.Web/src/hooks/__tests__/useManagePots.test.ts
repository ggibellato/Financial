import { act, renderHook } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { PotBalanceDto } from '../../api/types'
import { useManagePots } from '../useManagePots'

const { updatePotsMock } = vi.hoisted(() => ({
  updatePotsMock: vi.fn<FinancialApiClient['updatePots']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: { updatePots: updatePotsMock } as Partial<FinancialApiClient>,
}))

const POTS: PotBalanceDto[] = [
  { id: 'pot-a', name: 'Ariana', splitPercentage: 60, isActive: true, balance: 10 },
  { id: 'pot-b', name: 'Casa', splitPercentage: 40, isActive: true, balance: 5 },
]

function renderManagePots(onSaved = vi.fn()) {
  return renderHook(() => useManagePots('reserva', POTS, onSaved))
}

describe('useManagePots', () => {
  beforeEach(() => {
    updatePotsMock.mockReset()
  })

  it('treats a name that only gains surrounding spaces as unchanged', () => {
    const { result } = renderManagePots()

    act(() => result.current.changeRow(result.current.rows[0].key, { name: '  Ariana  ' }))

    expect(result.current.isDirty).toBe(false)
  })

  it('treats switching a pot off as a change', () => {
    const { result } = renderManagePots()

    act(() => result.current.changeRow(result.current.rows[1].key, { isActive: false }))

    expect(result.current.isDirty).toBe(true)
  })

  it('does not save while a pot has a field error', () => {
    const { result } = renderManagePots()
    act(() => result.current.changeRow(result.current.rows[0].key, { name: '' }))

    act(() => result.current.save())

    expect(updatePotsMock).not.toHaveBeenCalled()
  })

  it('does not save while a split percentage is out of range', () => {
    const { result } = renderManagePots()
    act(() => result.current.changeRow(result.current.rows[0].key, { splitPercentage: '160' }))

    act(() => result.current.save())

    expect(updatePotsMock).not.toHaveBeenCalled()
  })

  it('removes only the new pot asked for', () => {
    const { result } = renderManagePots()
    act(() => result.current.addPot())
    act(() => result.current.addPot())
    const [ariana, casa, first, second] = result.current.rows

    act(() => result.current.removeNewPot(first.key))

    expect(result.current.rows.map((row) => row.key)).toEqual([ariana.key, casa.key, second.key])
  })

  it('shows the save failure message when the update is rejected', async () => {
    updatePotsMock.mockRejectedValue('unexpected')
    const { result } = renderManagePots()
    act(() => result.current.changeRow(result.current.rows[0].key, { name: 'Ariana R' }))

    await act(async () => result.current.save())

    expect(result.current.error).toBe('The pots could not be saved.')
  })
})
