import { act, renderHook, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { CalendarConnectionStatusDto } from '../../api/types'
import { useCalendarConnection } from '../useCalendarConnection'

const { getCalendarStatusMock, disconnectCalendarMock, buildCalendarConnectUrlMock } = vi.hoisted(() => ({
  getCalendarStatusMock: vi.fn<FinancialApiClient['getCalendarStatus']>(),
  disconnectCalendarMock: vi.fn<FinancialApiClient['disconnectCalendar']>(),
  buildCalendarConnectUrlMock: vi.fn<FinancialApiClient['buildCalendarConnectUrl']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getCalendarStatus: getCalendarStatusMock,
    disconnectCalendar: disconnectCalendarMock,
    buildCalendarConnectUrl: buildCalendarConnectUrlMock,
  } as Partial<FinancialApiClient>,
}))

const NOT_CONNECTED: CalendarConnectionStatusDto = {
  connected: false,
  accountEmail: null,
  calendarName: null,
  calendarId: null,
  connectedAtUtc: null,
  disconnectReason: null,
}

const CONNECTED: CalendarConnectionStatusDto = {
  connected: true,
  accountEmail: 'user@gmail.com',
  calendarName: 'Financial - Credit Card Due Dates',
  calendarId: 'abc123@group.calendar.google.com',
  connectedAtUtc: '2026-09-01T10:00:00Z',
  disconnectReason: null,
}

describe('useCalendarConnection', () => {
  beforeEach(() => {
    getCalendarStatusMock.mockReset()
    disconnectCalendarMock.mockReset()
    buildCalendarConnectUrlMock.mockReset()
    getCalendarStatusMock.mockResolvedValue(NOT_CONNECTED)
    buildCalendarConnectUrlMock.mockReturnValue('https://api.example.com/api/v1/financial/integrations/calendar/connect')
    vi.spyOn(window, 'open').mockImplementation(() => null)
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('fetches the connection status on mount', async () => {
    const { result } = renderHook(() => useCalendarConnection())

    expect(result.current.isLoading).toBe(true)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(getCalendarStatusMock).toHaveBeenCalledOnce()
    expect(result.current.status).toEqual(NOT_CONNECTED)
  })

  it('surfaces a fetch error', async () => {
    getCalendarStatusMock.mockRejectedValue(new Error('Network down'))
    const { result } = renderHook(() => useCalendarConnection())

    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.error).toBe('Network down')
  })

  it('connect opens a new tab and arms the connecting state', async () => {
    const { result } = renderHook(() => useCalendarConnection())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.connect())

    expect(window.open).toHaveBeenCalledWith(
      'https://api.example.com/api/v1/financial/integrations/calendar/connect',
      '_blank',
      'noopener,noreferrer',
    )
    expect(result.current.isConnecting).toBe(true)
  })

  it('re-polls status on window focus while connecting, and clears isConnecting once connected', async () => {
    const { result } = renderHook(() => useCalendarConnection())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.connect())
    expect(result.current.isConnecting).toBe(true)

    getCalendarStatusMock.mockResolvedValue(CONNECTED)
    await act(async () => {
      window.dispatchEvent(new Event('focus'))
    })

    await waitFor(() => expect(result.current.isConnecting).toBe(false))
    expect(result.current.status).toEqual(CONNECTED)
  })

  it('keeps isConnecting true across a focus event while still not connected', async () => {
    const { result } = renderHook(() => useCalendarConnection())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.connect())
    getCalendarStatusMock.mockResolvedValue(NOT_CONNECTED)

    await act(async () => {
      window.dispatchEvent(new Event('focus'))
    })

    expect(result.current.isConnecting).toBe(true)
  })

  it('disconnect calls the API and refreshes status', async () => {
    disconnectCalendarMock.mockResolvedValue({ remoteCleanupSucceeded: true })
    getCalendarStatusMock.mockResolvedValueOnce(CONNECTED)
    const { result } = renderHook(() => useCalendarConnection())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    getCalendarStatusMock.mockResolvedValueOnce(NOT_CONNECTED)
    await act(async () => {
      await result.current.disconnect()
    })

    expect(disconnectCalendarMock).toHaveBeenCalledOnce()
    expect(result.current.status).toEqual(NOT_CONNECTED)
    expect(result.current.isDisconnecting).toBe(false)
  })

  it('surfaces a disconnect error and rethrows it to the caller', async () => {
    disconnectCalendarMock.mockRejectedValue(new Error('Failed to disconnect'))
    const { result } = renderHook(() => useCalendarConnection())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    await act(async () => {
      await expect(result.current.disconnect()).rejects.toThrow('Failed to disconnect')
    })

    expect(result.current.disconnectError).toBe('Failed to disconnect')
    expect(result.current.isDisconnecting).toBe(false)
  })
})
