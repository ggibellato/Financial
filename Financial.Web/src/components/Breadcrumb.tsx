import { useLocation } from 'react-router-dom'
import { NAV_TREE } from '../navigation/navTree'
import './Breadcrumb.css'

function Breadcrumb() {
  const location = useLocation()

  for (const category of NAV_TREE) {
    const child = category.children.find((c) => c.route === location.pathname)
    if (child) {
      return (
        <div className="breadcrumb">
          {category.label} › {child.label}
        </div>
      )
    }
  }

  return <div className="breadcrumb">—</div>
}

export default Breadcrumb
