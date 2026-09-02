import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { FluentProvider, webLightTheme } from '@fluentui/react-components'
import type { PaymentsDueData } from '../../hooks/usePaymentsDue'
import type { PaymentDueDto } from '../../api/types'
import PaymentDueBanner from '../PaymentDueBanner'

const dismissMock = vi.fn()

const mockHookValue: PaymentsDueData = {
  payments: null,
  dismiss: dismissMock,
}

vi.mock('../../hooks/usePaymentsDue', () => ({
  usePaymentsDue: () => mockHookValue,
}))

function setPayments(payments: PaymentDueDto[] | null) {
  mockHookValue.payments = payments
}

function renderBanner() {
  return render(
    <FluentProvider theme={webLightTheme}>
      <PaymentDueBanner />
    </FluentProvider>,
  )
}

const TODAY_PAYMENT: PaymentDueDto = { type: 'Mensais', name: 'Internet', dueDate: '2026-09-05', daysRemaining: 0 }
const SOON_PAYMENT: PaymentDueDto = { type: 'CreditCard', name: 'Nubank', dueDate: '2026-09-07', daysRemaining: 2 }
const UPCOMING_PAYMENT: PaymentDueDto = { type: 'Mensais', name: 'Rent', dueDate: '2026-09-10', daysRemaining: 5 }

describe('PaymentDueBanner', () => {
  beforeEach(() => {
    dismissMock.mockReset()
    setPayments(null)
  })

  it('no_banner_when_payments_is_null', () => {
    renderBanner()

    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
  })

  it('no_banner_when_payments_is_empty_array', () => {
    setPayments([])

    renderBanner()

    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
  })

  it('banner_visible_with_title_when_payments_present', () => {
    setPayments([TODAY_PAYMENT])

    renderBanner()

    expect(screen.getByRole('alert')).toBeInTheDocument()
    expect(screen.getByText('Upcoming payments')).toBeInTheDocument()
  })

  it('renders_type_name_date_and_days_remaining_per_item', () => {
    setPayments([SOON_PAYMENT])

    renderBanner()

    expect(screen.getByText('Credit card')).toBeInTheDocument()
    expect(screen.getByText('Nubank')).toBeInTheDocument()
    expect(screen.getByText('07/09/2026')).toBeInTheDocument()
    expect(screen.getByText('Due in 2 days')).toBeInTheDocument()
  })

  it('today_tier_uses_danger_color_and_alert_icon', () => {
    setPayments([TODAY_PAYMENT])

    renderBanner()

    const badge = screen.getByLabelText(/Due today.*urgent/)
    expect(badge).toBeInTheDocument()
  })

  it('soon_tier_uses_warning_color_and_clock_icon', () => {
    setPayments([SOON_PAYMENT])

    renderBanner()

    expect(screen.getByLabelText(/Due in 2 days.*soon/)).toBeInTheDocument()
  })

  it('upcoming_tier_uses_informative_color_and_calendar_icon', () => {
    setPayments([UPCOMING_PAYMENT])

    renderBanner()

    expect(screen.getByLabelText(/Due in 5 days.*upcoming/)).toBeInTheDocument()
  })

  it('items_render_in_the_order_provided_by_the_hook', () => {
    setPayments([UPCOMING_PAYMENT, TODAY_PAYMENT, SOON_PAYMENT])

    renderBanner()

    const names = screen.getAllByText(/^(Rent|Internet|Nubank)$/).map((el) => el.textContent)
    expect(names).toEqual(['Rent', 'Internet', 'Nubank'])
  })

  it('close_button_calls_dismiss', async () => {
    setPayments([TODAY_PAYMENT])
    const user = userEvent.setup()

    renderBanner()
    await user.click(screen.getByRole('button', { name: /dismiss/i }))

    expect(dismissMock).toHaveBeenCalledTimes(1)
  })

  it('close_button_is_keyboard_operable', async () => {
    setPayments([TODAY_PAYMENT])
    const user = userEvent.setup()

    renderBanner()
    await user.tab()
    await user.keyboard('{Enter}')

    expect(dismissMock).toHaveBeenCalledTimes(1)
  })

  it('close_button_has_accessible_name', () => {
    setPayments([TODAY_PAYMENT])

    renderBanner()

    expect(screen.getByRole('button', { name: /dismiss/i })).toBeInTheDocument()
  })
})
