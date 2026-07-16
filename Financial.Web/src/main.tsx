import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import './index.css'
import './styles/data-table.css'
import App from './App'
import ActiveInvestmentsPage from './pages/ActiveInvestmentsPage'
import HistoricInvestmentsPage from './pages/HistoricInvestmentsPage'
import DividendCheckPage from './pages/DividendCheckPage'
import CurrentValuesPage from './pages/CurrentValuesPage'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<App />}>
          <Route index element={<Navigate to="/active-investments" replace />} />
          <Route path="active-investments" element={<ActiveInvestmentsPage />} />
          <Route path="historic-investments" element={<HistoricInvestmentsPage />} />
          <Route path="dividend-check" element={<DividendCheckPage />} />
          <Route path="current-values" element={<CurrentValuesPage />} />
          <Route path="*" element={<div>Page not found.</div>} />
        </Route>
      </Routes>
    </BrowserRouter>
  </StrictMode>,
)
