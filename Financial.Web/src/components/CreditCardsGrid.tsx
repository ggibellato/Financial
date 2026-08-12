import type { CreditCardDto, UpdateCreditCardDto } from '../api/types'

interface CreditCardsGridProps {
  creditCards: CreditCardDto[]
  updatingCardId: string | null
  updateError: string | null
  onUpdate: (id: string, request: UpdateCreditCardDto) => void
}

export default function CreditCardsGrid({ creditCards, updatingCardId, updateError, onUpdate }: CreditCardsGridProps) {
  return (
    <section className="monthly-page__section monthly-page__section--grid">
      <div className="monthly-page__table-scroll">
        <table className="monthly-page__table data-table">
          <thead>
            <tr>
              <th>Card</th>
              <th>Next Invoice Due Date</th>
              <th>Active</th>
            </tr>
          </thead>
          <tbody>
            {creditCards.map((c) => (
              <tr key={c.id}>
                <td>{c.name}</td>
                <td>
                  <input
                    aria-label={`Next invoice due date for ${c.name}`}
                    type="date"
                    value={c.nextInvoiceDueDate ?? ''}
                    disabled={updatingCardId === c.id}
                    onChange={(e) =>
                      onUpdate(c.id, {
                        nextInvoiceDueDate: e.target.value === '' ? null : e.target.value,
                        isActive: c.isActive,
                      })
                    }
                  />
                </td>
                <td>
                  <input
                    aria-label={`Active for ${c.name}`}
                    type="checkbox"
                    checked={c.isActive}
                    disabled={updatingCardId === c.id}
                    onChange={(e) =>
                      onUpdate(c.id, {
                        nextInvoiceDueDate: c.nextInvoiceDueDate,
                        isActive: e.target.checked,
                      })
                    }
                  />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {updateError && <p className="monthly-page__error">{updateError}</p>}
    </section>
  )
}
