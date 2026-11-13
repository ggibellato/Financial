import type { NavCategory } from '../navigation/navTree'
import NavCategoryLinks from './NavCategoryLinks'
import './SidebarFlyout.css'

interface SidebarFlyoutProps {
  id: string
  category: NavCategory
  anchorRect: DOMRect
  onNavigate: () => void
}

function SidebarFlyout({ id, category, anchorRect, onNavigate }: SidebarFlyoutProps) {
  return (
    <div id={id} className="sidebar-flyout" style={{ top: anchorRect.top, left: anchorRect.right }}>
      <span className="sidebar-flyout__title">{category.label}</span>
      <NavCategoryLinks category={category} onNavigate={onNavigate} />
    </div>
  )
}

export default SidebarFlyout
