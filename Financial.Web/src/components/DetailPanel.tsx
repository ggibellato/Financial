import { useCallback, useState } from 'react'
import { useSelectedNode } from '../context/SelectedNodeContext'
import AggregatedSummaryTab from './AggregatedSummaryTab'
import AssetSummaryTab from './AssetSummaryTab'
import TransactionsTab from './TransactionsTab'
import './DetailPanel.css'

type TabId = 'summary' | 'transactions' | 'credits'

const TABS: { id: TabId; label: string }[] = [
  { id: 'summary', label: 'Summary' },
  { id: 'transactions', label: 'Transactions' },
  { id: 'credits', label: 'Credits' },
]

function nodeKey(n: ReturnType<typeof useSelectedNode>['selectedNode']): string {
  if (!n) return ''
  return `${n.nodeType}:${n.brokerName}:${n.portfolioName ?? ''}:${n.assetName ?? ''}`
}

export default function DetailPanel() {
  const { selectedNode } = useSelectedNode()
  const [activeTab, setActiveTab] = useState<TabId>('summary')
  const [prevKey, setPrevKey] = useState('')

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
            <span
              className={`detail-panel__status detail-panel__status--${selectedNode.isActive ? 'active' : 'inactive'}`}
            >
              {selectedNode.isActive ? '● Active' : '○ Inactive'}
            </span>
          )}
        </div>
        {breadcrumb && <p className="detail-panel__breadcrumb">{breadcrumb}</p>}
      </div>

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
        {activeTab === 'summary' && !isAsset && <AggregatedSummaryTab />}
        {activeTab === 'transactions' && <TransactionsTab />}
        {activeTab === 'credits' && (
          <p className="detail-panel__placeholder">Credits — coming in F06</p>
        )}
      </div>
    </div>
  )
}
