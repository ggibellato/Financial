import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { TaxRuleDto } from '../../api/types'
import { useTaxRules } from '../useTaxRules'

const { getTaxRulesMock, createTaxRuleMock, updateTaxRuleMock, deleteTaxRuleMock } = vi.hoisted(() => ({
  getTaxRulesMock: vi.fn<FinancialApiClient['getTaxRules']>(),
  createTaxRuleMock: vi.fn<FinancialApiClient['createTaxRule']>(),
  updateTaxRuleMock: vi.fn<FinancialApiClient['updateTaxRule']>(),
  deleteTaxRuleMock: vi.fn<FinancialApiClient['deleteTaxRule']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getTaxRules: getTaxRulesMock,
    createTaxRule: createTaxRuleMock,
    updateTaxRule: updateTaxRuleMock,
    deleteTaxRule: deleteTaxRuleMock,
  } as Partial<FinancialApiClient>,
}))

const RULE: TaxRuleDto = {
  id: 'rule-1',
  jurisdiction: 'BR',
  eventCategory: 'Dividend',
  label: 'BR dividend withholding',
  description: '',
  effectiveFrom: '2026-01-01',
  effectiveTo: null,
}

describe('useTaxRules', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('loads tax rules on mount', async () => {
    getTaxRulesMock.mockResolvedValue([RULE])

    const { result } = renderHook(() => useTaxRules())

    expect(result.current.isLoading).toBe(true)
    await waitFor(() => expect(result.current.isLoading).toBe(false))
    expect(result.current.taxRules).toEqual([RULE])
  })

  it('surfaces a load error and retries on demand', async () => {
    getTaxRulesMock.mockRejectedValueOnce(new Error('boom'))
    getTaxRulesMock.mockResolvedValueOnce([RULE])

    const { result } = renderHook(() => useTaxRules())

    await waitFor(() => expect(result.current.error).toBe('boom'))

    result.current.retry()

    await waitFor(() => expect(result.current.error).toBeNull())
    expect(result.current.taxRules).toEqual([RULE])
  })

  it('create and update refresh the list', async () => {
    getTaxRulesMock.mockResolvedValue([RULE])
    createTaxRuleMock.mockResolvedValue(RULE)
    updateTaxRuleMock.mockResolvedValue(RULE)

    const { result } = renderHook(() => useTaxRules())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    await act(async () => {
      await result.current.createTaxRule({
        jurisdiction: 'BR',
        eventCategory: 'Dividend',
        label: 'x',
        description: '',
        effectiveFrom: '2026-01-01',
        effectiveTo: null,
      })
    })
    expect(getTaxRulesMock).toHaveBeenCalledTimes(2)

    await act(async () => {
      await result.current.updateTaxRule('rule-1', {
        label: 'y',
        description: '',
        effectiveFrom: '2026-01-01',
        effectiveTo: null,
      })
    })
    expect(getTaxRulesMock).toHaveBeenCalledTimes(3)
  })

  it('a successful delete removes the rule from the list', async () => {
    getTaxRulesMock.mockResolvedValueOnce([RULE]).mockResolvedValueOnce([])
    deleteTaxRuleMock.mockResolvedValue(undefined)

    const { result } = renderHook(() => useTaxRules())
    await waitFor(() => expect(result.current.taxRules).toEqual([RULE]))

    result.current.deleteTaxRule(RULE)

    await waitFor(() => expect(result.current.taxRules).toEqual([]))
    expect(result.current.deleteError).toBeNull()
  })

  it('a rejected delete (still backing a final classification) surfaces the server message without removing the row', async () => {
    getTaxRulesMock.mockResolvedValue([RULE])
    deleteTaxRuleMock.mockRejectedValue(
      new Error('Cannot delete tax rule "BR dividend withholding" while a final classification still depends on it, for tax year(s): 2026.'),
    )

    const { result } = renderHook(() => useTaxRules())
    await waitFor(() => expect(result.current.taxRules).toEqual([RULE]))

    result.current.deleteTaxRule(RULE)

    await waitFor(() =>
      expect(result.current.deleteError).toBe(
        'Cannot delete tax rule "BR dividend withholding" while a final classification still depends on it, for tax year(s): 2026.',
      ),
    )
    expect(result.current.taxRules).toEqual([RULE])
  })
})
