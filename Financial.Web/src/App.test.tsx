import { render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import App from './App'

const getNavigationTreeMock = vi.fn()

vi.mock('./api/financialApiClient', () => ({
  createFinancialApiClient: () => ({
    getNavigationTree: getNavigationTreeMock,
  }),
}))

describe('App', () => {
  beforeEach(() => {
    getNavigationTreeMock.mockReset()
    getNavigationTreeMock.mockResolvedValue({
      nodeType: 'Investments',
      displayName: 'All Investments',
      metadata: {},
      children: [],
    })
  })

  it('renders the app header', () => {
    render(
      <MemoryRouter initialEntries={['/']}>
        <Routes>
          <Route path="/" element={<App />} />
        </Routes>
      </MemoryRouter>,
    )

    expect(screen.getByRole('heading', { name: 'Portfolio Dashboard' })).toBeInTheDocument()
  })
})
