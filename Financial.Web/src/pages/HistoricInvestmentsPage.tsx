import { SelectedNodeProvider } from '../context/SelectedNodeContext'
import SplitPanel from '../components/SplitPanel'
import InvestmentTree from '../components/InvestmentTree'
import DetailPanel from '../components/DetailPanel'
import './HistoricInvestmentsPage.css'

export default function HistoricInvestmentsPage() {
  return (
    <SelectedNodeProvider scope="historic">
      <div className="historic-investments">
        <SplitPanel left={<InvestmentTree />} right={<DetailPanel />} />
      </div>
    </SelectedNodeProvider>
  )
}
