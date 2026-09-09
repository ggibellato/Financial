import { SelectedNodeProvider } from '../context/SelectedNodeContext'
import SplitPanel from '../components/SplitPanel'
import InvestmentTree from '../components/InvestmentTree'
import DetailPanel from '../components/DetailPanel'
import type { InvestmentScope } from '../api/types'
import './InvestmentTreePage.css'

export default function InvestmentTreePage({ scope }: { scope: InvestmentScope }) {
  return (
    <SelectedNodeProvider scope={scope}>
      <div className="investment-tree-page">
        <SplitPanel left={<InvestmentTree />} right={<DetailPanel />} />
      </div>
    </SelectedNodeProvider>
  )
}
