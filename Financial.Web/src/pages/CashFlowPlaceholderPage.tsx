import './CashFlowPlaceholderPage.css'

interface CashFlowPlaceholderPageProps {
  title: string
}

export default function CashFlowPlaceholderPage({ title }: CashFlowPlaceholderPageProps) {
  return (
    <section className="cashflow-placeholder">
      <h2>{title}</h2>
      <p>{title} view is coming soon.</p>
    </section>
  )
}
