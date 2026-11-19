import { fireEvent, render, screen } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import TickerCombobox, { type TickerGroup } from '../TickerCombobox'

const GROUPS: TickerGroup[] = [
  { label: 'Ja possuidas', tickers: ['KLBN4', 'TASA4', 'TAEE3'] },
  { label: 'Outras Barse', tickers: ['UNIP6', 'CMIG4', 'TRPL4', 'BBAS3'] },
  { label: 'Outras', tickers: ['CSAN3'] },
]

function renderCombobox(value = 'KLBN4', onChange = vi.fn()) {
  return { onChange, ...render(<TickerCombobox groups={GROUPS} value={value} onChange={onChange} />) }
}

function openDropdown() {
  fireEvent.focus(screen.getByLabelText('Ticker'))
}

const input = () => screen.getByLabelText('Ticker')
const activeOptions = () => screen.queryAllByRole('option').filter((option) => option.classList.contains('ticker-combobox__option--active'))
const pressKey = (key: string) => fireEvent.keyDown(input(), { key })

describe('TickerCombobox', () => {
  beforeEach(() => {
    Element.prototype.scrollIntoView = vi.fn()
  })

  it('displays the initial value in the input', () => {
    renderCombobox('KLBN4')
    expect(screen.getByLabelText('Ticker')).toHaveValue('KLBN4')
  })

  it('renders all three group labels when open', () => {
    renderCombobox()
    openDropdown()
    expect(screen.getByText('Ja possuidas')).toBeInTheDocument()
    expect(screen.getByText('Outras Barse')).toBeInTheDocument()
    expect(screen.getByText('Outras')).toBeInTheDocument()
  })

  it('renders all 8 tickers across groups when open', () => {
    renderCombobox()
    openDropdown()
    for (const ticker of ['KLBN4', 'TASA4', 'TAEE3', 'UNIP6', 'CMIG4', 'TRPL4', 'BBAS3', 'CSAN3']) {
      expect(screen.getByRole('option', { name: ticker })).toBeInTheDocument()
    }
  })

  it('clicking an option calls onChange with that ticker', () => {
    const { onChange } = renderCombobox()
    openDropdown()
    fireEvent.mouseDown(screen.getByRole('option', { name: 'TASA4' }))
    expect(onChange).toHaveBeenCalledWith('TASA4')
  })

  it('typing into the input calls onChange with the typed value', () => {
    const { onChange } = renderCombobox()
    fireEvent.change(screen.getByLabelText('Ticker'), { target: { value: 'CXSE3' } })
    expect(onChange).toHaveBeenCalledWith('CXSE3')
  })

  it('pressing Escape closes the dropdown', () => {
    renderCombobox()
    openDropdown()
    expect(screen.getByRole('listbox')).toBeInTheDocument()
    fireEvent.keyDown(screen.getByLabelText('Ticker'), { key: 'Escape' })
    expect(screen.queryByRole('listbox')).not.toBeInTheDocument()
  })

  it('clicking outside the component closes the dropdown', () => {
    renderCombobox()
    openDropdown()
    expect(screen.getByRole('listbox')).toBeInTheDocument()
    fireEvent.mouseDown(document.body)
    expect(screen.queryByRole('listbox')).not.toBeInTheDocument()
  })

  it('pressing ArrowDown while closed opens the dropdown and activates the first option', () => {
    Element.prototype.scrollIntoView = vi.fn()
    renderCombobox()
    const input = screen.getByLabelText('Ticker')

    fireEvent.keyDown(input, { key: 'ArrowDown' })

    expect(screen.getByRole('listbox')).toBeInTheDocument()
    expect(screen.getByRole('option', { name: 'KLBN4' })).toHaveClass('ticker-combobox__option--active')
  })

  it('pressing ArrowDown repeatedly moves the active option forward, capped at the last one', () => {
    Element.prototype.scrollIntoView = vi.fn()
    renderCombobox()
    const input = screen.getByLabelText('Ticker')

    for (let i = 0; i < 10; i++) {
      fireEvent.keyDown(input, { key: 'ArrowDown' })
    }

    expect(screen.getByRole('option', { name: 'CSAN3' })).toHaveClass('ticker-combobox__option--active')
  })

  it('pressing ArrowUp moves the active option back, capped at the first one', () => {
    Element.prototype.scrollIntoView = vi.fn()
    renderCombobox()
    const input = screen.getByLabelText('Ticker')

    fireEvent.keyDown(input, { key: 'ArrowDown' })
    fireEvent.keyDown(input, { key: 'ArrowDown' })
    fireEvent.keyDown(input, { key: 'ArrowUp' })
    fireEvent.keyDown(input, { key: 'ArrowUp' })
    fireEvent.keyDown(input, { key: 'ArrowUp' })

    expect(screen.getByRole('option', { name: 'KLBN4' })).toHaveClass('ticker-combobox__option--active')
  })

  it('pressing Enter on the active option calls onChange and closes the dropdown', () => {
    Element.prototype.scrollIntoView = vi.fn()
    const { onChange } = renderCombobox()
    const input = screen.getByLabelText('Ticker')

    fireEvent.keyDown(input, { key: 'ArrowDown' })
    fireEvent.keyDown(input, { key: 'ArrowDown' })
    fireEvent.keyDown(input, { key: 'Enter' })

    expect(onChange).toHaveBeenCalledWith('TASA4')
    expect(screen.queryByRole('listbox')).not.toBeInTheDocument()
  })

  it('starts closed', () => {
    renderCombobox()

    expect(screen.queryByRole('listbox')).not.toBeInTheDocument()
    expect(input()).toHaveAttribute('aria-expanded', 'false')
  })

  it('marks only the current value as selected, and only the active option as active', () => {
    renderCombobox('TASA4')
    pressKey('ArrowDown')

    expect(screen.getByRole('option', { name: 'TASA4' })).toHaveAttribute('aria-selected', 'true')
    expect(screen.getByRole('option', { name: 'KLBN4' })).toHaveAttribute('aria-selected', 'false')
    expect(activeOptions()).toEqual([screen.getByRole('option', { name: 'KLBN4' })])
    expect([...screen.getByRole('option', { name: 'TASA4' }).classList]).toEqual(['ticker-combobox__option'])
  })

  it('pressing Enter on the first option chooses it', () => {
    const { onChange } = renderCombobox('')

    pressKey('ArrowDown')
    pressKey('Enter')

    expect(onChange).toHaveBeenCalledWith('KLBN4')
  })

  it('chooses nothing on keys other than Enter', () => {
    const { onChange } = renderCombobox()

    pressKey('ArrowDown')
    pressKey('Tab')
    pressKey('a')

    expect(onChange).not.toHaveBeenCalled()
  })

  it('keeps the keys it handles from scrolling or submitting, and a mousedown on an option from taking focus', () => {
    renderCombobox()

    expect(pressKey('ArrowDown')).toBe(false)
    expect(pressKey('ArrowUp')).toBe(false)
    expect(pressKey('Enter')).toBe(false)
    openDropdown()
    expect(fireEvent.mouseDown(screen.getByRole('option', { name: 'TASA4' }))).toBe(false)
  })

  it('scrolls the active option into view', () => {
    renderCombobox()

    pressKey('ArrowDown')
    pressKey('ArrowDown')

    const scroll = vi.mocked(Element.prototype.scrollIntoView)
    expect(scroll.mock.calls).toEqual([[{ block: 'nearest' }], [{ block: 'nearest' }]])
    expect(scroll.mock.contexts).toEqual([screen.getByRole('option', { name: 'KLBN4' }), screen.getByRole('option', { name: 'TASA4' })])
  })

  it('opens an empty list and moves through nothing when there are no tickers', () => {
    render(<TickerCombobox groups={[]} value="" onChange={vi.fn()} />)

    pressKey('ArrowDown')
    pressKey('ArrowDown')

    expect(screen.getByRole('listbox')).toBeEmptyDOMElement()
  })

  it('opens the list when typing', () => {
    renderCombobox()

    fireEvent.change(input(), { target: { value: 'KL' } })

    expect(screen.getByRole('listbox')).toBeInTheDocument()
  })

  it('stays open on a click inside it', () => {
    renderCombobox()
    openDropdown()

    fireEvent.mouseDown(input())

    expect(screen.getByRole('listbox')).toBeInTheDocument()
  })

  it.each([
    ['choosing an option', () => fireEvent.mouseDown(screen.getByRole('option', { name: 'TASA4' }))],
    ['Escape', () => pressKey('Escape')],
    ['a click outside', () => fireEvent.mouseDown(document.body)],
  ])('reopens with no active option after %s', (_, close) => {
    renderCombobox()
    pressKey('ArrowDown')
    pressKey('ArrowDown')

    close()
    openDropdown()

    expect(screen.getByRole('listbox')).toBeInTheDocument()
    expect(activeOptions()).toEqual([])
  })

  it('offers the tickers and reports to the handler passed in after they change', () => {
    const first = vi.fn()
    const second = vi.fn()
    const { rerender } = render(<TickerCombobox groups={GROUPS} value="" onChange={first} />)

    rerender(<TickerCombobox groups={[{ label: 'Novas', tickers: ['VALE3', 'PETR4'] }]} value="" onChange={second} />)
    fireEvent.change(input(), { target: { value: 'V' } })
    expect(screen.queryByRole('option', { name: 'KLBN4' })).not.toBeInTheDocument()
    fireEvent.mouseDown(screen.getByRole('option', { name: 'PETR4' }))
    fireEvent.change(input(), { target: { value: 'P' } })
    pressKey('ArrowDown')
    pressKey('ArrowDown')
    pressKey('Enter')

    expect(second.mock.calls).toEqual([['V'], ['PETR4'], ['P'], ['PETR4']])
    expect(first).not.toHaveBeenCalled()
  })

  it('pressing Enter with no active option does nothing', () => {
    const { onChange } = renderCombobox()
    openDropdown()

    fireEvent.keyDown(screen.getByLabelText('Ticker'), { key: 'Enter' })

    expect(onChange).not.toHaveBeenCalled()
  })
})
