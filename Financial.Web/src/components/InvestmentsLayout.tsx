import { NavLink, Outlet } from 'react-router-dom'
import './InvestmentsLayout.css'

function InvestmentsLayout() {
  return (
    <div className="investments-layout">
      <nav className="investments-layout__nav" aria-label="Investments">
        <NavLink to="/investments/active-investments">Active Investments</NavLink>
        <NavLink to="/investments/historic-investments">Historic Investments</NavLink>
        <NavLink to="/investments/dividend-check">Shares Dividend Check</NavLink>
        <NavLink to="/investments/current-values">Read Assets Current Values</NavLink>
      </nav>
      <div className="investments-layout__content">
        <Outlet />
      </div>
    </div>
  )
}

export default InvestmentsLayout
