import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import MoveAssetDialog from '../MoveAssetDialog'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { AssetDetailsDto, TreeNodeDto } from '../../api/types'
import { ApiError } from '../../api/apiError'

const getNavigationTreeMock = vi.fn()
const moveAssetMock = vi.fn()
const archiveAssetMock = vi.fn()

vi.mock('../../api/financialApiClient', () => ({
  createFinancialApiClient: (): Partial<FinancialApiClient> => ({
    getNavigationTree: getNavigationTreeMock,
    moveAsset: moveAssetMock,
    archiveAsset: archiveAssetMock,
  }),
}))

function portfolio(name: string): TreeNodeDto {
  return {
    nodeType: 'Portfolio',
    displayName: name,
    metadata: { PortfolioName: name },
    children: [],
  }
}

function tree(portfolioNames: string[]): TreeNodeDto {
  return {
    nodeType: 'Investments',
    displayName: 'Root',
    metadata: {},
    children: [
      {
        nodeType: 'Broker',
        displayName: 'XPI',
        metadata: { BrokerName: 'XPI' },
        children: portfolioNames.map(portfolio),
      },
    ],
  }
}

function movedAsset(destination: string): AssetDetailsDto {
  return { name: 'BCIA11', portfolioName: destination } as AssetDetailsDto
}

function renderDialog(
  overrides: {
    onCancel?: () => void
    onMoved?: (a: AssetDetailsDto, archived: boolean) => void
    canArchive?: boolean
  } = {},
) {
  return render(
    <MoveAssetDialog
      brokerName="XPI"
      portfolioName="Default"
      assetName="BCIA11"
      scope="active"
      canArchive={overrides.canArchive ?? false}
      onCancel={overrides.onCancel ?? vi.fn()}
      onMoved={overrides.onMoved ?? vi.fn()}
    />,
  )
}

describe('MoveAssetDialog', () => {
  beforeEach(() => {
    getNavigationTreeMock.mockReset()
    moveAssetMock.mockReset()
    archiveAssetMock.mockReset()
    getNavigationTreeMock.mockResolvedValue(tree(['Default', 'ISA', 'SIPP']))
    moveAssetMock.mockResolvedValue(movedAsset('ISA'))
    archiveAssetMock.mockResolvedValue(movedAsset('Closed'))
  })

  it('offers the other portfolios of the broker, not the one the asset is in', async () => {
    renderDialog()

    await waitFor(() => expect(screen.getByRole('combobox', { name: 'Destination portfolio' })).toBeInTheDocument())

    const options = screen.getAllByRole('option').map((option) => option.textContent)
    expect(options).toEqual(['ISA', 'SIPP'])
  })

  it('moves the asset into the selected portfolio', async () => {
    const onMoved = vi.fn()
    renderDialog({ onMoved })

    await waitFor(() => expect(screen.getByRole('combobox', { name: 'Destination portfolio' })).toBeInTheDocument())
    fireEvent.change(screen.getByRole('combobox', { name: 'Destination portfolio' }), { target: { value: 'SIPP' } })
    fireEvent.click(screen.getByRole('button', { name: 'Move' }))

    await waitFor(() => expect(onMoved).toHaveBeenCalled())
    expect(moveAssetMock).toHaveBeenCalledWith({
      brokerName: 'XPI',
      scope: 'active',
      sourcePortfolioName: 'Default',
      assetName: 'BCIA11',
      destinationPortfolioName: 'SIPP',
    })
  })

  it('moves the asset into a portfolio named here, trimming the name', async () => {
    renderDialog()

    await waitFor(() => expect(screen.getByRole('combobox', { name: 'Destination portfolio' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('radio', { name: 'Move to a new portfolio' }))
    fireEvent.change(screen.getByRole('textbox', { name: 'New portfolio name' }), { target: { value: '  Pension  ' } })
    fireEvent.click(screen.getByRole('button', { name: 'Move' }))

    await waitFor(() => expect(moveAssetMock).toHaveBeenCalled())
    expect(moveAssetMock.mock.calls[0][0].destinationPortfolioName).toBe('Pension')
  })

  it('starts on naming a new portfolio when the broker has no other one', async () => {
    getNavigationTreeMock.mockResolvedValue(tree(['Default']))

    renderDialog()

    await waitFor(() => expect(screen.getByRole('radio', { name: 'Move to a new portfolio' })).toBeChecked())
    expect(screen.getByRole('radio', { name: 'Move to an existing portfolio' })).toBeDisabled()
  })

  it('sends a name that clashes with an existing portfolio and shows the server refusal', async () => {
    // The dialog no longer second-guesses the rule; the domain owns it and its wording.
    moveAssetMock.mockRejectedValue(
      new ApiError('Broker "XPI" already has a portfolio named "ISA". Select it instead of creating another.', 409),
    )
    renderDialog()

    await waitFor(() => expect(screen.getByRole('combobox', { name: 'Destination portfolio' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('radio', { name: 'Move to a new portfolio' }))
    fireEvent.change(screen.getByRole('textbox', { name: 'New portfolio name' }), { target: { value: 'isa' } })
    fireEvent.click(screen.getByRole('button', { name: 'Move' }))

    await waitFor(() => expect(screen.getByRole('alert')).toHaveTextContent('already has a portfolio named "ISA"'))
  })

  it('refuses a blank new name', async () => {
    renderDialog()

    await waitFor(() => expect(screen.getByRole('combobox', { name: 'Destination portfolio' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('radio', { name: 'Move to a new portfolio' }))
    fireEvent.change(screen.getByRole('textbox', { name: 'New portfolio name' }), { target: { value: '   ' } })

    expect(screen.getByRole('alert')).toHaveTextContent('Enter a name')
    expect(screen.getByRole('button', { name: 'Move' })).toBeDisabled()
  })

  it("shows the server's reason when the move is refused, and does not report a move", async () => {
    const onMoved = vi.fn()
    moveAssetMock.mockRejectedValue(
      new ApiError('Portfolio "ISA" already holds an asset named "BCIA11".', 409),
    )
    renderDialog({ onMoved })

    await waitFor(() => expect(screen.getByRole('combobox', { name: 'Destination portfolio' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'Move' }))

    await waitFor(() =>
      expect(screen.getByRole('alert')).toHaveTextContent('already holds an asset named "BCIA11"'),
    )
    expect(onMoved).not.toHaveBeenCalled()
  })

  it('does not offer archiving unless the asset can be archived', async () => {
    renderDialog()

    await waitFor(() => expect(screen.getByRole('combobox', { name: 'Destination portfolio' })).toBeInTheDocument())
    expect(screen.queryByRole('radio', { name: 'Archive to Historic Investments' })).not.toBeInTheDocument()
  })

  it('offers Historic destinations for a closed asset, keeping one named like the source', async () => {
    // Across scopes a Historic "Default" is a different portfolio from the Active one being left.
    getNavigationTreeMock.mockImplementation((scope?: string) =>
      Promise.resolve(scope === 'historic' ? tree(['Closed', 'Default']) : tree(['Default', 'ISA', 'SIPP'])),
    )
    renderDialog({ canArchive: true })

    await waitFor(() =>
      expect(screen.getByRole('radio', { name: 'Archive to Historic Investments' })).toBeInTheDocument(),
    )
    fireEvent.click(screen.getByRole('radio', { name: 'Archive to Historic Investments' }))

    await waitFor(() =>
      expect(screen.getAllByRole('option').map((o) => o.textContent)).toEqual(['Closed', 'Default']),
    )
  })

  it('archives instead of moving when Historic is chosen', async () => {
    const onMoved = vi.fn()
    getNavigationTreeMock.mockImplementation((scope?: string) =>
      Promise.resolve(scope === 'historic' ? tree(['Closed']) : tree(['Default', 'ISA'])),
    )
    renderDialog({ canArchive: true, onMoved })

    await waitFor(() =>
      expect(screen.getByRole('radio', { name: 'Archive to Historic Investments' })).toBeInTheDocument(),
    )
    fireEvent.click(screen.getByRole('radio', { name: 'Archive to Historic Investments' }))
    await waitFor(() => expect(screen.getAllByRole('option').map((o) => o.textContent)).toEqual(['Closed']))
    fireEvent.click(screen.getByRole('button', { name: 'Move' }))

    await waitFor(() => expect(onMoved).toHaveBeenCalled())
    expect(moveAssetMock).not.toHaveBeenCalled()
    expect(archiveAssetMock).toHaveBeenCalledWith({
      brokerName: 'XPI',
      sourcePortfolioName: 'Default',
      assetName: 'BCIA11',
      destinationPortfolioName: 'Closed',
    })
    expect(onMoved.mock.calls[0][1]).toBe(true)
  })

  it("shows the server's reason when an archive is refused", async () => {
    getNavigationTreeMock.mockImplementation((scope?: string) =>
      Promise.resolve(scope === 'historic' ? tree(['Closed']) : tree(['Default', 'ISA'])),
    )
    archiveAssetMock.mockRejectedValue(
      new ApiError('BCIA11 still holds a position of 8. Only a fully closed asset can be archived.', 409),
    )
    renderDialog({ canArchive: true })

    await waitFor(() =>
      expect(screen.getByRole('radio', { name: 'Archive to Historic Investments' })).toBeInTheDocument(),
    )
    fireEvent.click(screen.getByRole('radio', { name: 'Archive to Historic Investments' }))
    await waitFor(() => expect(screen.getAllByRole('option').map((o) => o.textContent)).toEqual(['Closed']))
    fireEvent.click(screen.getByRole('button', { name: 'Move' }))

    await waitFor(() => expect(screen.getByRole('alert')).toHaveTextContent('fully closed'))
  })

  it('reports a same-scope move as not archived', async () => {
    const onMoved = vi.fn()
    renderDialog({ onMoved })

    await waitFor(() => expect(screen.getByRole('combobox', { name: 'Destination portfolio' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'Move' }))

    await waitFor(() => expect(onMoved).toHaveBeenCalled())
    expect(onMoved.mock.calls[0][1]).toBe(false)
  })

  it('cancels without moving anything', async () => {
    const onCancel = vi.fn()
    renderDialog({ onCancel })

    await waitFor(() => expect(screen.getByRole('combobox', { name: 'Destination portfolio' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))

    expect(onCancel).toHaveBeenCalled()
    expect(moveAssetMock).not.toHaveBeenCalled()
  })
})
