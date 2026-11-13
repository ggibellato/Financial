import { NavLink } from 'react-router-dom'
import type { NavCategory, NavChild } from '../navigation/navTree'
import './NavCategoryLinks.css'

interface NavLinkListProps {
  label: string
  items: NavChild[]
  onNavigate?: () => void
}

function NavLinkList({ label, items, onNavigate }: NavLinkListProps) {
  return (
    <ul className="nav-links__list" aria-label={label}>
      {items.map((child) => (
        <li key={child.id}>
          <NavLink to={child.route} className="sidebar__link" onClick={onNavigate}>
            {child.label}
          </NavLink>
        </li>
      ))}
    </ul>
  )
}

interface NavCategoryLinksProps {
  category: NavCategory
  onNavigate?: () => void
}

function NavCategoryLinks({ category, onNavigate }: NavCategoryLinksProps) {
  if (!category.groups) {
    return <NavLinkList label={category.label} items={category.children} onNavigate={onNavigate} />
  }

  return (
    <>
      {category.groups.map((group) => (
        <div className="nav-links__group" key={group.id}>
          <span className="nav-links__group-title">{group.label}</span>
          <NavLinkList label={group.label} items={group.children} onNavigate={onNavigate} />
        </div>
      ))}
    </>
  )
}

export default NavCategoryLinks
