import { useState, type ReactNode } from 'react'
import { NavLink, useLocation } from 'react-router-dom'
import { NAV_TREE } from '../navigation/navTree'
import { getStoredSidebarCollapsed, setStoredSidebarCollapsed } from '../utils/sidebarStorage'
import './Sidebar.css'

function ToggleIcon() {
  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      width="20"
      height="20"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      <rect x="3" y="3" width="18" height="18" rx="2" />
      <line x1="9" y1="3" x2="9" y2="21" />
    </svg>
  )
}

function InvestmentsIcon() {
  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      width="20"
      height="20"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      <polyline points="3 17 9 11 13 15 21 7" />
      <polyline points="14 7 21 7 21 14" />
    </svg>
  )
}

function CashFlowIcon() {
  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      width="20"
      height="20"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      <rect x="2" y="6" width="20" height="12" rx="2" />
      <line x1="2" y1="10" x2="22" y2="10" />
    </svg>
  )
}

const CATEGORY_ICONS: Record<string, () => ReactNode> = {
  investments: InvestmentsIcon,
  cashflow: CashFlowIcon,
}

function Sidebar() {
  const [collapsed, setCollapsed] = useState(() => getStoredSidebarCollapsed())
  const location = useLocation()

  const toggleCollapsed = () => {
    const next = !collapsed
    setCollapsed(next)
    setStoredSidebarCollapsed(next)
  }

  return (
    <nav className={`sidebar${collapsed ? ' sidebar--collapsed' : ''}`} aria-label="Main">
      <button
        type="button"
        className="sidebar__toggle"
        onClick={toggleCollapsed}
        aria-label={collapsed ? 'Expand sidebar' : 'Collapse sidebar'}
        title={collapsed ? 'Expand sidebar' : 'Collapse sidebar'}
      >
        <ToggleIcon />
      </button>

      {NAV_TREE.map((category) => {
        const CategoryIcon = CATEGORY_ICONS[category.id]
        const hasActiveChild = category.children.some((child) => child.route === location.pathname)

        return (
          <div className="sidebar__category" key={category.id}>
            <div
              className={`sidebar__category-header${hasActiveChild ? ' sidebar__category-header--active' : ''}`}
            >
              <CategoryIcon />
              {!collapsed && <span className="sidebar__category-label">{category.label}</span>}
            </div>
            {!collapsed && (
              <ul className="sidebar__children" aria-label={category.label}>
                {category.children.map((child) => (
                  <li key={child.id}>
                    <NavLink to={child.route} className="sidebar__link">
                      {child.label}
                    </NavLink>
                  </li>
                ))}
              </ul>
            )}
          </div>
        )
      })}
    </nav>
  )
}

export default Sidebar
