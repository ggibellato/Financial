import { useEffect, useMemo, useState } from 'react'
import { createFinancialApiClient } from '../api/financialApiClient'
import type { AssetDetailsDto, InvestmentScope, TreeNodeDto } from '../api/types'
import { getErrorMessage } from '../utils/formatters'
import './MoveAssetDialog.css'

interface MoveAssetDialogProps {
  brokerName: string
  portfolioName: string
  assetName: string
  scope: InvestmentScope
  onCancel: () => void
  onMoved: (moved: AssetDetailsDto) => void
}

function getMetaString(metadata: Record<string, unknown>, key: string): string {
  const value = metadata[key]
  return typeof value === 'string' ? value : ''
}

/** The broker's portfolios, taken from the tree the user is already looking at. */
function portfolioNamesOf(tree: TreeNodeDto | null, brokerName: string): string[] {
  const broker = tree?.children.find(
    (child) => child.nodeType === 'Broker' && getMetaString(child.metadata, 'BrokerName') === brokerName,
  )

  return (broker?.children ?? [])
    .filter((child) => child.nodeType === 'Portfolio')
    .map((child) => getMetaString(child.metadata, 'PortfolioName'))
    .filter((name) => name.length > 0)
}

export default function MoveAssetDialog({
  brokerName,
  portfolioName,
  assetName,
  scope,
  onCancel,
  onMoved,
}: MoveAssetDialogProps) {
  const apiClient = useMemo(() => createFinancialApiClient(), [])
  const [allPortfolios, setAllPortfolios] = useState<string[]>([])
  const [createNew, setCreateNew] = useState(false)
  const [selectedPortfolio, setSelectedPortfolio] = useState('')
  const [newPortfolioName, setNewPortfolioName] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isMoving, setIsMoving] = useState(false)

  useEffect(() => {
    apiClient
      .getNavigationTree(scope)
      .then((tree) => {
        const names = portfolioNamesOf(tree, brokerName)
        setAllPortfolios(names)

        const destinations = names.filter((name) => name !== portfolioName)
        setSelectedPortfolio(destinations[0] ?? '')

        // Nowhere to move to yet means naming a portfolio is the only way forward, so start there
        // rather than on an empty list the user cannot act on.
        setCreateNew(destinations.length === 0)
      })
      .catch((err: unknown) => setError(getErrorMessage(err, 'Unable to load portfolios.')))
  }, [apiClient, scope, brokerName, portfolioName])

  const destinations = allPortfolios.filter((name) => name !== portfolioName)
  const trimmedNewName = newPortfolioName.trim()

  // Shape only: that something was chosen, or that a typed name is not blank. Whether the
  // destination is legal is the domain's to decide, and its refusal is what the user is shown -
  // re-deciding it here would put the same rule in two places and let the wordings drift apart.
  const validationMessage = createNew
    ? trimmedNewName.length === 0
      ? 'Enter a name for the new portfolio.'
      : ''
    : selectedPortfolio.length === 0
      ? 'Select the portfolio to move the asset into.'
      : ''

  const destinationPortfolioName = createNew ? trimmedNewName : selectedPortfolio
  const canMove = validationMessage.length === 0 && !isMoving

  const handleMove = async () => {
    if (!canMove) return

    setIsMoving(true)
    setError(null)
    try {
      const moved = await apiClient.moveAsset({
        brokerName,
        scope,
        sourcePortfolioName: portfolioName,
        assetName,
        destinationPortfolioName,
      })
      onMoved(moved)
    } catch (err: unknown) {
      // The reason comes from the domain, so this reads the same as the desktop app.
      setError(getErrorMessage(err, 'The asset could not be moved.'))
      setIsMoving(false)
    }
  }

  return (
    <div className="move-asset-dialog__backdrop" onClick={onCancel}>
      <div
        className="move-asset-dialog"
        role="dialog"
        aria-modal="true"
        aria-label="Move asset"
        onClick={(e) => e.stopPropagation()}
      >
        <h3 className="move-asset-dialog__title">Move Asset</h3>
        <p className="move-asset-dialog__context">
          {brokerName} / {portfolioName} / {assetName}
        </p>

        <label className="move-asset-dialog__option">
          <input
            type="radio"
            name="move-destination"
            checked={!createNew}
            disabled={destinations.length === 0}
            onChange={() => setCreateNew(false)}
          />
          Move to an existing portfolio
        </label>
        <select
          className="move-asset-dialog__input"
          aria-label="Destination portfolio"
          value={selectedPortfolio}
          disabled={createNew || destinations.length === 0}
          onChange={(e) => setSelectedPortfolio(e.target.value)}
        >
          {destinations.map((name) => (
            <option key={name} value={name}>
              {name}
            </option>
          ))}
        </select>

        <label className="move-asset-dialog__option">
          <input
            type="radio"
            name="move-destination"
            checked={createNew}
            onChange={() => setCreateNew(true)}
          />
          Move to a new portfolio
        </label>
        <input
          className="move-asset-dialog__input"
          type="text"
          aria-label="New portfolio name"
          value={newPortfolioName}
          disabled={!createNew}
          onChange={(e) => setNewPortfolioName(e.target.value)}
        />

        {(error ?? validationMessage) && (
          <p className="move-asset-dialog__error" role="alert">
            {error ?? validationMessage}
          </p>
        )}

        <div className="move-asset-dialog__actions">
          <button type="button" onClick={handleMove} disabled={!canMove}>
            Move
          </button>
          <button type="button" onClick={onCancel}>
            Cancel
          </button>
        </div>
      </div>
    </div>
  )
}
