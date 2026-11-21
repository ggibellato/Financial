import { fireEvent, render, screen } from '../../test/renderWithFluent'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import TickerCombobox, { type TickerGroup } from '../TickerCombobox'

const GROUPS: TickerGroup[] = [
  { label: 'Ja possuidas', tickers: ['KLBN4', 'TASA4'] },
  { label: 'Outras', tickers: ['CSAN3'] },
]

const ticker = () => screen.getByRole('combobox', { name: /Ticker/ })

function renderCombobox(value = 'KLBN4') {
  const onChange = vi.fn()
  render(<TickerCombobox groups={GROUPS} value={value} onChange={onChange} />)
  return { onChange }
}

describe('TickerCombobox', () => {
  it('reports the ticker chosen from the list', async () => {
    const { onChange } = renderCombobox()

    await userEvent.click(ticker())
    await userEvent.click(screen.getByRole('option', { name: 'TASA4' }))

    expect(onChange).toHaveBeenLastCalledWith('TASA4')
  })

  it('reports a typed ticker that is not on the watchlist', () => {
    const { onChange } = renderCombobox()

    fireEvent.change(ticker(), { target: { value: 'CXSE3' } })

    expect(onChange).toHaveBeenLastCalledWith('CXSE3')
    expect(onChange).not.toHaveBeenCalledWith('')
  })

  const shownTickers = () => screen.queryAllByRole('option').map((option) => option.textContent)

  it.each([
    ['an empty value', '', ['KLBN4', 'TASA4', 'CSAN3']],
    ['a watchlist ticker', 'TASA4', ['KLBN4', 'TASA4', 'CSAN3']],
    ['a partial ticker', 'kl', ['KLBN4']],
  ])('offers the tickers matching %s', async (_, value, expected) => {
    renderCombobox(value)

    await userEvent.click(ticker())

    expect(shownTickers()).toEqual(expected)
  })

  it('keeps the list closed when no watchlist ticker matches', async () => {
    renderCombobox('CXSE3')

    await userEvent.click(ticker())

    expect(ticker()).toHaveAttribute('aria-expanded', 'false')
  })

  it('picks the active option on Enter instead of submitting the form', async () => {
    const onChange = vi.fn()
    const onSubmit = vi.fn((event: React.FormEvent) => event.preventDefault())
    render(
      <form onSubmit={onSubmit}>
        <TickerCombobox groups={GROUPS} value="TA" onChange={onChange} />
      </form>,
    )

    await userEvent.click(ticker())
    await userEvent.keyboard('{ArrowDown}{Enter}')

    expect(onChange).toHaveBeenLastCalledWith('TASA4')
    expect(onSubmit).not.toHaveBeenCalled()
  })
})
