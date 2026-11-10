import { Link, useLocation } from 'react-router-dom'
import { findNavLocation } from '../navigation/navTree'
import './Breadcrumb.css'

function Breadcrumb() {
  const navLocation = findNavLocation(useLocation().pathname)

  if (!navLocation) {
    return (
      <nav className="breadcrumb" aria-label="Breadcrumb">
        <ol className="breadcrumb__list">
          <li aria-current="page">—</li>
        </ol>
      </nav>
    )
  }

  const { trail, page, detail } = navLocation
  return (
    <nav className="breadcrumb" aria-label="Breadcrumb">
      <ol className="breadcrumb__list">
        {trail.map((label) => (
          <li key={label}>{label}</li>
        ))}
        {detail ? (
          <>
            <li>
              <Link to={page.route}>{page.label}</Link>
            </li>
            <li aria-current="page">{detail.label}</li>
          </>
        ) : (
          <li aria-current="page">{page.label}</li>
        )}
      </ol>
    </nav>
  )
}

export default Breadcrumb
