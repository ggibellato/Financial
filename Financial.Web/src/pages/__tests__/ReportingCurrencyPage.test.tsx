import { beforeEach, describe, expect, it, vi } from 'vitest'
import { render, screen, waitFor } from '../../test/renderWithFluent'
import ReportingCurrencyPage from '../ReportingCurrencyPage'
import type { FinancialApiClient } from '../../api/financialApiClient'

const { getReportingCurrencyMock, setReportingCurrencyMock, setReportingCurrencyEnabledMock } = vi.hoisted(() => ({
  getReportingCurrencyMock: vi.fn<FinancialApiClient['getReportingCurrency']>(),
  setReportingCurrencyMock: vi.fn<FinancialApiClient['setReportingCurrency']>(),
  setReportingCurrencyEnabledMock: vi.fn<FinancialApiClient['setReportingCurrencyEnabled']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getReportingCurrency: getReportingCurrencyMock,
    setReportingCurrency: setReportingCurrencyMock,
    setReportingCurrencyEnabled: setReportingCurrencyEnabledMock,
  } as Partial<FinancialApiClient>,
}))

describe('ReportingCurrencyPage', () => {
  beforeEach(() => {
    getReportingCurrencyMock.mockReset()
    setReportingCurrencyMock.mockReset()
    setReportingCurrencyEnabledMock.mockReset()
    getReportingCurrencyMock.mockResolvedValue({ currency: 'GBP', enabled: true })
  })

  it('renders_the_three_currency_options', async () => {
    render(<ReportingCurrencyPage />)

    expect(await screen.findByRole('radio', { name: 'GBP' })).toBeInTheDocument()
    expect(screen.getByRole('radio', { name: 'BRL' })).toBeInTheDocument()
    expect(screen.getByRole('radio', { name: 'USD' })).toBeInTheDocument()
  })

  it('shows_a_loading_state_before_data_arrives', () => {
    render(<ReportingCurrencyPage />)

    expect(screen.getByText(/Loading/)).toBeInTheDocument()
  })

  it('shows_an_error_state_on_load_failure', async () => {
    getReportingCurrencyMock.mockRejectedValue(new Error('Network down'))
    render(<ReportingCurrencyPage />)

    expect(await screen.findByRole('alert')).toBeInTheDocument()
  })

  it('the_currently_saved_currency_is_selected', async () => {
    getReportingCurrencyMock.mockResolvedValue({ currency: 'BRL', enabled: true })
    render(<ReportingCurrencyPage />)

    expect(await screen.findByRole('radio', { name: 'BRL' })).toBeChecked()
  })

  it('selecting_a_currency_calls_setReportingCurrency', async () => {
    setReportingCurrencyMock.mockResolvedValue({ currency: 'USD', enabled: true })
    render(<ReportingCurrencyPage />)
    const usdOption = await screen.findByRole('radio', { name: 'USD' })

    usdOption.click()

    await waitFor(() => expect(setReportingCurrencyMock).toHaveBeenCalledWith({ currency: 'USD', enabled: true }))
  })

  it('shows_a_save_error_inline_without_hiding_the_control', async () => {
    setReportingCurrencyMock.mockRejectedValue(new Error('Currency not recognized'))
    render(<ReportingCurrencyPage />)
    const brlOption = await screen.findByRole('radio', { name: 'BRL' })

    brlOption.click()

    expect(await screen.findByText('Currency not recognized')).toBeInTheDocument()
    expect(screen.getByRole('radio', { name: 'GBP' })).toBeInTheDocument()
  })

  it('the_toggle_reflects_the_currently_saved_enabled_state', async () => {
    getReportingCurrencyMock.mockResolvedValue({ currency: 'GBP', enabled: false })
    render(<ReportingCurrencyPage />)

    expect(await screen.findByRole('switch')).not.toBeChecked()
  })

  it('turning_the_toggle_off_calls_setReportingCurrencyEnabled_and_disables_the_currency_picker', async () => {
    setReportingCurrencyEnabledMock.mockResolvedValue({ currency: 'GBP', enabled: false })
    render(<ReportingCurrencyPage />)
    const toggle = await screen.findByRole('switch')

    toggle.click()

    await waitFor(() => expect(setReportingCurrencyEnabledMock).toHaveBeenCalledWith({ enabled: false }))
    await waitFor(() => expect(screen.getByRole('radio', { name: 'GBP' })).toBeDisabled())
  })
})
