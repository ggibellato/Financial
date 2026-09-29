import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import ReservaPage from '../ReservaPage'
import { ApiError } from '../../api/apiError'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { BankDto, CategoryDto, ReserveBucketBalanceDto, ReserveBucketDto, ReserveMovementDto } from '../../api/types'

const {
  getReserveBalancesMock,
  getReserveMovementsMock,
  getReserveBucketsMock,
  getBanksMock,
  getCategoriesMock,
  postIncomeSplitMock,
  postWithdrawalMock,
  updateReserveMovementMock,
  deleteReserveMovementMock,
} = vi.hoisted(() => ({
  getReserveBalancesMock: vi.fn<FinancialApiClient['getReserveBalances']>(),
  getReserveMovementsMock: vi.fn<FinancialApiClient['getReserveMovements']>(),
  getReserveBucketsMock: vi.fn<FinancialApiClient['getReserveBuckets']>(),
  getBanksMock: vi.fn<FinancialApiClient['getBanks']>(),
  getCategoriesMock: vi.fn<FinancialApiClient['getCategories']>(),
  postIncomeSplitMock: vi.fn<FinancialApiClient['postIncomeSplit']>(),
  postWithdrawalMock: vi.fn<FinancialApiClient['postWithdrawal']>(),
  updateReserveMovementMock: vi.fn<FinancialApiClient['updateReserveMovement']>(),
  deleteReserveMovementMock: vi.fn<FinancialApiClient['deleteReserveMovement']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getReserveBalances: getReserveBalancesMock,
    getReserveMovements: getReserveMovementsMock,
    getReserveBuckets: getReserveBucketsMock,
    getBanks: getBanksMock,
    getCategories: getCategoriesMock,
    postIncomeSplit: postIncomeSplitMock,
    postWithdrawal: postWithdrawalMock,
    updateReserveMovement: updateReserveMovementMock,
    deleteReserveMovement: deleteReserveMovementMock,
  } as Partial<FinancialApiClient>,
}))

const BALANCES: ReserveBucketBalanceDto[] = [
  { bucketId: 'b1', bucketName: 'Investimento', balance: 654.33 },
  { bucketId: 'b2', bucketName: 'HouseTreats', balance: 654.33 },
  { bucketId: 'b3', bucketName: 'Ariana', balance: 327.17 },
  { bucketId: 'b4', bucketName: 'Gleison', balance: 327.17 },
]

const MOVEMENTS: ReserveMovementDto[] = [
  { id: 'm1', bucketId: 'b1', bucketName: 'Investimento', amount: 654.33, date: '2026-07-17', description: 'Ramsay', incomeId: null },
  { id: 'm2', bucketId: 'b2', bucketName: 'HouseTreats', amount: 654.33, date: '2026-07-17', description: 'Ramsay', incomeId: null },
  { id: 'm3', bucketId: 'b3', bucketName: 'Ariana', amount: 327.17, date: '2026-07-17', description: 'Ramsay', incomeId: null },
  { id: 'm4', bucketId: 'b4', bucketName: 'Gleison', amount: 327.17, date: '2026-07-17', description: 'Ramsay', incomeId: null },
]

const BUCKETS: ReserveBucketDto[] = [
  { id: 'b1', name: 'Investimento', isActive: true, splitPercentage: 33.33, warning: null },
  { id: 'b2', name: 'HouseTreats', isActive: true, splitPercentage: 33.33, warning: null },
  { id: 'b3', name: 'Ariana', isActive: true, splitPercentage: 16.67, warning: null },
  { id: 'b4', name: 'Gleison', isActive: true, splitPercentage: 16.67, warning: null },
]

const BANKS: BankDto[] = [
  { id: 'bk1', name: 'Chase', roundUpEnabled: true, openingBalance: 0, openingBalanceDate: '2026-01-01', hasReferences: false },
  { id: 'bk2', name: 'Barclays', roundUpEnabled: false, openingBalance: 0, openingBalanceDate: '2026-01-01', hasReferences: false },
]

const CATEGORIES: CategoryDto[] = [
  { id: 'c1', name: 'Ariana', active: true, isInvestment: false, isTithe: false, hasReferences: false },
  { id: 'c2', name: 'Saude', active: true, isInvestment: false, isTithe: false, hasReferences: false },
  { id: 'c3', name: 'Investimento', active: true, isInvestment: true, isTithe: false, hasReferences: false },
  { id: 'c4', name: 'Reserva', active: true, isInvestment: false, isTithe: false, hasReferences: false },
]

describe('ReservaPage', () => {
  beforeEach(() => {
    getReserveBalancesMock.mockReset()
    getReserveMovementsMock.mockReset()
    getReserveBucketsMock.mockReset()
    getBanksMock.mockReset()
    getCategoriesMock.mockReset()
    postIncomeSplitMock.mockReset()
    postWithdrawalMock.mockReset()
    updateReserveMovementMock.mockReset()
    deleteReserveMovementMock.mockReset()
    getReserveBalancesMock.mockResolvedValue(BALANCES)
    getReserveMovementsMock.mockResolvedValue(MOVEMENTS)
    getReserveBucketsMock.mockResolvedValue(BUCKETS)
    getBanksMock.mockResolvedValue(BANKS)
    getCategoriesMock.mockResolvedValue(CATEGORIES)
    sessionStorage.clear()
  })

  it('shows a loading state before data arrives', () => {
    render(<ReservaPage />)

    expect(screen.getByText('Loading...')).toBeInTheDocument()
  })

  it('shows an error state with retry when the fetch fails', async () => {
    getReserveBalancesMock.mockRejectedValue(new Error('Network down'))

    render(<ReservaPage />)

    await waitFor(() => expect(screen.getByRole('alert')).toBeInTheDocument())
    expect(screen.getByText('Network down')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Try again' })).toBeInTheDocument()
  })

  it('renders all 4 bucket balances and the movement history once loaded', async () => {
    render(<ReservaPage />)

    await waitFor(() => expect(screen.getAllByText('Ramsay').length).toBe(4))
    for (const b of BALANCES) {
      expect(screen.getAllByText(b.bucketName).length).toBeGreaterThan(0)
    }
  })

  it('shows a group total after the last movement of a same date+description split in history', async () => {
    render(<ReservaPage />)

    await waitFor(() => expect(screen.getAllByText('Ramsay').length).toBe(4))
    // "Date" is the movements table's own column header, distinguishing it from the balances table.
    const movementsTable = screen.getByRole('columnheader', { name: 'Date' }).closest('table') as HTMLElement
    const rows = within(movementsTable).getAllByRole('row')
    // header + 4 movement rows + 1 group-total row
    expect(rows).toHaveLength(6)
    expect(within(rows[5]).getByText('Total split for Ramsay')).toBeInTheDocument()
    expect(within(rows[5]).getByText('1,963.00')).toBeInTheDocument()
  })

  it('renders the total balance across all buckets, bold and always visible', async () => {
    render(<ReservaPage />)

    await waitFor(() => expect(screen.getAllByText('Ramsay').length).toBe(4))
    // "Balance" is the balances table's own column header, distinguishing its section from movements.
    const balancesSection = screen.getByRole('columnheader', { name: 'Balance' }).closest('section') as HTMLElement
    // 654.33 + 654.33 + 327.17 + 327.17 = 1963.00
    expect(within(balancesSection).getByText('1,963.00')).toBeInTheDocument()
  })

  it('shows the income-split form only after New Income Split is clicked', async () => {
    render(<ReservaPage />)

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Income Split' })).toBeInTheDocument())
    expect(screen.queryByText('New Income Split', { selector: 'h2' })).not.toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'New Income Split' }))

    expect(screen.getByText('New Income Split', { selector: 'h2' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Amount to Split/)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Add Income Split' })).toBeInTheDocument()
  })

  it('shows a field-level validation error on Description when Description is missing on Income Split', async () => {
    render(<ReservaPage />)

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Income Split' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'New Income Split' }))
    fireEvent.change(screen.getByLabelText(/^Date/), { target: { value: '2026-07-01' } })
    fireEvent.change(screen.getByLabelText(/^Amount to Split/), { target: { value: '1963' } })
    fireEvent.click(screen.getByRole('button', { name: 'Add Income Split' }))

    expect(await screen.findByText('Description is required')).toBeInTheDocument()
    expect(postIncomeSplitMock).not.toHaveBeenCalled()
  })

  it('shows the posted split breakdown and total after a successful submission', async () => {
    postIncomeSplitMock.mockResolvedValue({
      buckets: [
        { bucketId: 'b1', bucketName: 'Investimento', amount: 654.33 },
        { bucketId: 'b2', bucketName: 'HouseTreats', amount: 654.33 },
        { bucketId: 'b3', bucketName: 'Ariana', amount: 327.17 },
        { bucketId: 'b4', bucketName: 'Gleison', amount: 327.17 },
      ],
      total: 1963,
    })
    render(<ReservaPage />)

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Income Split' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'New Income Split' }))
    fireEvent.change(screen.getByLabelText(/^Date/), { target: { value: '2026-07-01' } })
    fireEvent.change(screen.getByLabelText(/^Amount to Split/), { target: { value: '1963' } })
    fireEvent.change(screen.getByLabelText(/^Description/), { target: { value: 'Ramsay' } })
    fireEvent.click(screen.getByRole('button', { name: 'Add Income Split' }))

    await waitFor(() => expect(screen.getByText('Income Split Posted')).toBeInTheDocument())
    const resultPanel = screen.getByTestId('income-split-form-panel')
    expect(within(resultPanel).getAllByText('654.33')).toHaveLength(2)
    expect(within(resultPanel).getByText('1,963.00')).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'Dismiss' }))
    expect(screen.queryByText('Income Split Posted')).not.toBeInTheDocument()
  })

  it('shows the withdrawal form only after New Withdrawal is clicked', async () => {
    render(<ReservaPage />)

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Withdrawal' })).toBeInTheDocument())
    expect(screen.queryByText('New Withdrawal', { selector: 'h2' })).not.toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'New Withdrawal' }))

    expect(screen.getByText('New Withdrawal', { selector: 'h2' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Bucket/)).toBeInTheDocument()
    expect(screen.getByLabelText(/^Amount/)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Add Withdrawal' })).toBeInTheDocument()
  })

  it('shows a field-level validation error on Amount when Amount is missing on Withdrawal', async () => {
    render(<ReservaPage />)

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Withdrawal' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'New Withdrawal' }))
    fireEvent.change(screen.getByLabelText(/^Date/), { target: { value: '2026-07-01' } })
    fireEvent.change(screen.getByLabelText(/^Description/), { target: { value: 'Big purchase' } })
    fireEvent.click(screen.getByRole('button', { name: 'Add Withdrawal' }))

    expect(await screen.findByText('Amount must be a positive number')).toBeInTheDocument()
    expect(postWithdrawalMock).not.toHaveBeenCalled()
  })

  it('shows a field-level validation error on Description when Description is cleared on Edit Movement', async () => {
    render(<ReservaPage />)

    await waitFor(() => expect(screen.getAllByText('Ramsay').length).toBe(4))
    fireEvent.click(screen.getAllByRole('button', { name: 'Edit movement' })[0])
    fireEvent.change(screen.getByLabelText(/^Description/), { target: { value: '' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    expect(await screen.findByText('Description is required')).toBeInTheDocument()
    expect(updateReserveMovementMock).not.toHaveBeenCalled()
  })

  it('edits a movement via the toggled panel and saves, updating the displayed row', async () => {
    updateReserveMovementMock.mockResolvedValue({ ...MOVEMENTS[0], amount: 700 })
    render(<ReservaPage />)

    await waitFor(() => expect(screen.getAllByText('Ramsay').length).toBe(4))

    fireEvent.click(screen.getAllByRole('button', { name: 'Edit movement' })[0])
    expect(screen.getByText('Edit Movement')).toBeInTheDocument()
    const amountInput = screen.getByDisplayValue('654.33')
    fireEvent.change(amountInput, { target: { value: '700' } })

    getReserveMovementsMock.mockResolvedValue([{ ...MOVEMENTS[0], amount: 700 }, ...MOVEMENTS.slice(1)])
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() =>
      expect(updateReserveMovementMock).toHaveBeenCalledWith('m1', {
        bucketId: 'b1',
        amount: 700,
        date: '2026-07-17',
        description: 'Ramsay',
      }),
    )
    await waitFor(() => expect(screen.getByText('700.00')).toBeInTheDocument())
  })

  describe('withdrawal through a bank', () => {
    async function openWithdrawalForm() {
      render(<ReservaPage />)
      await waitFor(() => expect(screen.getByRole('button', { name: 'New Withdrawal' })).toBeInTheDocument())
      fireEvent.click(screen.getByRole('button', { name: 'New Withdrawal' }))
    }

    function fillRequiredFields() {
      fireEvent.change(screen.getByLabelText(/^Amount/), { target: { value: '30' } })
      fireEvent.change(screen.getByLabelText(/^Date/), { target: { value: '2026-07-01' } })
      fireEvent.change(screen.getByLabelText(/^Description/), { target: { value: 'Dentist' } })
    }

    const submit = () => fireEvent.click(screen.getByRole('button', { name: 'Add Withdrawal' }))

    it('offers an optional Through bank select listing every bank, empty by default', async () => {
      await openWithdrawalForm()

      const bankSelect = screen.getByRole('combobox', { name: /^Through bank/ }) as HTMLSelectElement
      expect(bankSelect.value).toBe('')
      expect(within(bankSelect).getAllByRole('option').map((o) => o.textContent)).toEqual([
        'No bank (direct)',
        'Chase',
        'Barclays',
      ])
    })

    it('offers contextual help explaining Through bank', async () => {
      await openWithdrawalForm()

      expect(screen.getByRole('button', { name: /Through bank information/ })).toBeInTheDocument()
    })

    it('shows no category field until a bank is selected', async () => {
      await openWithdrawalForm()
      expect(screen.queryByLabelText(/^Expense category/)).not.toBeInTheDocument()

      fireEvent.change(screen.getByRole('combobox', { name: /^Through bank/ }), { target: { value: 'bk1' } })

      expect(screen.getByLabelText(/^Expense category/)).toBeInTheDocument()
    })

    it('lists only eligible categories and preselects the one named after the bucket', async () => {
      await openWithdrawalForm()
      fireEvent.change(screen.getByLabelText(/^Bucket/), { target: { value: 'b3' } })

      fireEvent.change(screen.getByRole('combobox', { name: /^Through bank/ }), { target: { value: 'bk1' } })

      const categorySelect = screen.getByLabelText(/^Expense category/) as HTMLSelectElement
      expect(categorySelect.value).toBe('c1')
      expect(within(categorySelect).getAllByRole('option').map((o) => o.textContent)).toEqual([
        'Select a category',
        'Ariana',
        'Saude',
      ])
    })

    it('shows the required message under the category when none matches and none is chosen', async () => {
      await openWithdrawalForm()
      fillRequiredFields()
      fireEvent.change(screen.getByRole('combobox', { name: /^Through bank/ }), { target: { value: 'bk1' } })

      submit()

      expect(await screen.findByText('Category is required when a bank is selected.')).toBeInTheDocument()
      expect(postWithdrawalMock).not.toHaveBeenCalled()
    })

    it('hides the category and posts null ids after the bank is cleared', async () => {
      postWithdrawalMock.mockResolvedValue({ ...MOVEMENTS[0], id: 'm9', amount: -30 })
      await openWithdrawalForm()
      fillRequiredFields()
      fireEvent.change(screen.getByRole('combobox', { name: /^Through bank/ }), { target: { value: 'bk1' } })
      fireEvent.change(screen.getByRole('combobox', { name: /^Through bank/ }), { target: { value: '' } })

      expect(screen.queryByLabelText(/^Expense category/)).not.toBeInTheDocument()
      submit()

      await waitFor(() =>
        expect(postWithdrawalMock).toHaveBeenCalledWith(
          expect.objectContaining({ paymentSourceBankId: null, expenseCategoryId: null }),
        ),
      )
    })

    it('keeps a manually chosen category when the bucket changes', async () => {
      await openWithdrawalForm()
      fireEvent.change(screen.getByLabelText(/^Bucket/), { target: { value: 'b3' } })
      fireEvent.change(screen.getByRole('combobox', { name: /^Through bank/ }), { target: { value: 'bk1' } })
      fireEvent.change(screen.getByLabelText(/^Expense category/), { target: { value: 'c2' } })

      fireEvent.change(screen.getByLabelText(/^Bucket/), { target: { value: 'b4' } })

      expect((screen.getByLabelText(/^Expense category/) as HTMLSelectElement).value).toBe('c2')
    })

    it('posts both ids, closes the form and refreshes the reserve data on success', async () => {
      postWithdrawalMock.mockResolvedValue({ ...MOVEMENTS[0], id: 'm9', amount: -30 })
      await openWithdrawalForm()
      fillRequiredFields()
      fireEvent.change(screen.getByRole('combobox', { name: /^Through bank/ }), { target: { value: 'bk2' } })
      fireEvent.change(screen.getByLabelText(/^Expense category/), { target: { value: 'c2' } })

      submit()

      await waitFor(() =>
        expect(postWithdrawalMock).toHaveBeenCalledWith(
          expect.objectContaining({ paymentSourceBankId: 'bk2', expenseCategoryId: 'c2', confirmed: false }),
        ),
      )
      await waitFor(() => expect(screen.queryByText('New Withdrawal', { selector: 'h2' })).not.toBeInTheDocument())
      expect(getReserveBalancesMock).toHaveBeenCalledTimes(2)
    })

    it('shows a server rejection as the general error and keeps the entered values', async () => {
      postWithdrawalMock.mockRejectedValue(new ApiError('Category is inactive.', 400))
      await openWithdrawalForm()
      fillRequiredFields()
      fireEvent.change(screen.getByRole('combobox', { name: /^Through bank/ }), { target: { value: 'bk1' } })
      fireEvent.change(screen.getByLabelText(/^Expense category/), { target: { value: 'c2' } })

      submit()

      expect(await screen.findByText('Category is inactive.')).toBeInTheDocument()
      expect((screen.getByLabelText(/^Description/) as HTMLInputElement).value).toBe('Dentist')
      expect((screen.getByRole('combobox', { name: /^Through bank/ }) as HTMLSelectElement).value).toBe('bk1')
      expect((screen.getByLabelText(/^Expense category/) as HTMLSelectElement).value).toBe('c2')
    })

    it('disables every withdrawal control while saving', async () => {
      postWithdrawalMock.mockReturnValue(new Promise(() => {}))
      await openWithdrawalForm()
      fillRequiredFields()
      fireEvent.change(screen.getByRole('combobox', { name: /^Through bank/ }), { target: { value: 'bk1' } })
      fireEvent.change(screen.getByLabelText(/^Expense category/), { target: { value: 'c2' } })

      submit()

      await waitFor(() => expect(screen.getByRole('button', { name: 'Saving...' })).toBeDisabled())
      for (const label of [/^Date/, /^Bucket/, /^Expense category/, /^Description/, /^Amount/]) {
        expect(screen.getByLabelText(label)).toBeDisabled()
      }
      expect(screen.getByRole('combobox', { name: /^Through bank/ })).toBeDisabled()
      expect(screen.getByRole('button', { name: 'Cancel' })).toBeDisabled()
    })

    it('orders the fields Date, Bucket, Through bank, Expense category, Description, Amount', async () => {
      await openWithdrawalForm()
      fireEvent.change(screen.getByRole('combobox', { name: /^Through bank/ }), { target: { value: 'bk1' } })

      const controls = [
        screen.getByLabelText(/^Date/),
        screen.getByLabelText(/^Bucket/),
        screen.getByRole('combobox', { name: /^Through bank/ }),
        screen.getByLabelText(/^Expense category/),
        screen.getByLabelText(/^Description/),
        screen.getByLabelText(/^Amount/),
      ]

      for (let i = 0; i < controls.length - 1; i += 1) {
        expect(controls[i].compareDocumentPosition(controls[i + 1]) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()
      }
    })
  })

  // useReserva no longer prompts; it asks its caller. These two prove the page is the caller
  // that answers, and that the prompt text is assembled here.
  it('prompts with the server reason when a withdrawal is rejected as an overdraw, and resubmits on accept', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true)
    postWithdrawalMock
      .mockRejectedValueOnce(new ApiError('This withdrawal exceeds the balance.', 409))
      .mockResolvedValueOnce({ ...MOVEMENTS[0], id: 'm9', amount: -100, description: 'Big purchase' })
    render(<ReservaPage />)

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Withdrawal' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'New Withdrawal' }))
    fireEvent.change(screen.getByLabelText(/^Bucket/), { target: { value: 'b1' } })
    fireEvent.change(screen.getByLabelText(/^Amount/), { target: { value: '100' } })
    fireEvent.change(screen.getByLabelText(/^Date/), { target: { value: '2026-07-01' } })
    fireEvent.change(screen.getByLabelText(/^Description/), { target: { value: 'Big purchase' } })
    fireEvent.click(screen.getByRole('button', { name: 'Add Withdrawal' }))

    await waitFor(() =>
      expect(window.confirm).toHaveBeenCalledWith(
        expect.stringContaining('This withdrawal exceeds the balance.'),
      ),
    )
    expect(window.confirm).toHaveBeenCalledWith(expect.stringContaining('Proceed anyway?'))
    await waitFor(() => expect(postWithdrawalMock).toHaveBeenCalledTimes(2))
    expect(postWithdrawalMock).toHaveBeenNthCalledWith(2, expect.objectContaining({ confirmed: true }))
  })

  it('does not resubmit an overdrawing withdrawal when the prompt is declined', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(false)
    postWithdrawalMock.mockRejectedValue(new ApiError('This withdrawal exceeds the balance.', 409))
    render(<ReservaPage />)

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Withdrawal' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'New Withdrawal' }))
    fireEvent.change(screen.getByLabelText(/^Bucket/), { target: { value: 'b1' } })
    fireEvent.change(screen.getByLabelText(/^Amount/), { target: { value: '100' } })
    fireEvent.change(screen.getByLabelText(/^Date/), { target: { value: '2026-07-01' } })
    fireEvent.change(screen.getByLabelText(/^Description/), { target: { value: 'Big purchase' } })
    fireEvent.click(screen.getByRole('button', { name: 'Add Withdrawal' }))

    await waitFor(() => expect(window.confirm).toHaveBeenCalled())
    expect(postWithdrawalMock).toHaveBeenCalledTimes(1)
  })

  it('warns that deleting a split movement removes all 4 lines, and deletes on confirm', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true)
    deleteReserveMovementMock.mockResolvedValue(undefined)
    render(<ReservaPage />)

    await waitFor(() => expect(screen.getAllByText('Ramsay').length).toBe(4))

    getReserveMovementsMock.mockResolvedValue([])
    fireEvent.click(screen.getAllByRole('button', { name: 'Delete movement' })[0])

    expect(window.confirm).toHaveBeenCalledWith(
      expect.stringContaining('part of a split and will delete all 4 lines'),
    )
    await waitFor(() => expect(deleteReserveMovementMock).toHaveBeenCalledWith('m1'))
    await waitFor(() => expect(screen.queryAllByText('Ramsay').length).toBe(0))
  })

  it('does not delete a movement when the confirmation prompt is declined', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(false)
    render(<ReservaPage />)

    await waitFor(() => expect(screen.getAllByText('Ramsay').length).toBe(4))

    fireEvent.click(screen.getAllByRole('button', { name: 'Delete movement' })[0])

    expect(deleteReserveMovementMock).not.toHaveBeenCalled()
  })

  it('lists every fetched bucket in the withdrawal and edit-movement dropdowns, including inactive ones', async () => {
    getReserveBucketsMock.mockResolvedValue([
      ...BUCKETS,
      { id: 'b5', name: 'Retired', isActive: false, splitPercentage: 0, warning: null },
    ])
    render(<ReservaPage />)

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Withdrawal' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'New Withdrawal' }))

    const withdrawalOptions = within(screen.getByLabelText(/^Bucket/)).getAllByRole('option')
    expect(withdrawalOptions.map((o) => o.textContent)).toEqual([
      'Investimento',
      'HouseTreats',
      'Ariana',
      'Gleison',
      'Retired',
    ])
    expect(withdrawalOptions.map((o) => (o as HTMLOptionElement).value)).toEqual([
      'b1',
      'b2',
      'b3',
      'b4',
      'b5',
    ])
  })

  it('renders a split-result row for every entry returned by the income-split response', async () => {
    postIncomeSplitMock.mockResolvedValue({
      buckets: [
        { bucketId: 'b1', bucketName: 'Investimento', amount: 981.5 },
        { bucketId: 'b2', bucketName: 'HouseTreats', amount: 981.5 },
      ],
      total: 1963,
    })
    render(<ReservaPage />)

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Income Split' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'New Income Split' }))
    fireEvent.change(screen.getByLabelText(/^Date/), { target: { value: '2026-07-01' } })
    fireEvent.change(screen.getByLabelText(/^Amount to Split/), { target: { value: '1963' } })
    fireEvent.change(screen.getByLabelText(/^Description/), { target: { value: 'Ramsay' } })
    fireEvent.click(screen.getByRole('button', { name: 'Add Income Split' }))

    await waitFor(() => expect(screen.getByText('Income Split Posted')).toBeInTheDocument())
    const resultPanel = screen.getByTestId('income-split-form-panel')
    expect(within(resultPanel).getAllByText('981.50')).toHaveLength(2)
    expect(within(resultPanel).queryByText('Ariana')).not.toBeInTheDocument()
  })

  it('shows a warning banner when active bucket percentages do not sum to 100%', async () => {
    getReserveBucketsMock.mockResolvedValue([
      { id: 'b1', name: 'Investimento', isActive: true, splitPercentage: 50, warning: null },
      { id: 'b2', name: 'HouseTreats', isActive: true, splitPercentage: 48.5, warning: null },
    ])
    render(<ReservaPage />)

    await waitFor(() => expect(screen.getByRole('alert')).toBeInTheDocument())
    expect(screen.getByText('Active bucket percentages sum to 98.50%, not 100%')).toBeInTheDocument()
  })

  it('does not show a warning banner when active bucket percentages sum to 100%', async () => {
    render(<ReservaPage />)

    await waitFor(() => expect(screen.getAllByText('Ramsay').length).toBe(4))
    expect(screen.queryByText(/Active bucket percentages sum to/)).not.toBeInTheDocument()
  })

  it('shows a lock icon and disables Edit/Delete for a movement linked to an income', async () => {
    getReserveMovementsMock.mockResolvedValue([
      { id: 'm1', bucketId: 'b1', bucketName: 'Investimento', amount: 200, date: '2026-07-25', description: 'Salary', incomeId: 'income-1' },
    ])
    render(<ReservaPage />)

    await waitFor(() => expect(screen.getByText('Salary')).toBeInTheDocument())

    const lockMessage = 'This reserve movement is linked to an income and can only be changed by editing that income.'
    expect(screen.getByLabelText(lockMessage)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Edit movement' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Delete movement' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Edit movement' })).toHaveAttribute('title', lockMessage)
    expect(screen.getByRole('button', { name: 'Delete movement' })).toHaveAttribute('title', lockMessage)
  })

  it('shows no lock icon and keeps Edit/Delete enabled for a movement not linked to an income', async () => {
    render(<ReservaPage />)

    await waitFor(() => expect(screen.getAllByText('Ramsay').length).toBe(4))

    expect(
      screen.queryByLabelText('This reserve movement is linked to an income and can only be changed by editing that income.'),
    ).not.toBeInTheDocument()
    for (const button of screen.getAllByRole('button', { name: 'Edit movement' })) {
      expect(button).toBeEnabled()
    }
    for (const button of screen.getAllByRole('button', { name: 'Delete movement' })) {
      expect(button).toBeEnabled()
    }
  })

  it('sorts the Balances grid by Balance ascending when its header is clicked', async () => {
    render(<ReservaPage />)

    await waitFor(() => expect(screen.getAllByText('Ramsay').length).toBe(4))

    fireEvent.click(screen.getByRole('button', { name: 'Balance' }))

    const balancesSection = screen.getByRole('columnheader', { name: 'Balance' }).closest('section') as HTMLElement
    const balancesTable = within(balancesSection).getByRole('columnheader', { name: 'Bucket' }).closest('table') as HTMLElement
    const dataRows = within(balancesTable).getAllByRole('row').slice(1)
    expect(within(dataRows[0]).getByText('Ariana')).toBeInTheDocument()
  })

  it('defaults the Movements grid to date descending, with the header showing the active sort, and supports clicking to re-sort', async () => {
    getReserveMovementsMock.mockResolvedValue([
      { id: 'm1', bucketId: 'b1', bucketName: 'Investimento', amount: 100, date: '2026-06-01', description: 'Older', incomeId: null },
      { id: 'm2', bucketId: 'b2', bucketName: 'HouseTreats', amount: 200, date: '2026-07-17', description: 'Newer', incomeId: null },
    ])

    render(<ReservaPage />)

    await waitFor(() => expect(screen.getByText('Newer')).toBeInTheDocument())

    const dateHeaderButton = screen.getByRole('button', { name: 'Date' })
    const dateHeader = screen.getByRole('columnheader', { name: 'Date' })
    expect(dateHeader).toHaveAttribute('aria-sort', 'descending')

    const movementsTable = dateHeader.closest('table') as HTMLElement
    let dataRows = within(movementsTable).getAllByRole('row').slice(1)
    expect(within(dataRows[0]).getByText('Newer')).toBeInTheDocument()
    expect(within(dataRows[1]).getByText('Older')).toBeInTheDocument()

    fireEvent.click(dateHeaderButton)
    expect(dateHeader).toHaveAttribute('aria-sort', 'none')

    fireEvent.click(within(movementsTable).getByRole('button', { name: 'Bucket' }))
    dataRows = within(movementsTable).getAllByRole('row').slice(1)
    expect(within(dataRows[0]).getByText('HouseTreats')).toBeInTheDocument()
    expect(within(dataRows[1]).getByText('Investimento')).toBeInTheDocument()
  })
})
