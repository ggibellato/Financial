import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import CategoriesPage from '../CategoriesPage'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { CategoryDto } from '../../api/types'

const { getCategoriesMock, createCategoryMock, updateCategoryMock, deleteCategoryMock } = vi.hoisted(() => ({
  getCategoriesMock: vi.fn<FinancialApiClient['getCategories']>(),
  createCategoryMock: vi.fn<FinancialApiClient['createCategory']>(),
  updateCategoryMock: vi.fn<FinancialApiClient['updateCategory']>(),
  deleteCategoryMock: vi.fn<FinancialApiClient['deleteCategory']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getCategories: getCategoriesMock,
    createCategory: createCategoryMock,
    updateCategory: updateCategoryMock,
    deleteCategory: deleteCategoryMock,
  } as Partial<FinancialApiClient>,
}))

const CATEGORIES: CategoryDto[] = [
  { id: 'c1', name: 'Mercado', active: true, isInvestment: false, isTithe: false, hasReferences: true },
  { id: 'c2', name: 'Extras', active: true, isInvestment: false, isTithe: false, hasReferences: false },
  { id: 'c3', name: 'Reserva', active: false, isInvestment: false, isTithe: false, hasReferences: false },
]

describe('CategoriesPage', () => {
  beforeEach(() => {
    getCategoriesMock.mockReset()
    createCategoryMock.mockReset()
    updateCategoryMock.mockReset()
    deleteCategoryMock.mockReset()
    getCategoriesMock.mockResolvedValue(CATEGORIES)
  })

  it('renders only active categories by default', async () => {
    render(<CategoriesPage />)

    await waitFor(() => expect(screen.getByText('Mercado')).toBeInTheDocument())
    expect(screen.getByText('Extras')).toBeInTheDocument()
    expect(screen.queryByText('Reserva')).not.toBeInTheDocument()
  })

  it('shows inactive categories once the Show inactive switch is toggled', async () => {
    render(<CategoriesPage />)
    await waitFor(() => expect(screen.getByText('Mercado')).toBeInTheDocument())

    fireEvent.click(screen.getByLabelText('Show inactive'))

    expect(screen.getByText('Reserva')).toBeInTheDocument()
  })

  it('shows the empty state when there are no categories', async () => {
    getCategoriesMock.mockResolvedValue([])
    render(<CategoriesPage />)

    expect(await screen.findByText('No categories yet — create one to get started.')).toBeInTheDocument()
  })

  it('shows an error state with retry on load failure', async () => {
    getCategoriesMock.mockRejectedValue(new Error('Network down'))
    render(<CategoriesPage />)

    expect(await screen.findByRole('alert')).toHaveTextContent('Network down')
  })

  it('creates a category through the Create Category dialog', async () => {
    createCategoryMock.mockResolvedValue({
      id: 'c4',
      name: 'Lazer',
      active: true,
      isInvestment: false,
      isTithe: false,
      hasReferences: false,
    })
    render(<CategoriesPage />)
    await waitFor(() => expect(screen.getByText('Mercado')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Create Category' }))
    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'Lazer' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() =>
      expect(createCategoryMock).toHaveBeenCalledWith({ name: 'Lazer', active: true, isInvestment: false, isTithe: false }),
    )
    await waitFor(() => expect(screen.queryByRole('heading', { name: 'Create Category' })).not.toBeInTheDocument())
  })

  it('edits a category through its row action', async () => {
    updateCategoryMock.mockResolvedValue({
      id: 'c1',
      name: 'Mercado Renamed',
      active: true,
      isInvestment: false,
      isTithe: false,
      hasReferences: true,
    })
    render(<CategoriesPage />)
    await waitFor(() => expect(screen.getByText('Mercado')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Edit Mercado' }))
    expect(screen.getByRole('heading', { name: 'Edit Category' })).toBeInTheDocument()
    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'Mercado Renamed' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() =>
      expect(updateCategoryMock).toHaveBeenCalledWith('c1', {
        name: 'Mercado Renamed',
        active: true,
        isInvestment: false,
        isTithe: false,
      }),
    )
  })

  it('disables delete confirmation when the category still has references', async () => {
    render(<CategoriesPage />)
    await waitFor(() => expect(screen.getByText('Mercado')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Delete Mercado' }))

    expect(screen.getByText(/still used by a transaction and cannot be deleted/)).toBeInTheDocument()
    const confirmButtons = screen.getAllByRole('button', { name: 'Delete' })
    expect(confirmButtons[confirmButtons.length - 1]).toBeDisabled()
  })

  it('deletes a category with no references', async () => {
    deleteCategoryMock.mockResolvedValue(undefined)
    render(<CategoriesPage />)
    await waitFor(() => expect(screen.getByText('Extras')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Delete Extras' }))

    expect(screen.getByText(/will be permanently removed/)).toBeInTheDocument()
    const confirmButtons = screen.getAllByRole('button', { name: 'Delete' })
    fireEvent.click(confirmButtons[confirmButtons.length - 1])

    await waitFor(() => expect(deleteCategoryMock).toHaveBeenCalledWith('c2'))
  })
})
