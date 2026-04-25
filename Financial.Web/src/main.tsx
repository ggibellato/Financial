import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import './index.css'
import App from './App'
import BrokerDetailPage from './pages/BrokerDetailPage'
import BrokersPage from './pages/BrokersPage'
import AssetDetailPage from './pages/AssetDetailPage'
import CreditsPage from './pages/CreditsPage'
import NavigationTreePage from './pages/NavigationTreePage'
import DividendCheckPage from './pages/DividendCheckPage'
import CurrentValuesPage from './pages/CurrentValuesPage'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<App />}>
          <Route index element={<Navigate to="/brokers" replace />} />
          <Route path="brokers" element={<BrokersPage />} />
          <Route path="brokers/:brokerName" element={<BrokerDetailPage />} />
          <Route path="assets/:brokerName/:portfolioName/:assetName" element={<AssetDetailPage />} />
          <Route path="credits/:brokerName" element={<CreditsPage />} />
          <Route path="credits/:brokerName/:portfolioName" element={<CreditsPage />} />
          <Route path="navigation" element={<NavigationTreePage />} />
          <Route path="dividends-check" element={<DividendCheckPage />} />
          <Route path="current-values" element={<CurrentValuesPage />} />
          <Route path="*" element={<div>Page not found.</div>} />
        </Route>
      </Routes>
    </BrowserRouter>
  </StrictMode>,
)
