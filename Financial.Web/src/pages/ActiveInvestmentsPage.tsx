import { SelectedNodeProvider } from '../context/SelectedNodeContext'
import SplitPanel from '../components/SplitPanel'
import InvestmentTree from '../components/InvestmentTree'
import DetailPanel from '../components/DetailPanel'
import './ActiveInvestmentsPage.css'

export default function ActiveInvestmentsPage() {
  return (
    <SelectedNodeProvider>
      <div className="active-investments">
        <SplitPanel left={<InvestmentTree />} right={<DetailPanel />} />
      </div>
    </SelectedNodeProvider>
  )
}
