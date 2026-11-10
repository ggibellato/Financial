import { useEffect } from 'react'
import { useLocation } from 'react-router-dom'
import { makeStyles } from '@fluentui/react-components'
import { findNavPageTitle } from '../navigation/navTree'

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
  const pageTitle = findNavPageTitle(pathname)

  useEffect(() => {
    document.title = pageTitle ? `${pageTitle} - ${APP_NAME}` : APP_NAME
  }, [pageTitle])

  return (
    <div role="status" className={styles.visuallyHidden}>
      {pageTitle}
    </div>
  )
}
