import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../../api/apiError'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { TitheMonthDto, TitheSummaryDto } from '../../api/types'
import { fireEvent, render, screen, waitFor, within } from '../../test/renderWithFluent'
import TitheTab from '../TitheTab'

const { getTitheSummaryByMonthMock, getTitheYearMock, updateTitheCarryForwardMock } = vi.hoisted(() => ({
  getTitheSummaryByMonthMock: vi.fn<FinancialApiClient['getTitheSummaryByMonth']>(),
  getTitheYearMock: vi.fn<FinancialApiClient['getTitheYear']>(),
  updateTitheCarryForwardMock: vi.fn<FinancialApiClient['updateTitheCarryForward']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getTitheSummaryByMonth: getTitheSummaryByMonthMock,
    getTitheYear: getTitheYearMock,
    updateTitheCarryForward: updateTitheCarryForwardMock,
  } as Partial<FinancialApiClient>,
}))

const AC = (n: string) => `[AC P61-F03-tithe-tab-and-history-${n}]`

const OCTOBER: TitheSummaryDto = {
  incomeBase: 4227.3,
  calculatedTithe: 422.73,
  tithePaid: 400,
  titheBalance: 142.73,
  carryForward: { amount: 120, included: true, fromYear: 2026, fromMonth: 9 },
}

const EMPTY: TitheSummaryDto = { incomeBase: 0, calculatedTithe: 0, tithePaid: 0, titheBalance: 0, carryForward: null }

const OTHER_MONTH: TitheSummaryDto = { incomeBase: 1000.1, calculatedTithe: 100.01, tithePaid: 120, titheBalance: -19.99, carryForward: null }

function history(): TitheMonthDto[] {
  return Array.from({ length: 12 }, (_, index) => ({ year: 2026, month: index + 1, summary: index + 1 === 10 ? OCTOBER : index + 1 === 9 ? EMPTY : OTHER_MONTH }))
}

function renderTab(onSelectMonth = vi.fn()) {
  render(<TitheTab year={2026} month={10} onSelectMonth={onSelectMonth} />)
  return onSelectMonth
}

function detail() {
  return screen.findByRole('group', { name: 'Tithe for October 2026' })
}

function historyTable() {
  return screen.findByRole('table', { name: 'Tithe by month, 2026' })
}

describe('TitheTab', () => {
  beforeEach(() => {
    getTitheSummaryByMonthMock.mockReset().mockResolvedValue(OCTOBER)
    getTitheYearMock.mockReset().mockResolvedValue(history())
    updateTitheCarryForwardMock.mockReset()
  })

  it(`${AC('01')} shows the income base, tithe, tithe paid, carry-in with its month and the balance owed`, async () => {
    renderTab()

    const figures = await detail()
    expect(figures).toHaveTextContent('Income base: 4,227.30')
    expect(figures).toHaveTextContent('Tithe: 422.73')
    expect(figures).toHaveTextContent('Tithe paid: 400.00')
    expect(figures).toHaveTextContent('Carry-in: 120.00 from September 2026')
    expect(figures).toHaveTextContent('Balance: 142.73 owed')
    expect(getTitheSummaryByMonthMock).toHaveBeenCalledWith(2026, 10)
  })

  it(`${AC('06')} shows a negative balance as in credit, in words`, async () => {
    getTitheSummaryByMonthMock.mockResolvedValue(OTHER_MONTH)
    renderTab()

    expect(await detail()).toHaveTextContent('Balance: 19.99 in credit')
  })

  it(`${AC('06')} shows an empty month with zero figures, no checkbox and a no-income message`, async () => {
    getTitheSummaryByMonthMock.mockResolvedValue(EMPTY)
    renderTab()

    const figures = await detail()
    expect(figures).toHaveTextContent('Income base: 0.00')
    expect(figures).toHaveTextContent('Carry-in: 0.00')
    expect(figures).toHaveTextContent('Balance: 0.00')
    expect(figures).not.toHaveTextContent(/owed|in credit/)
    expect(screen.getByText('No income this month.')).toBeInTheDocument()
    expect(screen.queryByRole('checkbox')).not.toBeInTheDocument()
  })

  it(`${AC('02')} shows the carry-forward checkbox only with a carry-in, checked from the server`, async () => {
    renderTab()

    const checkbox = await screen.findByRole('checkbox', { name: "Include September 2026's balance of 120.00" })
    expect(checkbox).toBeChecked()
    expect(screen.queryByText('No income this month.')).not.toBeInTheDocument()
  })

  it(`${AC('02')} keeps focus on the checkbox and shows progress while saving, then shows the refreshed figures`, async () => {
    let resolveUpdate!: (value: TitheSummaryDto) => void
    updateTitheCarryForwardMock.mockReturnValue(new Promise((resolve) => { resolveUpdate = resolve }))
    renderTab()
    const checkbox = await screen.findByRole('checkbox', { name: /Include September 2026/ })
    checkbox.focus()
    const excluded = { ...OCTOBER, titheBalance: 22.73, carryForward: { amount: 120, included: false, fromYear: 2026, fromMonth: 9 } }
    getTitheSummaryByMonthMock.mockResolvedValue(excluded)

    fireEvent.click(checkbox)

    expect(updateTitheCarryForwardMock).toHaveBeenCalledWith(2026, 10, { included: false })
    expect(checkbox).toHaveAttribute('aria-disabled', 'true')
    expect(checkbox).toHaveFocus()
    expect(screen.getByText('Saving…')).toBeInTheDocument()
    fireEvent.click(checkbox)
    expect(updateTitheCarryForwardMock).toHaveBeenCalledOnce()

    resolveUpdate(excluded)

    await waitFor(() => expect(checkbox).not.toBeChecked())
    expect(await detail()).toHaveTextContent('Balance: 22.73 owed')
    expect(screen.queryByText('Saving…')).not.toBeInTheDocument()
    expect(getTitheYearMock).toHaveBeenCalledTimes(2)
  })

  it(`${AC('02')} shows a failed update in an error message bar and leaves the checkbox as it was`, async () => {
    updateTitheCarryForwardMock.mockRejectedValue(new ApiError('Server error', 500))
    renderTab()

    fireEvent.click(await screen.findByRole('checkbox', { name: /Include September 2026/ }))

    expect(await screen.findByText("Couldn't update the carry-forward. Try again.")).toBeInTheDocument()
    expect(screen.getByRole('checkbox', { name: /Include September 2026/ })).toBeChecked()
  })

  it(`${AC('02')} keeps the no-balance message visible after the refresh removes the checkbox`, async () => {
    updateTitheCarryForwardMock.mockRejectedValue(new ApiError('No carry-forward is available for 2026-10.', 400))
    renderTab()
    const checkbox = await screen.findByRole('checkbox', { name: /Include September 2026/ })
    getTitheSummaryByMonthMock.mockResolvedValue({ ...OCTOBER, carryForward: null })

    fireEvent.click(checkbox)

    expect(await screen.findByText('There is no balance to carry into this month.')).toBeInTheDocument()
    await waitFor(() => expect(screen.queryByRole('checkbox')).not.toBeInTheDocument())
    expect(screen.getByText('There is no balance to carry into this month.')).toBeInTheDocument()
  })

  it(`${AC('03')} lists the twelve months with the chosen one marked and a totals footer`, async () => {
    renderTab()

    const table = await historyTable()
    const [header, ...rows] = within(table).getAllByRole('row')
    expect(within(header).getAllByRole('columnheader').map((cell) => cell.textContent)).toEqual(['Month', 'Income base', 'Tithe', 'Tithe paid', 'Carry-in', 'Included', 'Balance'])
    const body = rows.slice(0, 12)
    expect(body).toHaveLength(12)
    expect(body[0]).toHaveTextContent('January 2026')
    expect(body[11]).toHaveTextContent('December 2026')
    expect(body.filter((row) => row.getAttribute('aria-current') === 'date')).toEqual([body[9]])
    expect(within(body[9]).getByRole('button', { name: 'Show October 2026' })).toHaveAttribute('aria-current', 'date')
    expect(within(table).getAllByRole('button').filter((button) => button.hasAttribute('aria-current'))).toHaveLength(1)
    expect(body[9]).toHaveTextContent(/4,227\.30.*422\.73.*400\.00.*120\.00.*Yes.*142\.73 owed/)
    expect(within(body[8]).getByRole('img', { name: 'No carry-in' })).toBeInTheDocument()
    expect(body[0]).toHaveTextContent('19.99 in credit')
    const footer = rows[12]
    expect(footer).toHaveTextContent('Total')
    expect(footer).toHaveTextContent('14,228.30')
    expect(footer).toHaveTextContent('1,422.83')
    expect(footer).toHaveTextContent('1,600.00')
    expect(within(footer).getAllByRole('img', { name: 'Not totalled' })).toHaveLength(3)
  })

  it(`${AC('03')} selects a month from its row`, async () => {
    const onSelectMonth = renderTab()

    fireEvent.click(await screen.findByRole('button', { name: 'Show March 2026' }))

    expect(onSelectMonth).toHaveBeenCalledWith('2026-03')
  })

  it(`${AC('06')} shows a loading state for the detail and the history`, () => {
    getTitheSummaryByMonthMock.mockReturnValue(new Promise(() => {}))
    getTitheYearMock.mockReturnValue(new Promise(() => {}))
    renderTab()

    expect(screen.getByText('Loading the tithe…')).toBeInTheDocument()
    expect(screen.getByText('Loading the tithe history…')).toBeInTheDocument()
    expect(screen.queryByRole('table')).not.toBeInTheDocument()
  })

  it(`${AC('06')} shows each load error on its own with Try again, keeping the other part`, async () => {
    getTitheYearMock.mockRejectedValueOnce(new Error('boom'))
    renderTab()

    expect(await screen.findByText('Unable to load the tithe history.')).toBeInTheDocument()
    expect(await detail()).toHaveTextContent('Income base: 4,227.30')

    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))

    expect(await historyTable()).toBeInTheDocument()
    expect(getTitheYearMock).toHaveBeenCalledTimes(2)
  })

  it(`${AC('06')} shows the detail error while the history still loads`, async () => {
    getTitheSummaryByMonthMock.mockRejectedValueOnce(new Error('boom'))
    renderTab()

    expect(await screen.findByText('Unable to load the tithe for this month.')).toBeInTheDocument()
    expect(await historyTable()).toBeInTheDocument()
  })
})
