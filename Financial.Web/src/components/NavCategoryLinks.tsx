import { useId } from 'react'
import { NavLink } from 'react-router-dom'
import type { NavCategory, NavChild, NavGroup } from '../navigation/navTree'
import './NavCategoryLinks.css'

interface NavLinkListProps {
  label?: string
  labelledBy?: string
  items: NavChild[]
  onNavigate?: () => void
}

function NavLinkList({ label, labelledBy, items, onNavigate }: NavLinkListProps) {
  return (
    <ul className="nav-links__list" aria-label={label} aria-labelledby={labelledBy}>
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

interface NavGroupSectionProps {
  group: NavGroup
  onNavigate?: () => void
}

function NavGroupSection({ group, onNavigate }: NavGroupSectionProps) {
  const titleId = useId()
  return (
    <div className="nav-links__group">
      <span id={titleId} className="nav-links__group-title">
        {group.label}
      </span>
      <NavLinkList labelledBy={titleId} items={group.children} onNavigate={onNavigate} />
    </div>
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
        <NavGroupSection key={group.id} group={group} onNavigate={onNavigate} />
      ))}
    </>
  )
}

export default NavCategoryLinks
