import { useLocation } from 'react-router-dom'
import { NAV_TREE } from '../navigation/navTree'
import './Breadcrumb.css'

function Breadcrumb() {
  const location = useLocation()

  for (const category of NAV_TREE) {
    const child = category.children.find((c) => c.route === location.pathname)
    if (child) {
      return (
        <nav className="breadcrumb" aria-label="Breadcrumb">
          <ol className="breadcrumb__list">
            <li>{category.label}</li>
            <li aria-current="page">{child.label}</li>
          </ol>
        </nav>
      )
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
