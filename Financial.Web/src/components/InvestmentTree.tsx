import { useCallback, useEffect, useRef, useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { Tree, TreeItem, TreeItemLayout } from '@fluentui/react-components'
import type { TreeItemValue, TreeOpenChangeData, TreeOpenChangeEvent } from '@fluentui/react-components'
import { apiClient } from '../api/financialApiClient'
import type { PositionType, SelectedNode, TreeNodeDto } from '../api/types'
import { useSelectedNode } from '../context/SelectedNodeContext'
import { getErrorMessage } from '../utils/formatters'
import { findAssetInTree, readPendingSelection } from '../utils/holdingNavigation'
import { POSITION_TYPE_STATUS_CLASS } from '../utils/positionType'
import { getMetaNumber, getMetaString } from '../utils/treeNodeMetadata'
import ErrorState from './ErrorState'
import MoveAssetDialog from './MoveAssetDialog'
import LoadingState from './LoadingState'
import './InvestmentTree.css'

const ASSET_CLASS_OPTIONS: { value: number; label: string }[] = [
  { value: 1, label: 'Equity' },
  { value: 2, label: 'Real Estate' },
  { value: 3, label: 'Bond' },
  { value: 4, label: 'Fund' },
  { value: 5, label: 'ETF' },
  { value: 6, label: 'Cash' },
  { value: 7, label: 'Pension' },
  { value: 8, label: 'Other' },
  { value: 9, label: 'Cryptocurrency' },
]

const ALL_CLASSES = 'all'

export interface DraggedAsset {
  brokerName: string
  portfolioName: string
  assetName: string
}

export interface AssetDrop extends DraggedAsset {
  destinationPortfolioName?: string
}

const DRAG_MIME = 'application/x-financial-asset'

function getMetaPositionType(metadata: Record<string, unknown>): PositionType {
  const v = metadata['PositionType']
  return v === 'Long' || v === 'Short' ? v : 'Flat'
}

function buildAssetSelection(node: TreeNodeDto, brokerName: string, portfolioName: string): SelectedNode {
  const assetClass = getMetaNumber(node.metadata, 'GlobalAssetClass')
  return {
    nodeType: 'Asset',
    brokerName,
    portfolioName,
    assetName: getMetaString(node.metadata, 'AssetName'),
    ticker: getMetaString(node.metadata, 'Ticker'),
    exchange: getMetaString(node.metadata, 'Exchange'),
    positionType: getMetaPositionType(node.metadata),
    // getMetaNumber yields -1 when absent, so a missing quantity never reads as a closed position.
    quantity: getMetaNumber(node.metadata, 'Quantity'),
    assetClass: ASSET_CLASS_OPTIONS.find((o) => o.value === assetClass)?.label,
  }
}

interface NodeMatch {
  brokerName: string
  portfolioName?: string
}

function nodeMatchesSelected(selected: SelectedNode | null, nodeType: string, match: NodeMatch & { assetName?: string }): boolean {
  if (!selected) return false
  if (selected.nodeType !== nodeType) return false
  if (selected.brokerName !== match.brokerName) return false
  if (nodeType === 'Portfolio' && selected.portfolioName !== match.portfolioName) return false
  if (nodeType === 'Asset' && (selected.portfolioName !== match.portfolioName || selected.assetName !== match.assetName)) return false
  return true
}

interface DragContext {
  dragged: DraggedAsset | null
  setDragged: (asset: DraggedAsset | null) => void
  onDrop: (drop: AssetDrop) => void
}

/**
 * Narrows what the tree offers; it does not decide the move. A drop that looks fine here can still
 * be refused by the server, and that refusal is what the user is shown.
 *
 * A broker is always a valid target, even the one the asset already sits under: dropping there
 * means "into a new portfolio here", the only route to a portfolio that does not exist yet.
 */
function canAccept(dragged: DraggedAsset | null, brokerName: string, portfolioName?: string): boolean {
  if (!dragged || dragged.brokerName !== brokerName) return false
  return portfolioName === undefined || portfolioName !== dragged.portfolioName
}

interface AssetNodeProps {
  node: TreeNodeDto
  brokerName: string
  portfolioName: string
  filterClass: string
  drag: DragContext
}

function AssetNode({ node, brokerName, portfolioName, filterClass, drag }: AssetNodeProps) {
  const { selectedNode, setSelectedNode } = useSelectedNode()
  const assetName = getMetaString(node.metadata, 'AssetName')
  const positionType = getMetaPositionType(node.metadata)
  const assetClass = getMetaNumber(node.metadata, 'GlobalAssetClass')

  if (filterClass !== ALL_CLASSES && String(assetClass) !== filterClass) return null

  const isSelected = nodeMatchesSelected(selectedNode, 'Asset', { brokerName, portfolioName, assetName })
  const statusClass = POSITION_TYPE_STATUS_CLASS[positionType]

  const handleClick = () => {
    setSelectedNode(buildAssetSelection(node, brokerName, portfolioName))
  }

  return (
    <TreeItem
      itemType="leaf"
      value={`asset:${brokerName}:${portfolioName}:${assetName}`}
      aria-selected={isSelected}
      draggable
      onDragStart={(e) => {
        // The payload has to be set for the drag to start at all; the context carries the detail.
        e.dataTransfer.setData(DRAG_MIME, assetName)
        e.dataTransfer.effectAllowed = 'move'
        drag.setDragged({ brokerName, portfolioName, assetName })
      }}
      onDragEnd={() => drag.setDragged(null)}
    >
      <TreeItemLayout
        className={`investment-tree__node investment-tree__node--asset${isSelected ? ' investment-tree__node--selected' : ''}`}
        iconBefore={
          <span
            className={`investment-tree__status-icon investment-tree__status-icon--${statusClass}`}
            role="img"
            aria-label={positionType}
            title={positionType}
          >
            ●
          </span>
        }
        onClick={handleClick}
      >
        {node.displayName}
      </TreeItemLayout>
    </TreeItem>
  )
}

interface PortfolioNodeProps {
  node: TreeNodeDto
  brokerName: string
  filterClass: string
  drag: DragContext
}

function PortfolioNode({ node, brokerName, filterClass, drag }: PortfolioNodeProps) {
  const { selectedNode, setSelectedNode } = useSelectedNode()
  const [isDropTarget, setIsDropTarget] = useState(false)
  const portfolioName = getMetaString(node.metadata, 'PortfolioName')
  // -1 when absent, so a portfolio whose count is unknown is never offered for deletion.
  const assetCount = getMetaNumber(node.metadata, 'AssetCount')

  const visibleAssets = node.children.filter((child) => {
    if (child.nodeType !== 'Asset') return false
    if (filterClass === ALL_CLASSES) return true
    return String(getMetaNumber(child.metadata, 'GlobalAssetClass')) === filterClass
  })

  if (filterClass !== ALL_CLASSES && visibleAssets.length === 0) return null

  const isSelected = nodeMatchesSelected(selectedNode, 'Portfolio', { brokerName, portfolioName })

  const handleClick = () => {
    setSelectedNode({ nodeType: 'Portfolio', brokerName, portfolioName, assetCount })
  }

  const accepts = canAccept(drag.dragged, brokerName, portfolioName)
  const hasVisibleChildren = visibleAssets.length > 0

  return (
    <TreeItem
      itemType={hasVisibleChildren ? 'branch' : 'leaf'}
      value={`portfolio:${brokerName}:${portfolioName}`}
      aria-selected={isSelected}
    >
      <TreeItemLayout
        className={`investment-tree__node${isSelected ? ' investment-tree__node--selected' : ''}${
          isDropTarget ? ' investment-tree__row--drop-target' : ''
        }`}
        onClick={handleClick}
        // preventDefault only for a target that would take it: that is what makes an illegal drop
        // genuinely refuse rather than merely fail afterwards.
        onDragOver={(e) => {
          if (!accepts) return
          e.preventDefault()
          e.dataTransfer.dropEffect = 'move'
          setIsDropTarget(true)
        }}
        onDragLeave={() => setIsDropTarget(false)}
        onDrop={(e) => {
          setIsDropTarget(false)
          if (!accepts || !drag.dragged) return
          e.preventDefault()
          e.stopPropagation()
          drag.onDrop({ ...drag.dragged, destinationPortfolioName: portfolioName })
        }}
      >
        {node.displayName}
      </TreeItemLayout>
      {hasVisibleChildren && (
        <Tree>
          {node.children.map((child) =>
            child.nodeType === 'Asset' ? (
              <AssetNode
                key={child.displayName}
                node={child}
                brokerName={brokerName}
                portfolioName={portfolioName}
                filterClass={filterClass}
                drag={drag}
              />
            ) : null,
          )}
        </Tree>
      )}
    </TreeItem>
  )
}

interface BrokerNodeProps {
  node: TreeNodeDto
  filterClass: string
  drag: DragContext
}

function BrokerNode({ node, filterClass, drag }: BrokerNodeProps) {
  const { selectedNode, setSelectedNode } = useSelectedNode()
  const [isDropTarget, setIsDropTarget] = useState(false)
  const brokerName = getMetaString(node.metadata, 'BrokerName')
  const currency = getMetaString(node.metadata, 'Currency')

  const visiblePortfolios = node.children.filter((child) => {
    if (child.nodeType !== 'Portfolio') return false
    if (filterClass === ALL_CLASSES) return true
    return child.children.some(
      (asset) => asset.nodeType === 'Asset' && String(getMetaNumber(asset.metadata, 'GlobalAssetClass')) === filterClass,
    )
  })

  if (filterClass !== ALL_CLASSES && visiblePortfolios.length === 0) return null

  const isSelected = nodeMatchesSelected(selectedNode, 'Broker', { brokerName })

  const handleClick = () => {
    setSelectedNode({ nodeType: 'Broker', brokerName, currency })
  }

  const accepts = canAccept(drag.dragged, brokerName)
  const hasVisibleChildren = visiblePortfolios.length > 0

  return (
    <TreeItem
      itemType={hasVisibleChildren ? 'branch' : 'leaf'}
      value={`broker:${brokerName}`}
      aria-selected={isSelected}
    >
      <TreeItemLayout
        className={`investment-tree__node investment-tree__node--broker${isSelected ? ' investment-tree__node--selected' : ''}${
          isDropTarget ? ' investment-tree__row--drop-target' : ''
        }`}
        onClick={handleClick}
        onDragOver={(e) => {
          if (!accepts) return
          e.preventDefault()
          e.dataTransfer.dropEffect = 'move'
          setIsDropTarget(true)
        }}
        onDragLeave={() => setIsDropTarget(false)}
        onDrop={(e) => {
          setIsDropTarget(false)
          if (!accepts || !drag.dragged) return
          e.preventDefault()
          // No destination: dropping on the broker means a portfolio that does not exist yet.
          drag.onDrop({ ...drag.dragged })
        }}
      >
        {node.displayName}
      </TreeItemLayout>
      {hasVisibleChildren && (
        <Tree>
          {node.children.map((child) =>
            child.nodeType === 'Portfolio' ? (
              <PortfolioNode key={child.displayName} node={child} brokerName={brokerName} filterClass={filterClass} drag={drag} />
            ) : null,
          )}
        </Tree>
      )}
    </TreeItem>
  )
}

export default function InvestmentTree() {
  const { scope, reloadToken, reload, setSelectedNode } = useSelectedNode()
  const [tree, setTree] = useState<TreeNodeDto | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [retryCount, setRetryCount] = useState(0)
  const [filterClass, setFilterClass] = useState(ALL_CLASSES)
  const [dragged, setDragged] = useState<DraggedAsset | null>(null)
  const [drop, setDrop] = useState<AssetDrop | null>(null)
  // Brokers start expanded, portfolios start collapsed - matching the pre-migration per-node
  // useState(true)/useState(false) defaults. Only the root Tree can be controlled; nested Tree
  // elements automatically participate via Fluent's own context.
  const [openItems, setOpenItems] = useState<Set<TreeItemValue>>(new Set())
  const [scrollToken, setScrollToken] = useState(0)
  const containerRef = useRef<HTMLDivElement>(null)
  const { pathname, state: routerState } = useLocation()
  const navigate = useNavigate()
  // The pending selection is read where the tree resolves, so it is held in a ref rather than a
  // dependency: adding router state to the fetch effect would re-fetch the tree when it is cleared.
  const routerStateRef = useRef<unknown>(routerState)

  useEffect(() => {
    routerStateRef.current = routerState
  }, [routerState])

  useEffect(() => {
    apiClient
      .getNavigationTree(scope)
      .then((data) => {
        setTree(data)
        setError(null)
        const brokerKeys = data.children
          .filter((child) => child.nodeType === 'Broker')
          .map((child) => `broker:${getMetaString(child.metadata, 'BrokerName')}`)

        const pending = readPendingSelection(routerStateRef.current)
        const asset = pending ? findAssetInTree(data, pending) : null
        if (pending && asset) {
          setSelectedNode(buildAssetSelection(asset, pending.brokerName, pending.portfolioName))
          setOpenItems(
            new Set([
              ...brokerKeys,
              `broker:${pending.brokerName}`,
              `portfolio:${pending.brokerName}:${pending.portfolioName}`,
            ]),
          )
          setScrollToken((token) => token + 1)
        } else {
          setOpenItems(new Set(brokerKeys))
        }

        // Consumed state has to go, or browser back/forward would re-apply the same selection.
        if (pending) navigate(pathname, { replace: true, state: null })
      })
      .catch((err: unknown) => {
        setError(getErrorMessage(err, 'Unable to load investments.'))
      })
      .finally(() => setIsLoading(false))
  }, [scope, retryCount, reloadToken, pathname, navigate, setSelectedNode])

  useEffect(() => {
    if (scrollToken === 0) return
    const selected = containerRef.current?.querySelector('.investment-tree__node--selected')
    selected?.scrollIntoView({ block: 'nearest' })
    // Move focus too, not just scroll - a keyboard/screen-reader user gets no other signal that a
    // click-through from another page landed on this node.
    selected?.closest<HTMLElement>('[role="treeitem"]')?.focus()
  }, [scrollToken])

  const handleRetry = useCallback(() => {
    setIsLoading(true)
    setError(null)
    setRetryCount((c) => c + 1)
  }, [])

  const handleOpenChange = (_event: TreeOpenChangeEvent, data: TreeOpenChangeData) => {
    setOpenItems(new Set(data.openItems))
  }

  return (
    <div className="investment-tree" ref={containerRef}>
      <h2 className="investment-tree__heading">Investments</h2>
      <div className="investment-tree__filter">
        <label htmlFor="asset-class-filter" className="investment-tree__filter-label">
          Asset class
        </label>
        <select
          id="asset-class-filter"
          className="investment-tree__filter-select"
          value={filterClass}
          onChange={(e) => setFilterClass(e.target.value)}
        >
          <option value={ALL_CLASSES}>All</option>
          {ASSET_CLASS_OPTIONS.map((opt) => (
            <option key={opt.value} value={String(opt.value)}>
              {opt.label}
            </option>
          ))}
        </select>
      </div>
      {drop && (
        // A drop routes through the same dialog a menu move does, so the two cannot answer
        // differently: with a destination it moves straight away, without one it asks for a name.
        <MoveAssetDialog
          brokerName={drop.brokerName}
          portfolioName={drop.portfolioName}
          assetName={drop.assetName}
          scope={scope}
          canArchive={false}
          presetDestination={drop.destinationPortfolioName}
          newPortfolioOnly={drop.destinationPortfolioName === undefined}
          onCancel={() => setDrop(null)}
          onMoved={(moved) => {
            setDrop(null)
            setSelectedNode({
              nodeType: 'Asset',
              brokerName: drop.brokerName,
              portfolioName: moved.portfolioName,
              assetName: drop.assetName,
            })
            reload()
          }}
        />
      )}
      {isLoading ? (
        <LoadingState message="Loading investments..." />
      ) : error ? (
        <ErrorState message={error} onRetry={handleRetry} />
      ) : tree ? (
        <Tree className="investment-tree__list" aria-label="Investments" openItems={openItems} onOpenChange={handleOpenChange}>
          {tree.children.map((child) =>
            child.nodeType === 'Broker' ? (
              <BrokerNode key={child.displayName} node={child} filterClass={filterClass} drag={{ dragged, setDragged, onDrop: setDrop }} />
            ) : null,
          )}
        </Tree>
      ) : null}
    </div>
  )
}
