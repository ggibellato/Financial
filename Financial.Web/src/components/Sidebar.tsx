import { useRef, useState, type FocusEvent, type ReactNode } from 'react'
import { NavLink, useLocation } from 'react-router-dom'
import { NAV_TREE } from '../navigation/navTree'
import { getStoredSidebarCollapsed, setStoredSidebarCollapsed } from '../utils/sidebarStorage'
import SidebarFlyout from './SidebarFlyout'
import './Sidebar.css'

const CLOSE_DELAY_MS = 250

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

interface FlyoutAnchor {
  categoryId: string
  rect: DOMRect
}

function Sidebar() {
  const [collapsed, setCollapsed] = useState(() => getStoredSidebarCollapsed())
  const [flyoutAnchor, setFlyoutAnchor] = useState<FlyoutAnchor | null>(null)
  const location = useLocation()

  const triggerRefs = useRef<Record<string, HTMLDivElement | null>>({})
  const flyoutRef = useRef<HTMLDivElement | null>(null)
  const closeTimerRef = useRef<number | null>(null)
  const suppressFocusOpenRef = useRef(false)

  const toggleCollapsed = () => {
    const next = !collapsed
    setCollapsed(next)
    setStoredSidebarCollapsed(next)
  }

  const cancelClose = () => {
    if (closeTimerRef.current !== null) {
      window.clearTimeout(closeTimerRef.current)
      closeTimerRef.current = null
    }
  }

  const scheduleClose = () => {
    cancelClose()
    closeTimerRef.current = window.setTimeout(() => {
      setFlyoutAnchor(null)
    }, CLOSE_DELAY_MS)
  }

  const openFlyout = (categoryId: string, trigger: HTMLDivElement) => {
    if (suppressFocusOpenRef.current) {
      suppressFocusOpenRef.current = false
      return
    }
    cancelClose()
    setFlyoutAnchor({ categoryId, rect: trigger.getBoundingClientRect() })
  }

  const closeFlyoutNow = (categoryId: string, refocus?: boolean) => {
    cancelClose()
    setFlyoutAnchor((current) => (current?.categoryId === categoryId ? null : current))
    if (refocus) {
      suppressFocusOpenRef.current = true
      triggerRefs.current[categoryId]?.focus()
    }
  }

  const isFocusStillWithinCategory = (relatedTarget: EventTarget | null, categoryId: string) => {
    const node = relatedTarget as Node | null
    const trigger = triggerRefs.current[categoryId]
    const flyout = flyoutRef.current
    return Boolean((trigger && node && trigger.contains(node)) || (flyout && node && flyout.contains(node)))
  }

  const handleTriggerBlur = (event: FocusEvent<HTMLDivElement>, categoryId: string) => {
    if (!isFocusStillWithinCategory(event.relatedTarget, categoryId)) {
      closeFlyoutNow(categoryId)
    }
  }

  const handleFlyoutBlur = (event: FocusEvent<HTMLDivElement>, categoryId: string) => {
    if (!isFocusStillWithinCategory(event.relatedTarget, categoryId)) {
      closeFlyoutNow(categoryId)
    }
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
        const isOpen = collapsed && flyoutAnchor?.categoryId === category.id

        return (
          <div className="sidebar__category" key={category.id}>
            <div
              ref={(el) => {
                triggerRefs.current[category.id] = el
              }}
              className={`sidebar__category-header${hasActiveChild ? ' sidebar__category-header--active' : ''}`}
              tabIndex={collapsed ? 0 : -1}
              role={collapsed ? 'button' : undefined}
              aria-label={collapsed ? category.label : undefined}
              aria-haspopup={collapsed ? 'true' : undefined}
              aria-expanded={collapsed ? isOpen : undefined}
              onMouseEnter={(event) => openFlyout(category.id, event.currentTarget)}
              onMouseLeave={scheduleClose}
              onFocus={(event) => openFlyout(category.id, event.currentTarget)}
              onBlur={(event) => handleTriggerBlur(event, category.id)}
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
            {isOpen && flyoutAnchor && (
              <SidebarFlyout
                ref={flyoutRef}
                category={category}
                anchorRect={flyoutAnchor.rect}
                onClose={(refocus) => closeFlyoutNow(category.id, refocus)}
                onMouseEnter={cancelClose}
                onMouseLeave={scheduleClose}
                onBlur={(event) => handleFlyoutBlur(event, category.id)}
              />
            )}
          </div>
        )
      })}
    </nav>
  )
}

export default Sidebar
