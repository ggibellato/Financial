import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import './index.css'
import './styles/data-table.css'
import App from './App'
import InvestmentsLayout from './components/InvestmentsLayout'
import CashFlowLayout from './components/CashFlowLayout'
import ActiveInvestmentsPage from './pages/ActiveInvestmentsPage'
import HistoricInvestmentsPage from './pages/HistoricInvestmentsPage'
import DividendCheckPage from './pages/DividendCheckPage'
import CurrentValuesPage from './pages/CurrentValuesPage'
import CashFlowPlaceholderPage from './pages/CashFlowPlaceholderPage'
import ReservaPage from './pages/ReservaPage'
import RootRedirect from './pages/RootRedirect'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<App />}>
          <Route index element={<RootRedirect />} />
          <Route path="investments" element={<InvestmentsLayout />}>
            <Route index element={<Navigate to="/investments/active-investments" replace />} />
            <Route path="active-investments" element={<ActiveInvestmentsPage />} />
            <Route path="historic-investments" element={<HistoricInvestmentsPage />} />
            <Route path="dividend-check" element={<DividendCheckPage />} />
            <Route path="current-values" element={<CurrentValuesPage />} />
          </Route>
          <Route path="cashflow" element={<CashFlowLayout />}>
            <Route index element={<Navigate to="/cashflow/monthly" replace />} />
            <Route path="monthly" element={<CashFlowPlaceholderPage title="Monthly" />} />
            <Route path="reserva" element={<ReservaPage />} />
            <Route path="mensais" element={<CashFlowPlaceholderPage title="Mensais" />} />
            <Route path="controle-mae" element={<CashFlowPlaceholderPage title="Controle Mae" />} />
            <Route
              path="investment-snapshots"
              element={<CashFlowPlaceholderPage title="Investment Snapshots" />}
            />
            <Route path="yearly-summary" element={<CashFlowPlaceholderPage title="Yearly Summary" />} />
          </Route>
          <Route path="*" element={<div>Page not found.</div>} />
        </Route>
      </Routes>
    </BrowserRouter>
  </StrictMode>,
)
