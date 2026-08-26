import { useEffect, useRef, useState } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { AssetDetailsDto, InvestmentScope, TreeNodeDto } from '../api/types'
import { getErrorMessage } from '../utils/formatters'
import './MoveAssetDialog.css'

interface MoveAssetDialogProps {
  brokerName: string
  portfolioName: string
  assetName: string
  scope: InvestmentScope
  /** Only a closed position in Active Investments can be archived. */
  canArchive: boolean
  /**
   * Set by a drop on a portfolio: the destination is already known, so the move runs straight away
   * and the only thing left to show is the offer to tidy up an emptied source.
   */
  presetDestination?: string
  /**
   * Set by a drop on a broker, which means "into a new portfolio here". Offering no existing
   * destination is what leaves naming as the only route.
   */
  newPortfolioOnly?: boolean
  onCancel: () => void
  /** archived is true when the asset left this scope for Historic Investments. */
  onMoved: (moved: AssetDetailsDto, archived: boolean) => void
}

function getMetaString(metadata: Record<string, unknown>, key: string): string {
  const value = metadata[key]
  return typeof value === 'string' ? value : ''
}

function getMetaNumber(metadata: Record<string, unknown>, key: string): number {
  const value = metadata[key]
  return typeof value === 'number' ? value : -1
}

/** The broker's portfolios in one scope, taken from the tree that scope is rendered from. */
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
  canArchive,
  presetDestination,
  newPortfolioOnly = false,
  onCancel,
  onMoved,
}: MoveAssetDialogProps) {
  const [samePortfolios, setSamePortfolios] = useState<string[]>([])
  const [historicPortfolios, setHistoricPortfolios] = useState<string[]>([])
  const [archiveToHistoric, setArchiveToHistoric] = useState(false)
  const [createNew, setCreateNew] = useState(false)
  const [selectedPortfolio, setSelectedPortfolio] = useState('')
  const [newPortfolioName, setNewPortfolioName] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSaving, setIsSaving] = useState(false)
  // Set once the move has succeeded and left the source portfolio holding nothing. The dialog then
  // asks whether to remove it, rather than closing and leaving an empty portfolio behind.
  const [emptiedSource, setEmptiedSource] = useState<AssetDetailsDto | null>(null)

  useEffect(() => {
    // Every Historic portfolio is a candidate, including one named like the source: across scopes
    // that is a different portfolio, not the one the asset is already in.
    const trees = canArchive
      ? Promise.all([apiClient.getNavigationTree(scope), apiClient.getNavigationTree('historic')])
      : apiClient.getNavigationTree(scope).then((tree) => [tree, null] as const)

    Promise.resolve(trees)
      .then(([sameScopeTree, historicTree]) => {
        setSamePortfolios(
          newPortfolioOnly
            ? []
            : portfolioNamesOf(sameScopeTree, brokerName).filter((name) => name !== portfolioName),
        )
        setHistoricPortfolios(historicTree ? portfolioNamesOf(historicTree, brokerName) : [])
      })
      .catch((err: unknown) => setError(getErrorMessage(err, 'Unable to load portfolios.')))
  }, [scope, brokerName, portfolioName, canArchive, newPortfolioOnly])

  const destinations = archiveToHistoric ? historicPortfolios : samePortfolios

  // Derived, not reset by an effect. The portfolio lists arrive asynchronously, and an effect that
  // re-defaulted the choice when they landed would overwrite a selection the user had already made
  // in the meantime.
  const selected = destinations.includes(selectedPortfolio) ? selectedPortfolio : (destinations[0] ?? '')

  // Nothing to choose from means naming a portfolio is the only way forward, so the dialog settles
  // there rather than on an empty list the user cannot act on.
  const createNewPortfolio = destinations.length === 0 || createNew

  const trimmedNewName = newPortfolioName.trim()

  // Shape only: that something was chosen, or that a typed name is not blank. Whether the
  // destination is legal is the domain's to decide, and its refusal is what the user is shown.
  const validationMessage = createNewPortfolio
    ? trimmedNewName.length === 0
      ? 'Enter a name for the new portfolio.'
      : ''
    : selected.length === 0
      ? 'Select the portfolio to move the asset into.'
      : ''

  const destinationPortfolioName = createNewPortfolio ? trimmedNewName : selected
  const canSubmit = validationMessage.length === 0 && !isSaving

  /** Switching scope starts that scope's choice fresh rather than carrying the other one's over. */
  const selectScope = (toHistoric: boolean) => {
    setArchiveToHistoric(toHistoric)
    setSelectedPortfolio('')
    setCreateNew(false)
  }

  const handleSubmit = async (destination = destinationPortfolioName) => {
    if (destination.length === 0 || isSaving) return

    setIsSaving(true)
    setError(null)
    try {
      const moved = archiveToHistoric
        ? await apiClient.archiveAsset({
            brokerName,
            sourcePortfolioName: portfolioName,
            assetName,
            destinationPortfolioName: destination,
          })
        : await apiClient.moveAsset({
            brokerName,
            scope,
            sourcePortfolioName: portfolioName,
            assetName,
            destinationPortfolioName: destination,
          })
      // Whether the source is now empty comes from the tree, which already carries the count -
      // no extra field on the move response, and the freshest answer available.
      const sourceIsEmpty = await isSourceEmptyAsync()
      if (sourceIsEmpty) {
        setEmptiedSource(moved)
        setIsSaving(false)
        return
      }

      onMoved(moved, archiveToHistoric)
    } catch (err: unknown) {
      // The reason comes from the domain, so this reads the same as the desktop app.
      setError(getErrorMessage(err, 'The asset could not be moved.'))
      setIsSaving(false)
    }
  }

  const isSourceEmptyAsync = async () => {
    try {
      const tree = await apiClient.getNavigationTree(scope)
      const broker = tree.children.find(
        (child) => child.nodeType === 'Broker' && getMetaString(child.metadata, 'BrokerName') === brokerName,
      )
      const source = broker?.children.find(
        (child) => getMetaString(child.metadata, 'PortfolioName') === portfolioName,
      )
      // -1 when absent, so an unknown count never reads as empty.
      const count = source ? getMetaNumber(source.metadata, 'AssetCount') : -1
      return count === 0
    } catch {
      // The move already succeeded; failing to work out whether to tidy up must not undo it.
      return false
    }
  }

  // A drop on a portfolio already answered "where should this go", so there is nothing to ask.
  const hasSubmittedPreset = useRef(false)
  useEffect(() => {
    if (presetDestination && !hasSubmittedPreset.current) {
      hasSubmittedPreset.current = true
      void handleSubmit(presetDestination)
    }
  })

  const handleDeleteEmptiedSource = async () => {
    if (!emptiedSource) return

    setIsSaving(true)
    try {
      await apiClient.deleteEmptyPortfolio(brokerName, portfolioName, scope)
      onMoved(emptiedSource, archiveToHistoric)
    } catch (err: unknown) {
      // The move stands either way; only the tidy-up failed.
      setError(getErrorMessage(err, 'The portfolio could not be deleted.'))
      setIsSaving(false)
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

        {presetDestination && !emptiedSource ? (
          <p className="move-asset-dialog__prompt">Moving…</p>
        ) : emptiedSource ? (
          <>
            <p className="move-asset-dialog__prompt">
              &ldquo;{portfolioName}&rdquo; is now empty. Delete it?
            </p>
            {error && (
              <p className="move-asset-dialog__error" role="alert">
                {error}
              </p>
            )}
            <div className="move-asset-dialog__actions">
              <button type="button" onClick={handleDeleteEmptiedSource} disabled={isSaving}>
                Delete
              </button>
              <button type="button" onClick={() => onMoved(emptiedSource, archiveToHistoric)} disabled={isSaving}>
                Keep
              </button>
            </div>
          </>
        ) : (
        <>
        {canArchive && (
          <fieldset className="move-asset-dialog__scope">
            <legend className="move-asset-dialog__legend">Destination</legend>
            <label className="move-asset-dialog__option">
              <input
                type="radio"
                name="move-scope"
                checked={!archiveToHistoric}
                onChange={() => selectScope(false)}
              />
              Keep in Active Investments
            </label>
            <label className="move-asset-dialog__option">
              <input
                type="radio"
                name="move-scope"
                checked={archiveToHistoric}
                onChange={() => selectScope(true)}
              />
              Archive to Historic Investments
            </label>
          </fieldset>
        )}

        <label className="move-asset-dialog__option">
          <input
            type="radio"
            name="move-destination"
            checked={!createNewPortfolio}
            disabled={destinations.length === 0}
            onChange={() => setCreateNew(false)}
          />
          Move to an existing portfolio
        </label>
        <select
          className="move-asset-dialog__input"
          aria-label="Destination portfolio"
          value={selected}
          disabled={createNewPortfolio || destinations.length === 0}
          onChange={(e) => setSelectedPortfolio(e.target.value)}
        >
          {destinations.map((name) => (
            <option key={name} value={name}>
              {name}
            </option>
          ))}
        </select>

        <label className="move-asset-dialog__option">
          <input type="radio" name="move-destination" checked={createNewPortfolio} onChange={() => setCreateNew(true)} />
          Move to a new portfolio
        </label>
        <input
          className="move-asset-dialog__input"
          type="text"
          aria-label="New portfolio name"
          value={newPortfolioName}
          disabled={!createNewPortfolio}
          onChange={(e) => setNewPortfolioName(e.target.value)}
        />

        {(error ?? validationMessage) && (
          <p className="move-asset-dialog__error" role="alert">
            {error ?? validationMessage}
          </p>
        )}

        <div className="move-asset-dialog__actions">
          <button type="button" onClick={() => handleSubmit()} disabled={!canSubmit}>
            Move
          </button>
          <button type="button" onClick={onCancel}>
            Cancel
          </button>
        </div>
        </>
        )}
      </div>
    </div>
  )
}
