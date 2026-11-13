import { Fragment, useRef, useState, type FocusEvent, type KeyboardEvent, type ReactNode } from 'react'
import { useLocation } from 'react-router-dom'
import { Button, OverlayDrawer, DrawerBody, DrawerHeader, DrawerHeaderTitle } from '@fluentui/react-components'
import { DismissRegular } from '@fluentui/react-icons'
import { NAV_TREE, matchNavChild, type NavCategory } from '../navigation/navTree'
import { getStoredSidebarCollapsed, setStoredSidebarCollapsed } from '../utils/sidebarStorage'
import { useMediaQuery } from '../hooks/useMediaQuery'
import { MOBILE_MEDIA_QUERY } from '../styles/breakpoints'
import SidebarFlyout from './SidebarFlyout'
import NavCategoryLinks from './NavCategoryLinks'
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


interface OpenFlyout {
  categoryId: string
  anchorRect: DOMRect
}

interface SidebarProps {
  mobileOpen: boolean
  onMobileOpenChange: (open: boolean) => void
}

function hasActivePage(category: NavCategory, pathname: string) {
  const pages = [...category.children, ...(category.groups ?? []).flatMap((group) => group.children)]
  return pages.some((page) => matchNavChild(pathname, page) !== null)
}

function categoryHeaderClassName(category: NavCategory, pathname: string) {
  return `sidebar__category-header${hasActivePage(category, pathname) ? ' sidebar__category-header--active' : ''}`
}

function MobileNav({ mobileOpen, onMobileOpenChange }: SidebarProps) {
  const location = useLocation()

  return (
    <OverlayDrawer
      open={mobileOpen}
      onOpenChange={(_, data) => onMobileOpenChange(data.open)}
      position="start"
      size="small"
    >
      <DrawerHeader>
        <DrawerHeaderTitle
          action={
            <Button
              appearance="subtle"
              aria-label="Close navigation"
              icon={<DismissRegular />}
              onClick={() => onMobileOpenChange(false)}
            />
          }
        >
          Menu
        </DrawerHeaderTitle>
      </DrawerHeader>
      <DrawerBody>
        <nav className="sidebar sidebar--mobile" aria-label="Main">
          {NAV_TREE.map((category) => {
            const CategoryIcon = CATEGORY_ICONS[category.id]
            return (
              <div className="sidebar__mobile-category" key={category.id}>
                <span className={categoryHeaderClassName(category, location.pathname)}>
                  <CategoryIcon />
                  {category.label}
                </span>
                <NavCategoryLinks category={category} />
              </div>
            )
          })}
        </nav>
      </DrawerBody>
    </OverlayDrawer>
  )
}

function Sidebar({ mobileOpen, onMobileOpenChange }: SidebarProps) {
  const isMobile = useMediaQuery(MOBILE_MEDIA_QUERY)
  const [collapsed, setCollapsed] = useState(() => getStoredSidebarCollapsed())
  const [openFlyout, setOpenFlyout] = useState<OpenFlyout | null>(null)
  const location = useLocation()

  const triggerRefs = useRef<Record<string, HTMLButtonElement | null>>({})
  const closeTimerRef = useRef<number | null>(null)
  const suppressFocusOpenRef = useRef(false)

  if (isMobile) {
    return <MobileNav mobileOpen={mobileOpen} onMobileOpenChange={onMobileOpenChange} />
  }

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
      setOpenFlyout(null)
    }, CLOSE_DELAY_MS)
  }

  const open = (categoryId: string) => {
    if (suppressFocusOpenRef.current) {
      suppressFocusOpenRef.current = false
      return
    }
    const trigger = triggerRefs.current[categoryId]
    if (!trigger) return
    cancelClose()
    setOpenFlyout({ categoryId, anchorRect: trigger.getBoundingClientRect() })
  }

  const closeNow = () => {
    cancelClose()
    setOpenFlyout(null)
  }

  const handleBlur = (event: FocusEvent<HTMLDivElement>) => {
    if (!event.currentTarget.contains(event.relatedTarget as Node | null)) {
      closeNow()
    }
  }

  const handleKeyDown = (event: KeyboardEvent<HTMLDivElement>, categoryId: string) => {
    if (event.key !== 'Escape') return
    closeNow()
    const trigger = triggerRefs.current[categoryId]
    if (trigger && document.activeElement !== trigger) {
      suppressFocusOpenRef.current = true
      trigger.focus()
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
        const flyoutId = `sidebar-flyout-${category.id}`
        const isOpen = openFlyout?.categoryId === category.id

        return (
          <Fragment key={category.id}>
            {category.id === 'admin' && <hr className="sidebar__divider" />}
            <div
              className="sidebar__category"
              onMouseEnter={() => open(category.id)}
              onMouseLeave={scheduleClose}
              onBlur={handleBlur}
              onKeyDown={(event) => handleKeyDown(event, category.id)}
            >
              <button
                ref={(el) => {
                  triggerRefs.current[category.id] = el
                }}
                type="button"
                className={categoryHeaderClassName(category, location.pathname)}
                aria-label={collapsed ? category.label : undefined}
                aria-expanded={isOpen}
                aria-controls={isOpen ? flyoutId : undefined}
                onFocus={() => open(category.id)}
                onClick={() => open(category.id)}
              >
                <CategoryIcon />
                {!collapsed && <span className="sidebar__category-label">{category.label}</span>}
              </button>
              {isOpen && (
                <SidebarFlyout
                  id={flyoutId}
                  category={category}
                  anchorRect={openFlyout.anchorRect}
                  onNavigate={closeNow}
                />
              )}
            </div>
          </Fragment>
        )
      })}
    </nav>
  )
}

export default Sidebar
