import { NavLink, Outlet } from 'react-router-dom'
import './CashFlowLayout.css'

function CashFlowLayout() {
  return (
    <div className="cashflow-layout">
      <nav className="cashflow-layout__nav" aria-label="CashFlow">
        <NavLink to="/cashflow/monthly">Monthly</NavLink>
        <NavLink to="/cashflow/investment-snapshots">Investment Snapshots</NavLink>
        <NavLink to="/cashflow/yearly-summary">Yearly Summary</NavLink>
        <NavLink to="/cashflow/reserva">Reserva</NavLink>
        <NavLink to="/cashflow/mensais">Mensais</NavLink>
        <NavLink to="/cashflow/controle-mae">Controle Mae</NavLink>
      </nav>
      <div className="cashflow-layout__content">
        <Outlet />
      </div>
    </div>
  )
}

export default CashFlowLayout
