import { useEffect } from 'react'
import { useLocation } from 'react-router-dom'
import { findNavLocation } from '../navigation/navTree'
import { useVisuallyHiddenStyles } from '../styles/visuallyHidden'

const APP_NAME = 'Financial'


export default function RouteAnnouncer() {
  const styles = useVisuallyHiddenStyles()
  const { pathname } = useLocation()
  const navLocation = findNavLocation(pathname)
  const pageTitle = navLocation ? (navLocation.detail ?? navLocation.page).label : null
  const documentTitle = navLocation
    ? [pageTitle, ...[...navLocation.trail].reverse(), APP_NAME].join(' - ')
    : APP_NAME

  useEffect(() => {
    document.title = documentTitle
  }, [documentTitle])

  return (
    <div role="status" className={styles.visuallyHidden}>
      {pageTitle}
    </div>
  )
}
