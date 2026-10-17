import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import AdminEntityPage from '../AdminEntityPage'

type Props = Parameters<typeof AdminEntityPage>[0]

function renderPage(overrides: Partial<Props> = {}) {
  const props: Props = {
    title: 'Banks',
    createLabel: 'Create Bank',
    onCreate: vi.fn(),
    isLoading: false,
    loadError: null,
    onRetry: vi.fn(),
    isEmpty: false,
    emptyMessage: 'No banks yet — create one to get started.',
    children: <table aria-label="Banks" />,
    ...overrides,
  }
  render(<AdminEntityPage {...props} />)
  return props
}

describe('AdminEntityPage', () => {
  it('renders the title heading and calls onCreate from the create button', () => {
    const { onCreate } = renderPage()

    expect(screen.getByRole('heading', { level: 2, name: 'Banks' })).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Create Bank' }))
    expect(onCreate).toHaveBeenCalledOnce()
  })

  it('renders the content when loaded and not empty', () => {
    renderPage()

    expect(screen.getByRole('table', { name: 'Banks' })).toBeInTheDocument()
    expect(screen.queryByText('No banks yet — create one to get started.')).not.toBeInTheDocument()
  })

  it('shows the loading state instead of the content while loading', () => {
    renderPage({ isLoading: true, loadError: 'ignored while loading' })

    expect(screen.getByText('Loading...')).toBeInTheDocument()
    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
    expect(screen.queryByRole('table')).not.toBeInTheDocument()
  })

  it('shows the load error with a retry action instead of the content', () => {
    const { onRetry } = renderPage({ loadError: 'Failed to load banks' })

    expect(screen.getByRole('alert')).toHaveTextContent('Failed to load banks')
    expect(screen.queryByRole('table')).not.toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))
    expect(onRetry).toHaveBeenCalledOnce()
  })

  it('shows the empty message instead of the content when empty', () => {
    renderPage({ isEmpty: true })

    expect(screen.getByText('No banks yet — create one to get started.')).toBeInTheDocument()
    expect(screen.queryByRole('table')).not.toBeInTheDocument()
  })

  it('shows the action error only when one is set', () => {
    renderPage({ actionError: 'Could not delete Chase' })

    expect(screen.getByText('Could not delete Chase')).toBeInTheDocument()
  })

  it('orders the slots: header, belowHeader, action error, belowActionError, content, dialogs', () => {
    renderPage({
      belowHeader: <p>below header</p>,
      actionError: 'action error',
      belowActionError: <p>below action error</p>,
      dialogs: <p>dialogs</p>,
    })

    const order = ['Banks', 'below header', 'action error', 'below action error', 'dialogs'].map(
      (text) => screen.getByText(text),
    )
    const table = screen.getByRole('table', { name: 'Banks' })
    const precedes = (a: Element, b: Element) => Boolean(a.compareDocumentPosition(b) & Node.DOCUMENT_POSITION_FOLLOWING)

    expect(precedes(order[0], order[1])).toBe(true)
    expect(precedes(order[1], order[2])).toBe(true)
    expect(precedes(order[2], order[3])).toBe(true)
    expect(precedes(order[3], table)).toBe(true)
    expect(precedes(table, order[4])).toBe(true)
  })

  it('keeps dialogs mounted while the content is loading', () => {
    renderPage({ isLoading: true, dialogs: <p>dialogs</p> })

    expect(screen.getByText('dialogs')).toBeInTheDocument()
  })
})
