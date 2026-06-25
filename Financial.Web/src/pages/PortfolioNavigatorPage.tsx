import { SelectedNodeProvider } from '../context/SelectedNodeContext'
import SplitPanel from '../components/SplitPanel'
import InvestmentTree from '../components/InvestmentTree'
import DetailPanel from '../components/DetailPanel'
import './PortfolioNavigatorPage.css'

export default function PortfolioNavigatorPage() {
  return (
    <SelectedNodeProvider>
      <div className="portfolio-navigator">
        <SplitPanel left={<InvestmentTree />} right={<DetailPanel />} />
      </div>
    </SelectedNodeProvider>
  )
}
