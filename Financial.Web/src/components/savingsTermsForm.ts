import type { SavingsTermsDto } from '../api/types'
import { hasAtMostTwoDecimals, parseIntegerInRange, parseValidatedNumber, todayIsoDate } from '../utils/formatters'
import { nextRowKey } from '../utils/rowKeys'

export type InterestFrequency = 'Monthly' | 'Daily'

export const MIN_PAYMENT_DAY = 1
export const MAX_PAYMENT_DAY = 31
const MAX_ANNUAL_PERCENT = 100

export interface RateRow {
  key: string
  annualPercent: string
  effectiveFrom: string
}

export interface SavingsFormValues {
  interestFrequency: InterestFrequency
  paymentDay: string
  isTaxFree: boolean
  rates: RateRow[]
  roundUpOnly: boolean
}

interface RateErrors {
  annualPercent?: string
  effectiveFrom?: string
}

export interface SavingsErrors {
  paymentDay?: string
  rates: RateErrors[]
}

export function newRateRow(annualPercent = '', effectiveFrom = todayIsoDate()): RateRow {
  return { key: nextRowKey('rate'), annualPercent, effectiveFrom }
}

export function initialSavingsValues(savings: SavingsTermsDto | null | undefined): SavingsFormValues {
  if (!savings) return { interestFrequency: 'Monthly', paymentDay: '', isTaxFree: false, rates: [newRateRow()], roundUpOnly: false }
  return {
    interestFrequency: savings.interestFrequency as InterestFrequency,
    paymentDay: savings.paymentDay?.toString() ?? '',
    isTaxFree: savings.isTaxFree,
    rates: savings.rates.map((rate) => newRateRow(rate.annualPercent.toFixed(2), rate.effectiveFrom)),
    roundUpOnly: savings.roundUpOnly ?? false,
  }
}

function paymentDayError(values: SavingsFormValues): string | undefined {
  if (values.interestFrequency !== 'Monthly') return undefined
  if (values.paymentDay.trim().length === 0) return 'Enter the day interest is paid.'
  if (parseIntegerInRange(values.paymentDay, MIN_PAYMENT_DAY, MAX_PAYMENT_DAY) === null)
    return `Interest payment day must be between ${MIN_PAYMENT_DAY} and ${MAX_PAYMENT_DAY}.`
  return undefined
}

function annualPercentError(annualPercent: string): string | undefined {
  const percent = parseValidatedNumber(annualPercent)
  if (percent === null) return 'Enter the rate as a number, for example 4.50.'
  if (percent < 0 || percent > MAX_ANNUAL_PERCENT) return `Interest rate must be between 0% and ${MAX_ANNUAL_PERCENT}%.`
  if (!hasAtMostTwoDecimals(annualPercent)) return 'Interest rate can have at most 2 decimal places.'
  return undefined
}

function effectiveFromError(rates: RateRow[], index: number): string | undefined {
  const { effectiveFrom } = rates[index]
  if (effectiveFrom.length === 0) return 'Enter the date the rate takes effect.'
  if (rates.slice(0, index).some((rate) => rate.effectiveFrom === effectiveFrom))
    return "Two rates can't take effect on the same date."
  return undefined
}

export function validateSavings(values: SavingsFormValues): SavingsErrors {
  return {
    paymentDay: paymentDayError(values),
    rates: values.rates.map((rate, index) => ({
      annualPercent: annualPercentError(rate.annualPercent),
      effectiveFrom: effectiveFromError(values.rates, index),
    })),
  }
}

export function hasSavingsErrors(errors: SavingsErrors): boolean {
  return errors.paymentDay !== undefined || errors.rates.some((rate) => rate.annualPercent || rate.effectiveFrom)
}

export function sameSavings(a: SavingsFormValues, b: SavingsFormValues): boolean {
  const comparable = ({ interestFrequency, paymentDay, isTaxFree, rates }: SavingsFormValues) =>
    JSON.stringify([interestFrequency, paymentDay, isTaxFree, rates.map((rate) => [rate.annualPercent, rate.effectiveFrom])])
  return comparable(a) === comparable(b)
}

export function toSavingsTermsDto(values: SavingsFormValues): SavingsTermsDto {
  return {
    interestFrequency: values.interestFrequency,
    paymentDay: values.interestFrequency === 'Monthly' ? Number(values.paymentDay) : null,
    isTaxFree: values.isTaxFree,
    rates: values.rates.map((rate) => ({ annualPercent: Number(rate.annualPercent), effectiveFrom: rate.effectiveFrom })),
    roundUpOnly: values.roundUpOnly,
  }
}
