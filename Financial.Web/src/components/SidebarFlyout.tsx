import { forwardRef, type FocusEvent, type KeyboardEvent } from 'react'
import { createPortal } from 'react-dom'
import { NavLink } from 'react-router-dom'
import type { NavCategory } from '../navigation/navTree'
import './SidebarFlyout.css'

interface SidebarFlyoutProps {
  category: NavCategory
  anchorRect: DOMRect
  onClose: (refocus?: boolean) => void
  onMouseEnter: () => void
  onMouseLeave: () => void
  onBlur: (event: FocusEvent<HTMLDivElement>) => void
}

const SidebarFlyout = forwardRef<HTMLDivElement, SidebarFlyoutProps>(function SidebarFlyout(
  { category, anchorRect, onClose, onMouseEnter, onMouseLeave, onBlur },
  ref,
) {
  const handleKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    if (event.key === 'Escape') {
      onClose(true)
    }
  }

  return createPortal(
    <div
      ref={ref}
      className="sidebar-flyout"
      style={{ top: anchorRect.top, left: anchorRect.right }}
      onMouseEnter={onMouseEnter}
      onMouseLeave={onMouseLeave}
      onBlur={onBlur}
      onKeyDown={handleKeyDown}
    >
      <span className="sidebar-flyout__title">{category.label}</span>
      {category.groups ? (
        category.groups.map((group) => (
          <div className="sidebar-flyout__group" key={group.id}>
            <span className="sidebar-flyout__group-title">{group.label}</span>
            <ul className="sidebar-flyout__list" aria-label={group.label}>
              {group.children.map((child) => (
                <li key={child.id}>
                  <NavLink to={child.route} className="sidebar__link" onClick={() => onClose()}>
                    {child.label}
                  </NavLink>
                </li>
              ))}
            </ul>
          </div>
        ))
      ) : (
        <ul className="sidebar-flyout__list" aria-label={category.label}>
          {category.children.map((child) => (
            <li key={child.id}>
              <NavLink to={child.route} className="sidebar__link" onClick={() => onClose()}>
                {child.label}
              </NavLink>
            </li>
          ))}
        </ul>
      )}
    </div>,
    document.body,
  )
})

export default SidebarFlyout
