import { makeStyles, shorthands, tokens } from '@fluentui/react-components'

export const useFormPanelStyles = makeStyles({
  panel: {
    flexShrink: 0,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalM,
    border: `${tokens.strokeWidthThin} solid ${tokens.colorNeutralStroke2}`,
    borderRadius: tokens.borderRadiusMedium,
    backgroundColor: tokens.colorNeutralBackground2,
  },
  grid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(4, minmax(0, 1fr))',
    gap: tokens.spacingVerticalM,
    // Without this, the grid's default stretch alignment gives every Field the tallest
    // sibling's height, and Field's own internal grid (label/control/validationMessage rows)
    // redistributes that extra space unevenly — shifting a field's input up or down relative to
    // its row siblings depending on whether it currently shows a validation message.
    alignItems: 'start',
    '@media (max-width: 1023px)': {
      gridTemplateColumns: 'repeat(2, minmax(0, 1fr))',
    },
    '@media (max-width: 639px)': {
      gridTemplateColumns: '1fr',
    },
  },
  spanTwo: {
    gridColumnEnd: 'span 2',
    '@media (max-width: 639px)': {
      gridColumnEnd: 'auto',
    },
  },
  spanAll: {
    gridColumn: '1 / -1',
  },
  savingFieldset: {
    display: 'contents',
    ':disabled .fui-Input, :disabled .fui-Select__select, :disabled .fui-Textarea': {
      cursor: 'not-allowed',
      ...shorthands.borderColor(tokens.colorNeutralStrokeDisabled),
    },
    ':disabled .fui-Input__input, :disabled .fui-Select__select, :disabled .fui-Textarea__textarea, :disabled .fui-Label': {
      color: tokens.colorNeutralForegroundDisabled,
    },
    ':disabled .fui-Button': {
      cursor: 'not-allowed',
      color: tokens.colorNeutralForegroundDisabled,
      backgroundColor: tokens.colorNeutralBackgroundDisabled,
      ...shorthands.borderColor(tokens.colorNeutralStrokeDisabled),
    },
  },
  actions: {
    display: 'flex',
    flexWrap: 'wrap',
    gap: tokens.spacingHorizontalS,
    alignItems: 'center',
  },
  radioRow: {
    flexWrap: 'wrap',
  },
  deleteAction: {
    marginLeft: tokens.spacingHorizontalXXL,
  },
  dialogError: {
    marginBottom: tokens.spacingVerticalM,
  },
})
