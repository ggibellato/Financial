import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import IntegrationsPage from '../IntegrationsPage'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { CreditCardCalendarSyncStatusDto, CreditCardDto } from '../../api/types'

const {
  getCalendarStatusMock,
  disconnectCalendarMock,
  getCreditCardsMock,
  getCalendarSyncStatusesMock,
  resyncCreditCardCalendarMock,
  buildCalendarConnectUrlMock,
} = vi.hoisted(() => ({
  getCalendarStatusMock: vi.fn<FinancialApiClient['getCalendarStatus']>(),
  disconnectCalendarMock: vi.fn<FinancialApiClient['disconnectCalendar']>(),
  getCreditCardsMock: vi.fn<FinancialApiClient['getCreditCards']>(),
  getCalendarSyncStatusesMock: vi.fn<FinancialApiClient['getCalendarSyncStatuses']>(),
  resyncCreditCardCalendarMock: vi.fn<FinancialApiClient['resyncCreditCardCalendar']>(),
  buildCalendarConnectUrlMock: vi.fn<FinancialApiClient['buildCalendarConnectUrl']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getCalendarStatus: getCalendarStatusMock,
    disconnectCalendar: disconnectCalendarMock,
    getCreditCards: getCreditCardsMock,
    getCalendarSyncStatuses: getCalendarSyncStatusesMock,
    resyncCreditCardCalendar: resyncCreditCardCalendarMock,
    buildCalendarConnectUrl: buildCalendarConnectUrlMock,
  } as Partial<FinancialApiClient>,
}))

const CONNECTED_STATUS = {
  connected: true,
  accountEmail: 'user@gmail.com',
  calendarName: 'Financial - Credit Card Due Dates',
  calendarId: 'abc123@group.calendar.google.com',
  connectedAtUtc: '2026-09-01T10:00:00Z',
  disconnectReason: null,
}

const NOT_CONNECTED_STATUS = {
  connected: false,
  accountEmail: null,
  calendarName: null,
  calendarId: null,
  connectedAtUtc: null,
  disconnectReason: null,
}

const CREDIT_CARDS: CreditCardDto[] = [
  { id: 'card-synced', name: 'BaAmex', isActive: true, nextInvoiceDueDate: '2026-09-10', latestInvoiceDate: null, hasReferences: false },
  { id: 'card-error', name: 'Nubank', isActive: true, nextInvoiceDueDate: '2026-09-15', latestInvoiceDate: null, hasReferences: false },
]

const SYNC_STATUSES: CreditCardCalendarSyncStatusDto[] = [
  { creditCardId: 'card-synced', state: 'Synced', lastSuccessfulSyncUtc: '2026-09-01T10:00:00Z', lastError: null },
  { creditCardId: 'card-error', state: 'Error', lastSuccessfulSyncUtc: null, lastError: 'Rate limit exceeded' },
]

describe('IntegrationsPage', () => {
  beforeEach(() => {
    getCalendarStatusMock.mockReset()
    disconnectCalendarMock.mockReset()
    getCreditCardsMock.mockReset()
    getCalendarSyncStatusesMock.mockReset()
    resyncCreditCardCalendarMock.mockReset()
    buildCalendarConnectUrlMock.mockReset()
    buildCalendarConnectUrlMock.mockReturnValue('https://api.example.com/api/v1/financial/integrations/calendar/connect')
    getCreditCardsMock.mockResolvedValue(CREDIT_CARDS)
    getCalendarSyncStatusesMock.mockResolvedValue(SYNC_STATUSES)
    vi.spyOn(window, 'open').mockImplementation(() => null)
  })

  it('P45-F03-integrations-settings-web-01: shows a Connect Google Calendar button when not connected', async () => {
    getCalendarStatusMock.mockResolvedValue(NOT_CONNECTED_STATUS)
    render(<IntegrationsPage />)

    expect(await screen.findByRole('button', { name: 'Connect Google Calendar' })).toBeInTheDocument()
  })

  it('P45-F03-integrations-settings-web-01: shows the connected account email, calendar name and per-card sync list when connected', async () => {
    getCalendarStatusMock.mockResolvedValue(CONNECTED_STATUS)
    render(<IntegrationsPage />)

    expect(await screen.findByText('user@gmail.com')).toBeInTheDocument()
    expect(screen.getByText('Financial - Credit Card Due Dates')).toBeInTheDocument()
    await waitFor(() => expect(screen.getByText('BaAmex')).toBeInTheDocument())
    expect(screen.getByText('Nubank')).toBeInTheDocument()
  })

  it('P45-F03-integrations-settings-web-02: conveys each row sync status with visible text, never color alone', async () => {
    getCalendarStatusMock.mockResolvedValue(CONNECTED_STATUS)
    render(<IntegrationsPage />)

    expect(await screen.findByText('Synced')).toBeInTheDocument()
    expect(screen.getByText('Sync failed: Rate limit exceeded')).toBeInTheDocument()
  })

  it('P45-F03-integrations-settings-web-03: clicking Disconnect always shows a confirmation dialog before the request is sent', async () => {
    getCalendarStatusMock.mockResolvedValue(CONNECTED_STATUS)
    render(<IntegrationsPage />)
    await waitFor(() => expect(screen.getByText('user@gmail.com')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Disconnect' }))

    expect(screen.getByRole('heading', { name: 'Disconnect Google Calendar' })).toBeInTheDocument()
    expect(disconnectCalendarMock).not.toHaveBeenCalled()
  })

  it('P45-F03-integrations-settings-web-03: cancelling the confirmation dialog leaves the connection intact', async () => {
    getCalendarStatusMock.mockResolvedValue(CONNECTED_STATUS)
    render(<IntegrationsPage />)
    await waitFor(() => expect(screen.getByText('user@gmail.com')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Disconnect' }))
    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))

    expect(screen.queryByRole('heading', { name: 'Disconnect Google Calendar' })).not.toBeInTheDocument()
    expect(disconnectCalendarMock).not.toHaveBeenCalled()
  })

  it('P45-F03-integrations-settings-web-03: confirming the dialog sends the disconnect request', async () => {
    getCalendarStatusMock.mockResolvedValueOnce(CONNECTED_STATUS).mockResolvedValue(NOT_CONNECTED_STATUS)
    disconnectCalendarMock.mockResolvedValue({ remoteCleanupSucceeded: true })
    render(<IntegrationsPage />)
    await waitFor(() => expect(screen.getByText('user@gmail.com')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Disconnect' }))
    const dialogButtons = screen.getAllByRole('button', { name: 'Disconnect' })
    fireEvent.click(dialogButtons[dialogButtons.length - 1])

    await waitFor(() => expect(disconnectCalendarMock).toHaveBeenCalledOnce())
  })

  it('P45-F03-integrations-settings-web-04: clicking Retry on an errored row calls its resync endpoint and updates its status', async () => {
    getCalendarStatusMock.mockResolvedValue(CONNECTED_STATUS)
    resyncCreditCardCalendarMock.mockResolvedValue({
      creditCardId: 'card-error',
      state: 'Synced',
      lastSuccessfulSyncUtc: '2026-09-02T08:00:00Z',
      lastError: null,
    })
    render(<IntegrationsPage />)
    await waitFor(() => expect(screen.getByText('Nubank')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Retry calendar sync for Nubank' }))

    await waitFor(() => expect(resyncCreditCardCalendarMock).toHaveBeenCalledWith('card-error'))
    await waitFor(() => expect(screen.getAllByText('Synced')).toHaveLength(2))
  })

  it('P45-F03-integrations-settings-web-05: a failed status request shows a retry affordance instead of a blank panel', async () => {
    getCalendarStatusMock.mockRejectedValue(new Error('Network down'))
    render(<IntegrationsPage />)

    expect(await screen.findByRole('alert')).toHaveTextContent('Network down')
    expect(screen.getByRole('button', { name: 'Try again' })).toBeInTheDocument()
  })

  it('retrying after a failed status request loads the panel normally', async () => {
    getCalendarStatusMock.mockRejectedValueOnce(new Error('Network down')).mockResolvedValue(NOT_CONNECTED_STATUS)
    render(<IntegrationsPage />)
    await screen.findByRole('alert')

    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))

    expect(await screen.findByRole('button', { name: 'Connect Google Calendar' })).toBeInTheDocument()
  })

  it('is keyboard operable: Tab reaches the Connect button and Enter activates it', async () => {
    getCalendarStatusMock.mockResolvedValue(NOT_CONNECTED_STATUS)
    const user = userEvent.setup()
    render(<IntegrationsPage />)
    await screen.findByRole('button', { name: 'Connect Google Calendar' })

    await user.tab()
    expect(screen.getByRole('button', { name: 'Connect Google Calendar' })).toHaveFocus()
    await user.keyboard('{Enter}')

    expect(window.open).toHaveBeenCalledWith(
      'https://api.example.com/api/v1/financial/integrations/calendar/connect',
      '_blank',
      'noopener,noreferrer',
    )
  })
})
