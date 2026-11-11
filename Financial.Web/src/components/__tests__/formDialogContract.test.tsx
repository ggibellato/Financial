import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import type { BrokerDto, PortfolioDto } from '../../api/types'
import AssetFormDialog from '../AssetFormDialog'
import BrokerFormDialog from '../BrokerFormDialog'
import CategoryFormDialog from '../CategoryFormDialog'
import IncomeSourceFormDialog from '../IncomeSourceFormDialog'
import InvestmentAccountFormDialog from '../InvestmentAccountFormDialog'
import PortfolioFormDialog from '../PortfolioFormDialog'
import RecurringBillFormDialog from '../RecurringBillFormDialog'
import TaxRuleFormDialog from '../TaxRuleFormDialog'

const ACTIVE_BROKERS: BrokerDto[] = [
  { name: 'XPI', currency: 'BRL', status: 'Active', portfolioCount: 1, costBasisMethod: 'AverageCost' },
  { name: 'Avenue', currency: 'USD', status: 'Active', portfolioCount: 1, costBasisMethod: 'AverageCost' },
]

const PORTFOLIOS: PortfolioDto[] = [
  { name: 'Default', brokerName: 'XPI', brokerStatus: 'Active', assetCount: 1 },
  { name: 'ISA', brokerName: 'Avenue', brokerStatus: 'Active', assetCount: 0 },
]

interface Handlers {
  onCancel: () => void
  // eslint-disable-next-line @typescript-eslint/no-explicit-any -- each dialog's onSubmit takes different arguments
  onSubmit: (...args: any[]) => Promise<any>
}

interface FormDialogCase {
  name: string
  saveLabel: string
  renderDialog: (handlers: Handlers) => void
  makeInvalid: () => void
  validationMessage: string
  fillValid: () => void
  serverError: string
}

const change = (label: RegExp, value: string) => fireEvent.change(screen.getByLabelText(label), { target: { value } })

const nameDialog = (
  name: string,
  renderDialog: (handlers: Handlers) => void,
  validName: string,
  serverError: string,
  saveLabel: string,
): FormDialogCase => ({
  name,
  saveLabel,
  renderDialog,
  makeInvalid: () => change(/^Name/, '   '),
  validationMessage: 'Name is required.',
  fillValid: () => change(/^Name/, validName),
  serverError,
})

const CASES: FormDialogCase[] = [
  nameDialog(
    'BrokerFormDialog',
    (h) => render(<BrokerFormDialog broker={null} {...h} />),
    'XPI',
    'A broker named "XPI" already exists.',
    'Add broker',
  ),
  nameDialog(
    'CategoryFormDialog',
    (h) => render(<CategoryFormDialog category={null} {...h} />),
    'Mercado',
    'A category named "Mercado" already exists.',
    'Add category',
  ),
  nameDialog(
    'IncomeSourceFormDialog',
    (h) => render(<IncomeSourceFormDialog incomeSource={null} accounts={[]} accountsStatus="ready" {...h} />),
    'Gleison',
    'An income source named "Gleison" already exists.',
    'Add income source',
  ),
  nameDialog(
    'InvestmentAccountFormDialog',
    (h) => render(<InvestmentAccountFormDialog investmentAccount={null} creditCards={[]} accounts={[]} {...h} />),
    'ChaseSave',
    'An investment account named "ChaseSave" already exists.',
    'Add investment account',
  ),
  nameDialog(
    'PortfolioFormDialog',
    (h) => render(<PortfolioFormDialog portfolio={null} activeBrokers={ACTIVE_BROKERS} {...h} />),
    'Default',
    'Broker "XPI" already has a portfolio named "Default".',
    'Add portfolio',
  ),
  {
    ...nameDialog(
      'AssetFormDialog',
      (h) => render(<AssetFormDialog asset={null} activeBrokers={ACTIVE_BROKERS} portfolios={PORTFOLIOS} {...h} />),
      'BCIA11',
      'Portfolio "Default" already has an asset named "BCIA11".',
      'Add asset',
    ),
    makeInvalid: () => {
      change(/^Portfolio/, 'Default')
      change(/^Name/, '   ')
    },
    fillValid: () => {
      change(/^Portfolio/, 'Default')
      change(/^Name/, 'BCIA11')
    },
  },
  {
    name: 'RecurringBillFormDialog',
    saveLabel: 'Add recurring bill',
    renderDialog: (h) => render(<RecurringBillFormDialog recurringBill={null} {...h} />),
    makeInvalid: () => change(/^Due day/, '32'),
    validationMessage: 'Due day must be between 1 and 31.',
    fillValid: () => {
      change(/^Due day/, '10')
      change(/^Description/, 'Rent')
      change(/^Value/, '1500')
    },
    serverError: 'Due day must be between 1 and 31.',
  },
  {
    name: 'TaxRuleFormDialog',
    saveLabel: 'Add tax rule',
    renderDialog: (h) => render(<TaxRuleFormDialog taxRule={null} {...h} />),
    makeInvalid: () => change(/^Effective from/, '2026-01-01'),
    validationMessage: 'Label is required.',
    fillValid: () => {
      change(/^Label/, 'Another rule')
      change(/^Effective from/, '2026-06-01')
    },
    serverError: 'This range overlaps existing rule "BR dividend withholding" (2026-01-01–present).',
  },
]

const handlers = (overrides: Partial<Handlers> = {}): Handlers => ({
  onCancel: vi.fn(),
  onSubmit: vi.fn().mockResolvedValue(undefined),
  ...overrides,
})

describe('form dialog contract: invalid input', () => {
  it.each(CASES)('$name disables the confirm button and shows a validation message', ({ renderDialog, makeInvalid, validationMessage, saveLabel }) => {
    renderDialog(handlers())

    makeInvalid()

    expect(screen.getByText(validationMessage)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: saveLabel })).toBeDisabled()
  })
})

describe('form dialog contract: server rejection', () => {
  it.each(CASES)('$name shows the server error, focuses it and re-enables the confirm button when the submit rejects', async ({ renderDialog, fillValid, serverError, saveLabel }) => {
    renderDialog(handlers({ onSubmit: vi.fn().mockRejectedValue(new Error(serverError)) }))

    fillValid()
    fireEvent.click(screen.getByRole('button', { name: saveLabel }))

    expect(await screen.findByText(serverError)).toBeInTheDocument()
    expect(screen.getByRole('alert')).toHaveTextContent(serverError)
    expect(screen.getByRole('button', { name: saveLabel })).not.toBeDisabled()
    await waitFor(() => expect(screen.getByRole('alert')).toHaveFocus())
  })
})

describe('form dialog contract: keyboard submit', () => {
  it.each(CASES)('$name saves when Enter is pressed in a text field', async ({ renderDialog, fillValid }) => {
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    renderDialog(handlers({ onSubmit }))

    fillValid()
    await userEvent.type(screen.getAllByRole('textbox')[0], '{Enter}')

    await waitFor(() => expect(onSubmit).toHaveBeenCalledTimes(1))
  })
})

describe('form dialog contract: cancel', () => {
  it.each(CASES)('$name calls onCancel when Cancel is clicked', ({ renderDialog }) => {
    const onCancel = vi.fn()
    renderDialog(handlers({ onCancel }))

    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))

    expect(onCancel).toHaveBeenCalled()
  })
})
