import './AdminEntityPlaceholderPage.css'

interface AdminEntityPlaceholderPageProps {
  entityLabel: string
}

export default function AdminEntityPlaceholderPage({ entityLabel }: AdminEntityPlaceholderPageProps) {
  return (
    <section className="admin-entity-placeholder">
      <header className="admin-entity-placeholder__header">
        <h2>{entityLabel}</h2>
      </header>
      <p className="admin-entity-placeholder__notice">Coming soon.</p>
    </section>
  )
}
