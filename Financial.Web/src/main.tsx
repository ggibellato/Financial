import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter, Route, Routes } from 'react-router-dom'
import './index.css'
import './styles/data-table.css'
import App from './App'
import { PAGE_ROUTES } from './navigation/routes'
import RootRedirect from './pages/RootRedirect'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<App />}>
          <Route index element={<RootRedirect />} />
          {PAGE_ROUTES.map(({ path, element }) => (
            <Route key={path} path={path} element={element} />
          ))}
          <Route path="*" element={<div>Page not found.</div>} />
        </Route>
      </Routes>
    </BrowserRouter>
  </StrictMode>,
)
