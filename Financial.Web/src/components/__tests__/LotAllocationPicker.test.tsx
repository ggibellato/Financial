import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import type { OpenLotDto } from '../../api/types'
import LotAllocationPicker from '../LotAllocationPicker'

const LOT_A: OpenLotDto = { sourceTransactionId: 'lot-a', date: '2024-06-01T00:00:00', remainingQuantity: 15, unitCost: 12.5 }
const LOT_B: OpenLotDto = { sourceTransactionId: 'lot-b', date: '2024-09-01T00:00:00', remainingQuantity: 5, unitCost: 20 }

describe('LotAllocationPicker', () => {
  it('shows a loading state', () => {
    render(
      <LotAllocationPicker
        openLots={[]}
        isLoading
        onRetry={vi.fn()}
        error={null}
        saleQuantity={5}
        allocations={{}}
        onChange={vi.fn()}
        errorMessage={null}
      />,
    )
    expect(screen.getByText('Loading open lots...')).toBeInTheDocument()
  })

  it('shows an error state', () => {
    render(
      <LotAllocationPicker
        openLots={[]}
        isLoading={false}
        onRetry={vi.fn()}
        error="Unable to load open lots"
        saleQuantity={5}
        allocations={{}}
        onChange={vi.fn()}
        errorMessage={null}
      />,
    )
    expect(screen.getByText('Unable to load open lots')).toBeInTheDocument()
  })

  it('calls onRetry when the error state retry button is clicked', () => {
    const onRetry = vi.fn()
    render(
      <LotAllocationPicker
        openLots={[]}
        isLoading={false}
        onRetry={onRetry}
        error="Unable to load open lots"
        saleQuantity={5}
        allocations={{}}
        onChange={vi.fn()}
        errorMessage={null}
      />,
    )
    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))
    expect(onRetry).toHaveBeenCalledTimes(1)
  })

  it('gives each lot allocation input a distinct accessible name', () => {
    render(
      <LotAllocationPicker
        openLots={[LOT_A, LOT_B]}
        isLoading={false}
        onRetry={vi.fn()}
        error={null}
        saleQuantity={10}
        allocations={{}}
        onChange={vi.fn()}
        errorMessage={null}
      />,
    )
    expect(screen.getByRole('spinbutton', { name: /01\/06\/2024/ })).toBeInTheDocument()
    expect(screen.getByRole('spinbutton', { name: /01\/09\/2024/ })).toBeInTheDocument()
  })

  it('marks an over-allocated input as invalid and describes the error', () => {
    render(
      <LotAllocationPicker
        openLots={[LOT_B]}
        isLoading={false}
        onRetry={vi.fn()}
        error={null}
        saleQuantity={10}
        allocations={{ 'lot-b': '8' }}
        onChange={vi.fn()}
        errorMessage={null}
      />,
    )
    const input = screen.getByRole('spinbutton')
    expect(input).toHaveAttribute('aria-invalid', 'true')
    const describedBy = input.getAttribute('aria-describedby')
    expect(describedBy).toBeTruthy()
    expect(document.getElementById(describedBy!)).toHaveTextContent(/Exceeds the 5.00000000 remaining/)
  })

  it('shows an empty message when there are no open lots', () => {
    render(
      <LotAllocationPicker
        openLots={[]}
        isLoading={false}
        onRetry={vi.fn()}
        error={null}
        saleQuantity={5}
        allocations={{}}
        onChange={vi.fn()}
        errorMessage={null}
      />,
    )
    expect(screen.getByText('No open lots available for this holding.')).toBeInTheDocument()
  })

  it('lists each open lot with its date, remaining quantity, and unit cost', () => {
    render(
      <LotAllocationPicker
        openLots={[LOT_A, LOT_B]}
        isLoading={false}
        onRetry={vi.fn()}
        error={null}
        saleQuantity={10}
        allocations={{}}
        onChange={vi.fn()}
        errorMessage={null}
      />,
    )
    expect(screen.getByText('15.00000000')).toBeInTheDocument()
    expect(screen.getByText('12.50')).toBeInTheDocument()
    expect(screen.getByText('5.00000000')).toBeInTheDocument()
    expect(screen.getByText('20.00')).toBeInTheDocument()
  })

  it('calls onChange when a lot allocation input changes', () => {
    const onChange = vi.fn()
    render(
      <LotAllocationPicker
        openLots={[LOT_A]}
        isLoading={false}
        onRetry={vi.fn()}
        error={null}
        saleQuantity={10}
        allocations={{}}
        onChange={onChange}
        errorMessage={null}
      />,
    )
    const inputs = screen.getAllByRole('spinbutton')
    fireEvent.change(inputs[0], { target: { value: '7' } })
    expect(onChange).toHaveBeenCalledWith('lot-a', '7')
  })

  it('shows a neutral prompt instead of a confusing 0-of-0 mismatch before a sale quantity is entered', () => {
    render(
      <LotAllocationPicker
        openLots={[LOT_A]}
        isLoading={false}
        onRetry={vi.fn()}
        error={null}
        saleQuantity={0}
        allocations={{}}
        onChange={vi.fn()}
        errorMessage={null}
      />,
    )
    expect(screen.getByText('Enter a sale quantity, then allocate it across the lots above.')).toBeInTheDocument()
    expect(screen.queryByText(/Allocated/)).not.toBeInTheDocument()
  })

  it('shows a mismatch summary when the allocated total does not equal the sale quantity', () => {
    render(
      <LotAllocationPicker
        openLots={[LOT_A, LOT_B]}
        isLoading={false}
        onRetry={vi.fn()}
        error={null}
        saleQuantity={10}
        allocations={{ 'lot-a': '4' }}
        onChange={vi.fn()}
        errorMessage={null}
      />,
    )
    expect(screen.getByText(/Allocated 4.00000000 of 10.00000000/)).toBeInTheDocument()
    expect(screen.getByText(/6.00000000 remaining/)).toBeInTheDocument()
  })

  it('shows a match summary when the allocated total equals the sale quantity', () => {
    render(
      <LotAllocationPicker
        openLots={[LOT_A, LOT_B]}
        isLoading={false}
        onRetry={vi.fn()}
        error={null}
        saleQuantity={10}
        allocations={{ 'lot-a': '5', 'lot-b': '5' }}
        onChange={vi.fn()}
        errorMessage={null}
      />,
    )
    expect(screen.getByText('Allocated 10.00000000 of 10.00000000')).toBeInTheDocument()
  })

  it('rejects an allocation exceeding a lot inline', () => {
    render(
      <LotAllocationPicker
        openLots={[LOT_B]}
        isLoading={false}
        onRetry={vi.fn()}
        error={null}
        saleQuantity={10}
        allocations={{ 'lot-b': '8' }}
        onChange={vi.fn()}
        errorMessage={null}
      />,
    )
    expect(screen.getByText(/Exceeds the 5.00000000 remaining on this lot/)).toBeInTheDocument()
  })

  it('shows the save-time error message when present', () => {
    render(
      <LotAllocationPicker
        openLots={[LOT_A]}
        isLoading={false}
        onRetry={vi.fn()}
        error={null}
        saleQuantity={10}
        allocations={{}}
        onChange={vi.fn()}
        errorMessage="Allocate exactly 10 across one or more lots (currently allocated: 0)"
      />,
    )
    expect(screen.getByRole('alert')).toHaveTextContent('Allocate exactly 10 across one or more lots')
  })
})
