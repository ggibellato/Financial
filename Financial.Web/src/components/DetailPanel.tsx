import { useCallback, useState } from 'react'
import { useSelectedNode } from '../context/SelectedNodeContext'
import { POSITION_TYPE_STATUS_CLASS } from '../utils/positionType'
import AggregatedSummaryTab from './AggregatedSummaryTab'
import AssetSummaryTab from './AssetSummaryTab'
import PortfolioSummaryTab from './PortfolioSummaryTab'
import CreditsTab from './CreditsTab'
import MoveAssetDialog from './MoveAssetDialog'
import TransactionsTab from './TransactionsTab'
import PriceHistoryTab from './PriceHistoryTab'
import './DetailPanel.css'

type TabId = 'summary' | 'transactions' | 'credits' | 'priceHistory'

const TABS: { id: TabId; label: string }[] = [
  { id: 'summary', label: 'Summary' },
  { id: 'transactions', label: 'Transactions' },
  { id: 'credits', label: 'Credits' },
  { id: 'priceHistory', label: 'Price History' },
]

function nodeKey(n: ReturnType<typeof useSelectedNode>['selectedNode']): string {
  if (!n) return ''
  return `${n.nodeType}:${n.brokerName}:${n.portfolioName ?? ''}:${n.assetName ?? ''}`
}

export default function DetailPanel() {
  const { selectedNode, setSelectedNode, scope, reload } = useSelectedNode()
  const [activeTab, setActiveTab] = useState<TabId>('summary')
  const [prevKey, setPrevKey] = useState('')
  const [isMoving, setIsMoving] = useState(false)

  const currentKey = nodeKey(selectedNode)
  if (currentKey !== prevKey) {
    setPrevKey(currentKey)
    setActiveTab('summary')
  }

  const handleCopy = useCallback(() => {
    if (selectedNode?.assetName) {
      navigator.clipboard.writeText(selectedNode.assetName)
    }
  }, [selectedNode])

  if (!selectedNode) {
    return (
      <div className="detail-panel detail-panel--empty">
        <p>Select an item to view details</p>
      </div>
    )
  }

  const isAsset = selectedNode.nodeType === 'Asset'
  const isPortfolio = selectedNode.nodeType === 'Portfolio'

  const breadcrumb = isAsset
    ? `${selectedNode.ticker} · ${selectedNode.exchange} · ${selectedNode.brokerName} · ${selectedNode.portfolioName}`
    : isPortfolio
      ? selectedNode.brokerName
      : ''

  const nodeName = isAsset
    ? selectedNode.assetName ?? ''
    : isPortfolio
      ? selectedNode.portfolioName ?? ''
      : selectedNode.brokerName

  return (
    <div className="detail-panel">
      <div className="detail-panel__header">
        <div className="detail-panel__title-row">
          <span className="detail-panel__name">{nodeName}</span>
          {isAsset && (
            <button
              className="detail-panel__copy-btn"
              onClick={handleCopy}
              type="button"
              aria-label="Copy name"
              title="Copy name"
            >
              ⧉
            </button>
          )}
          {isAsset && (
            <button
              className="detail-panel__action-btn"
              onClick={() => setIsMoving(true)}
              type="button"
              title="Move to another portfolio"
            >
              Move...
            </button>
          )}
          {isAsset && selectedNode.positionType && (
            <span
              className={`detail-panel__status detail-panel__status--${POSITION_TYPE_STATUS_CLASS[selectedNode.positionType]}`}
            >
              ● {selectedNode.positionType}
            </span>
          )}
        </div>
        {breadcrumb && <p className="detail-panel__breadcrumb">{breadcrumb}</p>}
      </div>

      {isMoving && selectedNode.portfolioName && selectedNode.assetName && (
        <MoveAssetDialog
          brokerName={selectedNode.brokerName}
          portfolioName={selectedNode.portfolioName}
          assetName={selectedNode.assetName}
          scope={scope}
          onCancel={() => setIsMoving(false)}
          onMoved={(moved) => {
            setIsMoving(false)
            // Point the selection at where the asset landed before refreshing the tree, so the
            // detail panel is never left describing a portfolio the asset has left.
            setSelectedNode({ ...selectedNode, portfolioName: moved.portfolioName })
            reload()
          }}
        />
      )}

      <div className="detail-panel__tabs">
        {TABS.map((tab) => (
          <button
            key={tab.id}
            className={`detail-panel__tab${activeTab === tab.id ? ' detail-panel__tab--active' : ''}`}
            onClick={() => setActiveTab(tab.id)}
            type="button"
          >
            {tab.label}
          </button>
        ))}
      </div>

      <div className="detail-panel__content">
        {activeTab === 'summary' && isAsset && <AssetSummaryTab />}
        {activeTab === 'summary' && isPortfolio && <PortfolioSummaryTab />}
        {activeTab === 'summary' && !isAsset && !isPortfolio && <AggregatedSummaryTab />}
        {activeTab === 'transactions' && <TransactionsTab />}
        {activeTab === 'credits' && <CreditsTab />}
        {activeTab === 'priceHistory' && <PriceHistoryTab />}
      </div>
    </div>
  )
}
