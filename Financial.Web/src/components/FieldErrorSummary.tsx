import { forwardRef, useId } from 'react'
import { Link, MessageBar, MessageBarBody, MessageBarTitle, makeStyles, tokens } from '@fluentui/react-components'
import type { FieldError } from '../utils/fieldErrors'

const useStyles = makeStyles({
  root: {
    marginBottom: tokens.spacingVerticalM,
  },
  list: {
    margin: `${tokens.spacingVerticalXS} 0 0`,
    paddingLeft: tokens.spacingHorizontalXL,
  },
})

interface FieldErrorSummaryProps {
  errors: readonly FieldError[]
}

const FieldErrorSummary = forwardRef<HTMLDivElement, FieldErrorSummaryProps>(({ errors }, ref) => {
  const styles = useStyles()
  const titleId = useId()

  return (
    <MessageBar ref={ref} intent="error" layout="multiline" tabIndex={-1} aria-labelledby={titleId} className={styles.root}>
      <MessageBarBody>
        <MessageBarTitle id={titleId}>Fix {errors.length} fields before saving</MessageBarTitle>
        <ul className={styles.list}>
          {errors.map((error) => (
            <li key={error.fieldId}>
              <Link as="button" type="button" onClick={() => document.getElementById(error.fieldId)?.focus()}>
                {error.message}
              </Link>
            </li>
          ))}
        </ul>
      </MessageBarBody>
    </MessageBar>
  )
})

FieldErrorSummary.displayName = 'FieldErrorSummary'

export default FieldErrorSummary
