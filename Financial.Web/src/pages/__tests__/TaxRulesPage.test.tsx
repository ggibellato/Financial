import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import TaxRulesPage from '../TaxRulesPage'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { TaxRuleDto } from '../../api/types'

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

const RULES: TaxRuleDto[] = [
  {
    id: 'r1',
    jurisdiction: 'BR',
    eventCategory: 'Dividend',
    label: 'BR dividend withholding',
    description: '',
    effectiveFrom: '2026-01-01',
    effectiveTo: null,
  },
  {
    id: 'r2',
    jurisdiction: 'UK',
    eventCategory: 'Interest',
    label: 'UK interest rule',
    description: '',
    effectiveFrom: '2025-04-06',
    effectiveTo: '2026-04-05',
  },
]

describe('TaxRulesPage', () => {
  beforeEach(() => {
    getTaxRulesMock.mockReset()
    createTaxRuleMock.mockReset()
    updateTaxRuleMock.mockReset()
    deleteTaxRuleMock.mockReset()
    getTaxRulesMock.mockResolvedValue(RULES)
  })

  it('renders every tax rule', async () => {
    render(<TaxRulesPage />)

    await waitFor(() => expect(screen.getByText('BR dividend withholding')).toBeInTheDocument())
    expect(screen.getByText('UK interest rule')).toBeInTheDocument()
  })

  it('shows the empty state prompting creation of the first rule', async () => {
    getTaxRulesMock.mockResolvedValue([])
    render(<TaxRulesPage />)

    expect(await screen.findByText('No tax rules configured yet — create one to get started.')).toBeInTheDocument()
  })

  it('shows an error state with retry on load failure', async () => {
    getTaxRulesMock.mockRejectedValue(new Error('Network down'))
    render(<TaxRulesPage />)

    expect(await screen.findByRole('alert')).toHaveTextContent('Network down')
  })

  it('creates a tax rule through the Create Tax Rule dialog', async () => {
    createTaxRuleMock.mockResolvedValue(RULES[0])
    render(<TaxRulesPage />)
    await waitFor(() => expect(screen.getByText('BR dividend withholding')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Create Tax Rule' }))
    fireEvent.change(screen.getByLabelText(/^Label/), { target: { value: 'New rule' } })
    fireEvent.change(screen.getByLabelText(/^Effective From/), { target: { value: '2026-01-01' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() =>
      expect(createTaxRuleMock).toHaveBeenCalledWith({
        jurisdiction: 'BR',
        eventCategory: 'CapitalGain',
        label: 'New rule',
        description: '',
        effectiveFrom: '2026-01-01',
        effectiveTo: null,
      }),
    )
    await waitFor(() => expect(screen.queryByRole('heading', { name: 'Create Tax Rule' })).not.toBeInTheDocument())
  })

  it('edits a tax rule through its row action', async () => {
    updateTaxRuleMock.mockResolvedValue({ ...RULES[0], label: 'Renamed' })
    render(<TaxRulesPage />)
    await waitFor(() => expect(screen.getByText('BR dividend withholding')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Edit BR dividend withholding' }))
    expect(screen.getByRole('heading', { name: 'Edit Tax Rule' })).toBeInTheDocument()
    fireEvent.change(screen.getByLabelText(/^Label/), { target: { value: 'Renamed' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() =>
      expect(updateTaxRuleMock).toHaveBeenCalledWith('r1', {
        label: 'Renamed',
        description: '',
        effectiveFrom: '2026-01-01',
        effectiveTo: null,
      }),
    )
  })

  it('deletes a tax rule after confirmation, with wording stating it is permanently deleted', async () => {
    deleteTaxRuleMock.mockResolvedValue(undefined)
    render(<TaxRulesPage />)
    await waitFor(() => expect(screen.getByText('BR dividend withholding')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Delete BR dividend withholding' }))
    expect(screen.getByText(/will be permanently deleted/)).toBeInTheDocument()
    const confirmButtons = screen.getAllByRole('button', { name: 'Delete' })
    fireEvent.click(confirmButtons[confirmButtons.length - 1])

    await waitFor(() => expect(deleteTaxRuleMock).toHaveBeenCalledWith('r1'))
  })

  it('shows the server rejection inline, naming the affected tax year(s), when a delete is blocked', async () => {
    deleteTaxRuleMock.mockRejectedValue(
      new Error(
        'Cannot delete tax rule "BR dividend withholding" while a final classification still depends on it, for tax year(s): 2026.',
      ),
    )
    render(<TaxRulesPage />)
    await waitFor(() => expect(screen.getByText('BR dividend withholding')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Delete BR dividend withholding' }))
    const confirmButtons = screen.getAllByRole('button', { name: 'Delete' })
    fireEvent.click(confirmButtons[confirmButtons.length - 1])

    expect(
      await screen.findByText(
        'Cannot delete tax rule "BR dividend withholding" while a final classification still depends on it, for tax year(s): 2026.',
      ),
    ).toBeInTheDocument()
    expect(screen.getByText('BR dividend withholding')).toBeInTheDocument()
  })
})
