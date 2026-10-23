import { Link, useLocation } from 'react-router-dom'
import { NAV_TREE, matchNavChild } from '../navigation/navTree'
import './Breadcrumb.css'

function Breadcrumb() {
  const location = useLocation()

  for (const category of NAV_TREE) {
    for (const child of category.children) {
      const match = matchNavChild(location.pathname, child)
      if (match === 'page') {
        return (
          <nav className="breadcrumb" aria-label="Breadcrumb">
            <ol className="breadcrumb__list">
              <li>{category.label}</li>
              <li aria-current="page">{child.label}</li>
            </ol>
          </nav>
        )
      }
      if (match === 'detail' && child.detail) {
        return (
          <nav className="breadcrumb" aria-label="Breadcrumb">
            <ol className="breadcrumb__list">
              <li>{category.label}</li>
              <li>
                <Link to={child.route}>{child.label}</Link>
              </li>
              <li aria-current="page">{child.detail.label}</li>
            </ol>
          </nav>
        )
      }
    }
  }

  return (
    <nav className="breadcrumb" aria-label="Breadcrumb">
      <ol className="breadcrumb__list">
        <li aria-current="page">—</li>
      </ol>
    </nav>
  )
}

export default Breadcrumb
