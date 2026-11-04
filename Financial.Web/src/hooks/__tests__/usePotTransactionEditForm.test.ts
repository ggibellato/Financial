import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../../api/apiError'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { ReserveMovementDto } from '../../api/types'
import { usePotTransactionEditForm } from '../usePotTransactionEditForm'

const { updateReserveMovementMock } = vi.hoisted(() => ({
  updateReserveMovementMock: vi.fn<FinancialApiClient['updateReserveMovement']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: { updateReserveMovement: updateReserveMovementMock } as Partial<FinancialApiClient>,
}))

const MOVEMENT: ReserveMovementDto = { id: 'm1', bucketId: 'p1', bucketName: 'Trips', amount: -20, date: '2026-10-05', description: 'Boots', incomeId: null }

function renderForm(onSaved = vi.fn()) {
  const view = renderHook(() => usePotTransactionEditForm(onSaved))
  act(() => view.result.current.open(MOVEMENT))
  return view
}

beforeEach(() => {
  updateReserveMovementMock.mockReset().mockResolvedValue(MOVEMENT)
})

describe('usePotTransactionEditForm', () => {
  it('opens clean, filled from the movement', () => {
    const { result } = renderForm()

    expect(result.current.isOpen).toBe(true)
    expect(result.current.isDirty).toBe(false)
    expect(result.current.edit?.values).toEqual({
      editMovementBucketId: 'p1',
      editMovementAmount: '-20',
      editMovementDate: '2026-10-05',
      editMovementDescription: 'Boots',
    })
  })

  it('is dirty only while a value differs from the movement', () => {
    const { result } = renderForm()

    act(() => result.current.setField('editMovementDescription', 'Chemist'))
    expect(result.current.isDirty).toBe(true)

    act(() => result.current.setField('editMovementDescription', 'Boots'))
    expect(result.current.isDirty).toBe(false)
  })

  it('saves the changed values and closes', async () => {
    const onSaved = vi.fn()
    const { result } = renderForm(onSaved)

    act(() => result.current.setField('editMovementAmount', '-21.50'))
    act(() => result.current.submit())

    expect(result.current.edit?.isSaving).toBe(true)
    await waitFor(() => expect(onSaved).toHaveBeenCalledOnce())
    expect(updateReserveMovementMock).toHaveBeenCalledWith('m1', { bucketId: 'p1', amount: -21.5, date: '2026-10-05', description: 'Boots' })
    expect(result.current.isOpen).toBe(false)
  })

  it('names each invalid field and sends nothing', () => {
    const onSaved = vi.fn()
    const { result } = renderForm(onSaved)

    act(() => result.current.setField('editMovementDescription', ' '))
    act(() => result.current.setField('editMovementAmount', ''))
    act(() => result.current.submit())

    expect(result.current.edit?.errorFields).toEqual({ editMovementDescription: 'Description is required', editMovementAmount: 'Amount must be a number' })
    expect(updateReserveMovementMock).not.toHaveBeenCalled()
    expect(onSaved).not.toHaveBeenCalled()
  })

  it('keeps the form and its values with the server message on a failed save', async () => {
    updateReserveMovementMock.mockRejectedValue(new ApiError('Date must not be in the future.', 400))
    const onSaved = vi.fn()
    const { result } = renderForm(onSaved)

    act(() => result.current.setField('editMovementDate', '2030-01-01'))
    act(() => result.current.submit())

    await waitFor(() => expect(result.current.edit?.error).toBe('Date must not be in the future.'))
    expect(result.current.edit?.isSaving).toBe(false)
    expect(result.current.edit?.values.editMovementDate).toBe('2030-01-01')
    expect(onSaved).not.toHaveBeenCalled()
  })

  it('sends one request when submitted twice', async () => {
    const onSaved = vi.fn()
    const { result } = renderForm(onSaved)

    act(() => result.current.submit())
    act(() => result.current.submit())

    await waitFor(() => expect(onSaved).toHaveBeenCalledOnce())
    expect(updateReserveMovementMock).toHaveBeenCalledOnce()
  })

  it('closes without saving on cancel', () => {
    const { result } = renderForm()

    act(() => result.current.cancel())

    expect(result.current.isOpen).toBe(false)
    expect(updateReserveMovementMock).not.toHaveBeenCalled()
  })
})
