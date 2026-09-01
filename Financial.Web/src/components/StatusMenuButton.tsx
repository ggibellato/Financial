import {
  Badge,
  Menu,
  MenuButton,
  MenuItem,
  MenuList,
  MenuPopover,
  MenuTrigger,
  makeStyles,
  tokens,
  type BadgeProps,
} from '@fluentui/react-components'
import { CheckmarkRegular } from '@fluentui/react-icons'

const STATUS_COLORS: Record<string, BadgeProps['color']> = {
  Unset: 'subtle',
  Scheduled: 'informative',
  Paid: 'success',
}

const useStyles = makeStyles({
  trigger: {
    minWidth: 0,
    paddingLeft: tokens.spacingHorizontalS,
    paddingRight: tokens.spacingHorizontalXS,
  },
})

interface StatusMenuButtonProps {
  statuses: string[]
  status: string
  isUpdating?: boolean
  onChange: (status: string) => void
}

export default function StatusMenuButton({ statuses, status, isUpdating = false, onChange }: StatusMenuButtonProps) {
  const styles = useStyles()

  return (
    <Menu>
      <MenuTrigger disableButtonEnhancement>
        <MenuButton
          className={styles.trigger}
          appearance="subtle"
          disabled={isUpdating}
          aria-label={`Status: ${status}. Change status`}
        >
          <Badge appearance="filled" color={STATUS_COLORS[status] ?? 'subtle'}>
            {status}
          </Badge>
        </MenuButton>
      </MenuTrigger>
      <MenuPopover>
        <MenuList hasCheckmarks>
          {statuses.map((candidate) => {
            const isCurrent = candidate === status
            return (
              <MenuItem
                key={candidate}
                disabled={isCurrent}
                icon={isCurrent ? <CheckmarkRegular /> : undefined}
                onClick={() => {
                  if (!isCurrent) onChange(candidate)
                }}
              >
                {candidate}
              </MenuItem>
            )
          })}
        </MenuList>
      </MenuPopover>
    </Menu>
  )
}
