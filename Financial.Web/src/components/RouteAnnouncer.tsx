import { useEffect } from 'react'
import { useLocation } from 'react-router-dom'
import { makeStyles } from '@fluentui/react-components'
import { findNavLocation } from '../navigation/navTree'

const APP_NAME = 'Financial'

const useRouteAnnouncerStyles = makeStyles({
  visuallyHidden: {
    position: 'absolute',
    width: '1px',
    height: '1px',
    overflow: 'hidden',
    clip: 'rect(0 0 0 0)',
    whiteSpace: 'nowrap',
  },
})

export default function RouteAnnouncer() {
  const styles = useRouteAnnouncerStyles()
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
