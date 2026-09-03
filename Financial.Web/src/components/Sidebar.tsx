import { useRef, useState, type FocusEvent, type KeyboardEvent, type ReactNode } from 'react'
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

function AdminIcon() {
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
      <path d="M12,2 L14,5 L17.5,4.5 L18,8 L21,10 L19,13 L21,16 L18,18 L17.5,21.5 L14,21 L12,24 L10,21 L6.5,21.5 L6,18 L3,16 L5,13 L3,10 L6,8 L6.5,4.5 L10,5 Z" />
    </svg>
  )
}

function SettingsIcon() {
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
      <circle cx="12" cy="12" r="3" />
      <path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 1 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 1 1-2.83-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 1 1 2.83-2.83l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 1 1 2.83 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z" />
    </svg>
  )
}

const CATEGORY_ICONS: Record<string, () => ReactNode> = {
  investments: InvestmentsIcon,
  cashflow: CashFlowIcon,
  admin: AdminIcon,
  settings: SettingsIcon,
}

interface FlyoutAnchor {
  categoryId: string
  rect: DOMRect
}

function Sidebar() {
  const [collapsed, setCollapsed] = useState(() => getStoredSidebarCollapsed())
  const [flyoutAnchor, setFlyoutAnchor] = useState<FlyoutAnchor | null>(null)
  const [expandedGroupId, setExpandedGroupId] = useState<string | null>(null)
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
        const hasActiveChild =
          category.children.some((child) => child.route === location.pathname) ||
          (category.groups ?? []).some((group) => group.children.some((child) => child.route === location.pathname))
        const isOpen = collapsed && flyoutAnchor?.categoryId === category.id

        return (
          <div className="sidebar__category" key={category.id}>
            {category.id === 'admin' && <hr className="sidebar__divider" />}
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
            {!collapsed && category.groups && (
              <ul className="sidebar__groups" aria-label={category.label}>
                {category.groups.map((group) => {
                  const groupHasActiveChild = group.children.some((child) => child.route === location.pathname)
                  const groupExpanded = expandedGroupId === group.id
                  const toggleGroup = () =>
                    setExpandedGroupId((current) => (current === group.id ? null : group.id))
                  const handleGroupKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
                    if (event.key === 'Enter' || event.key === ' ') {
                      event.preventDefault()
                      toggleGroup()
                    }
                  }

                  return (
                    <li key={group.id} className="sidebar__group">
                      <div
                        className={`sidebar__group-header${groupHasActiveChild ? ' sidebar__group-header--active' : ''}`}
                        role="button"
                        tabIndex={0}
                        aria-expanded={groupExpanded}
                        onClick={toggleGroup}
                        onKeyDown={handleGroupKeyDown}
                      >
                        <span className="sidebar__group-disclosure" aria-hidden="true">
                          {groupExpanded ? '▾' : '▸'}
                        </span>
                        <span>{group.label}</span>
                      </div>
                      {groupExpanded && (
                        <ul className="sidebar__children" aria-label={group.label}>
                          {group.children.map((child) => (
                            <li key={child.id}>
                              <NavLink to={child.route} className="sidebar__link">
                                {child.label}
                              </NavLink>
                            </li>
                          ))}
                        </ul>
                      )}
                    </li>
                  )
                })}
              </ul>
            )}
            {!collapsed && !category.groups && (
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
