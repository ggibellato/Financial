import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import MoveAssetDialog from '../MoveAssetDialog'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { AssetDetailsDto, TreeNodeDto } from '../../api/types'
import { ApiError } from '../../api/apiError'

const { getNavigationTreeMock, moveAssetMock, archiveAssetMock, deleteEmptyPortfolioMock } = vi.hoisted(() => ({
  getNavigationTreeMock: vi.fn(),
  moveAssetMock: vi.fn(),
  archiveAssetMock: vi.fn(),
  deleteEmptyPortfolioMock: vi.fn(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getNavigationTree: getNavigationTreeMock,
    moveAsset: moveAssetMock,
    archiveAsset: archiveAssetMock,
    deleteEmptyPortfolio: deleteEmptyPortfolioMock,
  } as Partial<FinancialApiClient>,
}))

function portfolio(name: string, assetCount?: number): TreeNodeDto {
  return {
    nodeType: 'Portfolio',
    displayName: name,
    metadata: assetCount === undefined ? { PortfolioName: name } : { PortfolioName: name, AssetCount: assetCount },
    children: [],
  }
}

/** "Default" left holding nothing by the move, which is what raises the offer. */
function treeWithEmptiedSource(): TreeNodeDto {
  return {
    nodeType: 'Investments',
    displayName: 'Root',
    metadata: {},
    children: [
      {
        nodeType: 'Broker',
        displayName: 'XPI',
        metadata: { BrokerName: 'XPI' },
        children: [portfolio('Default', 0), portfolio('ISA', 1)],
      },
    ],
  }
}

function withAsset(node: TreeNodeDto, quantity: number): TreeNodeDto {
  const asset: TreeNodeDto = {
    nodeType: 'Asset',
    displayName: 'BCIA11',
    metadata: { AssetName: 'BCIA11', Quantity: quantity },
    children: [],
  }
  return { ...node, children: [asset] }
}

/** sourceQuantity places BCIA11 in "Default" holding that quantity, as the server's tree reports it. */
function tree(portfolioNames: string[], sourceQuantity?: number): TreeNodeDto {
  const portfolios = portfolioNames.map((name) =>
    name === 'Default' && sourceQuantity !== undefined ? withAsset(portfolio(name), sourceQuantity) : portfolio(name),
  )
  return {
    nodeType: 'Investments',
    displayName: 'Root',
    metadata: {},
    children: [
      {
        nodeType: 'Broker',
        displayName: 'XPI',
        metadata: { BrokerName: 'XPI' },
        children: portfolios,
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
    offerArchive?: boolean
    presetDestination?: string
    newPortfolioOnly?: boolean
  } = {},
) {
  return render(
    <MoveAssetDialog
      brokerName="XPI"
      portfolioName="Default"
      assetName="BCIA11"
      scope="active"
      offerArchive={overrides.offerArchive ?? false}
      presetDestination={overrides.presetDestination}
      newPortfolioOnly={overrides.newPortfolioOnly}
      onCancel={overrides.onCancel ?? vi.fn()}
      onMoved={overrides.onMoved ?? vi.fn()}
    />,
  )
}

function broker(name: string, portfolios: TreeNodeDto[]): TreeNodeDto {
  return { nodeType: 'Broker', displayName: name, metadata: { BrokerName: name }, children: portfolios }
}

function root(...brokers: TreeNodeDto[]): TreeNodeDto {
  return { nodeType: 'Investments', displayName: 'Root', metadata: {}, children: brokers }
}

const closedAssetTrees = (scope?: string) =>
  Promise.resolve(scope === 'historic' ? tree(['Closed']) : tree(['Default', 'ISA', 'SIPP'], 0))

const archiveRadio = () => screen.getByRole('radio', { name: 'Archive to historic investments' })
const keepRadio = () => screen.getByRole('radio', { name: 'Keep in active investments' })
const existingRadio = () => screen.getByRole('radio', { name: 'Move to an existing portfolio' })
const newRadio = () => screen.getByRole('radio', { name: 'Move to a new portfolio' })
const destinationSelect = () => screen.getByRole('combobox', { name: 'Destination portfolio' })
const newNameBox = () => screen.getByRole('textbox', { name: 'New portfolio name' })
const optionTexts = () => screen.queryAllByRole('option').map((option) => option.textContent)

/**
 * The dialog paints its destination `<select>` immediately — empty and disabled — and fills it only
 * once `getNavigationTree` resolves. So waiting for the combobox to *exist* proves nothing: it is
 * already there on the first frame. Worse, `fireEvent.change` to an option that has not rendered yet
 * is a silent no-op in jsdom, so the select keeps its default and the test reads as though the dialog
 * ignored the choice. Wait for the options instead.
 */
async function waitForDestinations() {
  await waitFor(() => expect(screen.getAllByRole('option').length).toBeGreaterThan(0))
}

describe('MoveAssetDialog', () => {
  beforeEach(() => {
    getNavigationTreeMock.mockReset()
    moveAssetMock.mockReset()
    archiveAssetMock.mockReset()
    deleteEmptyPortfolioMock.mockReset()
    deleteEmptyPortfolioMock.mockResolvedValue(undefined)
    getNavigationTreeMock.mockResolvedValue(tree(['Default', 'ISA', 'SIPP']))
    moveAssetMock.mockResolvedValue(movedAsset('ISA'))
    archiveAssetMock.mockResolvedValue(movedAsset('Closed'))
  })

  it('offers the other portfolios of the broker, not the one the asset is in', async () => {
    renderDialog()

    await waitForDestinations()

    const options = screen.getAllByRole('option').map((option) => option.textContent)
    expect(options).toEqual(['ISA', 'SIPP'])
  })

  it('moves the asset into the selected portfolio', async () => {
    const onMoved = vi.fn()
    renderDialog({ onMoved })

    await waitForDestinations()
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

    await waitForDestinations()
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

    await waitForDestinations()
    fireEvent.click(screen.getByRole('radio', { name: 'Move to a new portfolio' }))
    fireEvent.change(screen.getByRole('textbox', { name: 'New portfolio name' }), { target: { value: 'isa' } })
    fireEvent.click(screen.getByRole('button', { name: 'Move' }))

    await waitFor(() => expect(screen.getByRole('alert')).toHaveTextContent('already has a portfolio named "ISA"'))
  })

  it('refuses a blank new name', async () => {
    renderDialog()

    await waitForDestinations()
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

    await waitForDestinations()
    fireEvent.click(screen.getByRole('button', { name: 'Move' }))

    await waitFor(() =>
      expect(screen.getByRole('alert')).toHaveTextContent('already holds an asset named "BCIA11"'),
    )
    expect(onMoved).not.toHaveBeenCalled()
  })

  it('does not offer archiving unless the asset can be archived', async () => {
    renderDialog()

    await waitForDestinations()
    expect(screen.queryByRole('radio', { name: 'Archive to historic investments' })).not.toBeInTheDocument()
  })

  it('does not offer archiving while the freshly fetched tree still shows a position', async () => {
    getNavigationTreeMock.mockResolvedValue(tree(['Default', 'ISA'], 8))
    renderDialog({ offerArchive: true })

    await waitForDestinations()
    expect(screen.queryByRole('radio', { name: 'Archive to historic investments' })).not.toBeInTheDocument()
    expect(getNavigationTreeMock).not.toHaveBeenCalledWith('historic')
  })

  it('offers Historic destinations for a closed asset, keeping one named like the source', async () => {
    // Across scopes a Historic "Default" is a different portfolio from the Active one being left.
    getNavigationTreeMock.mockImplementation((scope?: string) =>
      Promise.resolve(scope === 'historic' ? tree(['Closed', 'Default']) : tree(['Default', 'ISA', 'SIPP'], 0)),
    )
    renderDialog({ offerArchive: true })

    await waitFor(() =>
      expect(screen.getByRole('radio', { name: 'Archive to historic investments' })).toBeInTheDocument(),
    )
    fireEvent.click(screen.getByRole('radio', { name: 'Archive to historic investments' }))

    await waitFor(() =>
      expect(screen.getAllByRole('option').map((o) => o.textContent)).toEqual(['Closed', 'Default']),
    )
  })

  it('archives instead of moving when Historic is chosen', async () => {
    const onMoved = vi.fn()
    getNavigationTreeMock.mockImplementation((scope?: string) =>
      Promise.resolve(scope === 'historic' ? tree(['Closed']) : tree(['Default', 'ISA'], 0)),
    )
    renderDialog({ offerArchive: true, onMoved })

    await waitFor(() =>
      expect(screen.getByRole('radio', { name: 'Archive to historic investments' })).toBeInTheDocument(),
    )
    fireEvent.click(screen.getByRole('radio', { name: 'Archive to historic investments' }))
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
      Promise.resolve(scope === 'historic' ? tree(['Closed']) : tree(['Default', 'ISA'], 0)),
    )
    archiveAssetMock.mockRejectedValue(
      new ApiError('BCIA11 still holds a position of 8. Only a fully closed asset can be archived.', 409),
    )
    renderDialog({ offerArchive: true })

    await waitFor(() =>
      expect(screen.getByRole('radio', { name: 'Archive to historic investments' })).toBeInTheDocument(),
    )
    fireEvent.click(screen.getByRole('radio', { name: 'Archive to historic investments' }))
    await waitFor(() => expect(screen.getAllByRole('option').map((o) => o.textContent)).toEqual(['Closed']))
    fireEvent.click(screen.getByRole('button', { name: 'Move' }))

    await waitFor(() => expect(screen.getByRole('alert')).toHaveTextContent('fully closed'))
  })

  it('reports a same-scope move as not archived', async () => {
    const onMoved = vi.fn()
    renderDialog({ onMoved })

    await waitForDestinations()
    fireEvent.click(screen.getByRole('button', { name: 'Move' }))

    await waitFor(() => expect(onMoved).toHaveBeenCalled())
    expect(onMoved.mock.calls[0][1]).toBe(false)
  })

  it('offers to delete the source when the move empties it, instead of closing', async () => {
    const onMoved = vi.fn()
    getNavigationTreeMock.mockResolvedValue(treeWithEmptiedSource())
    renderDialog({ onMoved })

    await waitForDestinations()
    fireEvent.click(screen.getByRole('button', { name: 'Move' }))

    await waitFor(() => expect(screen.getByText(/is now empty/)).toBeInTheDocument())
    expect(onMoved).not.toHaveBeenCalled()
  })

  it('deletes the emptied source when asked, then reports the move', async () => {
    const onMoved = vi.fn()
    getNavigationTreeMock.mockResolvedValue(treeWithEmptiedSource())
    renderDialog({ onMoved })

    await waitForDestinations()
    fireEvent.click(screen.getByRole('button', { name: 'Move' }))
    await waitFor(() => expect(screen.getByRole('button', { name: 'Delete' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'Delete' }))

    await waitFor(() => expect(onMoved).toHaveBeenCalled())
    expect(deleteEmptyPortfolioMock).toHaveBeenCalledWith('XPI', 'Default', 'active')
  })

  it('keeps the emptied source when declined, and the move still stands', async () => {
    const onMoved = vi.fn()
    getNavigationTreeMock.mockResolvedValue(treeWithEmptiedSource())
    renderDialog({ onMoved })

    await waitForDestinations()
    fireEvent.click(screen.getByRole('button', { name: 'Move' }))
    await waitFor(() => expect(screen.getByRole('button', { name: 'Keep' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'Keep' }))

    await waitFor(() => expect(onMoved).toHaveBeenCalled())
    expect(deleteEmptyPortfolioMock).not.toHaveBeenCalled()
  })

  it('does not offer when the source still holds assets', async () => {
    const onMoved = vi.fn()
    getNavigationTreeMock.mockResolvedValue({
      nodeType: 'Investments',
      displayName: 'Root',
      metadata: {},
      children: [
        {
          nodeType: 'Broker',
          displayName: 'XPI',
          metadata: { BrokerName: 'XPI' },
          children: [portfolio('Default', 2), portfolio('ISA', 1)],
        },
      ],
    } as TreeNodeDto)
    renderDialog({ onMoved })

    await waitForDestinations()
    fireEvent.click(screen.getByRole('button', { name: 'Move' }))

    await waitFor(() => expect(onMoved).toHaveBeenCalled())
    expect(screen.queryByText(/is now empty/)).not.toBeInTheDocument()
  })

  it('a failed tidy-up does not undo the move', async () => {
    const onMoved = vi.fn()
    getNavigationTreeMock.mockResolvedValue(treeWithEmptiedSource())
    deleteEmptyPortfolioMock.mockRejectedValue(new ApiError('Portfolio "Default" still holds 1 asset(s).', 409))
    renderDialog({ onMoved })

    await waitForDestinations()
    fireEvent.click(screen.getByRole('button', { name: 'Move' }))
    await waitFor(() => expect(screen.getByRole('button', { name: 'Delete' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'Delete' }))

    await waitFor(() => expect(screen.getByRole('alert')).toHaveTextContent('still holds'))
    expect(screen.getByRole('button', { name: 'Keep' })).toBeInTheDocument()
  })

  it("offers only this broker's named portfolios when the tree holds other brokers first", async () => {
    getNavigationTreeMock.mockResolvedValue(
      root(broker('Other', [portfolio('Elsewhere')]), broker('XPI', [portfolio('Default'), portfolio('ISA'), { ...portfolio(''), metadata: {} }])),
    )
    renderDialog()

    await waitForDestinations()
    expect(optionTexts()).toEqual(['ISA'])
  })

  it("reads whether the source emptied from this broker's own source portfolio", async () => {
    const onMoved = vi.fn()
    getNavigationTreeMock.mockResolvedValue(
      root(broker('Other', [portfolio('Default', 0)]), broker('XPI', [portfolio('ISA', 0), portfolio('Default', 1)])),
    )
    renderDialog({ onMoved })

    await waitForDestinations()
    fireEvent.click(screen.getByRole('button', { name: 'Move' }))

    await waitFor(() => expect(onMoved).toHaveBeenCalled())
    expect(screen.queryByText(/is now empty/)).not.toBeInTheDocument()
  })

  it('reports the move when the tree cannot be read afterwards to check the source', async () => {
    const onMoved = vi.fn()
    getNavigationTreeMock.mockResolvedValueOnce(tree(['Default', 'ISA'])).mockRejectedValueOnce(new Error('offline'))
    renderDialog({ onMoved })

    await waitForDestinations()
    fireEvent.click(screen.getByRole('button', { name: 'Move' }))

    await waitFor(() => expect(onMoved).toHaveBeenCalled())
    expect(screen.queryByText(/is now empty/)).not.toBeInTheDocument()
  })

  it('offers nothing and starts on a new portfolio while the portfolios load', () => {
    getNavigationTreeMock.mockReturnValue(new Promise(() => undefined))
    renderDialog({ offerArchive: true })

    expect(optionTexts()).toEqual([])
    expect(newRadio()).toBeChecked()
    expect(newNameBox()).toHaveValue('')
    expect(screen.queryByRole('radio', { name: 'Archive to historic investments' })).not.toBeInTheDocument()
  })

  it('says the portfolios could not be loaded', async () => {
    getNavigationTreeMock.mockRejectedValue('offline')
    renderDialog()

    await waitFor(() => expect(screen.getByRole('alert')).toHaveTextContent('Unable to load portfolios.'))
  })

  it('reads the portfolios again for another asset', async () => {
    const { rerender } = renderDialog()
    await waitForDestinations()

    rerender(<MoveAssetDialog brokerName="XPI" portfolioName="ISA" assetName="BCIA11" scope="active" offerArchive={false} onCancel={vi.fn()} onMoved={vi.fn()} />)

    await waitFor(() => expect(optionTexts()).toEqual(['Default', 'SIPP']))
  })

  it('does not look at Historic for a closed asset unless archiving is offered', async () => {
    getNavigationTreeMock.mockResolvedValue(tree(['Default', 'ISA'], 0))
    renderDialog()

    await waitForDestinations()
    expect(getNavigationTreeMock).not.toHaveBeenCalledWith('historic')
  })

  it('offers only a new portfolio for a drop on a broker', async () => {
    renderDialog({ newPortfolioOnly: true })

    await waitFor(() => expect(getNavigationTreeMock).toHaveBeenCalled())
    await waitFor(() => expect(newRadio()).toBeChecked())
    expect(optionTexts()).toEqual([])
  })

  it('starts on an existing portfolio, switches between it and a new one, and enables only the chosen field', async () => {
    renderDialog()
    await waitForDestinations()

    expect(existingRadio()).toBeChecked()
    expect(existingRadio()).toBeEnabled()
    expect(destinationSelect()).toBeEnabled()
    expect(newNameBox()).toBeDisabled()

    fireEvent.click(newRadio())
    expect(destinationSelect()).toBeDisabled()
    expect(newNameBox()).toBeEnabled()

    fireEvent.click(existingRadio())
    expect(existingRadio()).toBeChecked()
    expect(destinationSelect()).toBeEnabled()
    expect(newNameBox()).toBeDisabled()
  })

  it('starts a closed asset on keeping it active, and Keep brings the active portfolios back', async () => {
    getNavigationTreeMock.mockImplementation(closedAssetTrees)
    renderDialog({ offerArchive: true })
    await waitFor(() => expect(archiveRadio()).toBeInTheDocument())

    expect(keepRadio()).toBeChecked()
    fireEvent.click(archiveRadio())
    await waitFor(() => expect(optionTexts()).toEqual(['Closed']))

    fireEvent.click(keepRadio())

    expect(keepRadio()).toBeChecked()
    expect(optionTexts()).toEqual(['ISA', 'SIPP'])
  })

  it('disables Move while the move runs', async () => {
    moveAssetMock.mockReturnValue(new Promise(() => undefined))
    renderDialog()
    await waitForDestinations()

    fireEvent.click(screen.getByRole('button', { name: 'Move' }))

    await waitFor(() => expect(screen.getByRole('button', { name: 'Move' })).toBeDisabled())
  })

  it('names a failed move, lets it be tried again, and clears the message while it runs again', async () => {
    moveAssetMock.mockRejectedValueOnce('offline').mockReturnValueOnce(new Promise(() => undefined))
    renderDialog()
    await waitForDestinations()

    fireEvent.click(screen.getByRole('button', { name: 'Move' }))
    await waitFor(() => expect(screen.getByRole('alert')).toHaveTextContent('The asset could not be moved.'))
    expect(screen.getByRole('button', { name: 'Move' })).toBeEnabled()

    fireEvent.click(screen.getByRole('button', { name: 'Move' }))

    await waitFor(() => expect(screen.queryByRole('alert')).not.toBeInTheDocument())
  })

  it('moves a dropped asset once, even when the move empties the source', async () => {
    getNavigationTreeMock.mockResolvedValue(treeWithEmptiedSource())
    renderDialog({ presetDestination: 'ISA' })

    expect(await screen.findByText(/is now empty/)).toBeInTheDocument()
    expect(moveAssetMock).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ destinationPortfolioName: 'ISA' }))
  })

  it('disables Delete and Keep while the emptied source is deleted', async () => {
    getNavigationTreeMock.mockResolvedValue(treeWithEmptiedSource())
    deleteEmptyPortfolioMock.mockReturnValue(new Promise(() => undefined))
    renderDialog()
    await waitForDestinations()
    fireEvent.click(screen.getByRole('button', { name: 'Move' }))
    fireEvent.click(await screen.findByRole('button', { name: 'Delete' }))

    await waitFor(() => expect(screen.getByRole('button', { name: 'Delete' })).toBeDisabled())
    expect(screen.getByRole('button', { name: 'Keep' })).toBeDisabled()
  })

  it('names a failed tidy-up and lets it be tried again', async () => {
    getNavigationTreeMock.mockResolvedValue(treeWithEmptiedSource())
    deleteEmptyPortfolioMock.mockRejectedValue('offline')
    renderDialog()
    await waitForDestinations()
    fireEvent.click(screen.getByRole('button', { name: 'Move' }))
    fireEvent.click(await screen.findByRole('button', { name: 'Delete' }))

    await waitFor(() => expect(screen.getByRole('alert')).toHaveTextContent('The portfolio could not be deleted.'))
    expect(screen.getByRole('button', { name: 'Delete' })).toBeEnabled()
  })

  it('cancels without moving anything', async () => {
    const onCancel = vi.fn()
    renderDialog({ onCancel })

    await waitForDestinations()
    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))

    expect(onCancel).toHaveBeenCalled()
    expect(moveAssetMock).not.toHaveBeenCalled()
  })

  it('renders as a modal dialog and closes on Escape, matching every other dialog in the app', async () => {
    const onCancel = vi.fn()
    renderDialog({ onCancel })

    await waitForDestinations()
    expect(screen.getByRole('dialog')).toBeInTheDocument()

    fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Escape' })

    expect(onCancel).toHaveBeenCalled()
  })
})
