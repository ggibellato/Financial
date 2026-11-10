import { TableHeaderCell } from '@fluentui/react-components'

interface ActionsColumnHeaderProps {
  className?: string
}

export default function ActionsColumnHeader({ className = 'data-table__col--action' }: ActionsColumnHeaderProps) {
  return <TableHeaderCell className={className} aria-label="Actions" />
}
