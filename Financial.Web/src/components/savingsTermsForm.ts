import type { SavingsTermsDto } from '../api/types'
import { todayIsoDate } from '../utils/formatters'

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
}

export interface RateErrors {
  annualPercent?: string
  effectiveFrom?: string
}

export interface SavingsErrors {
  paymentDay?: string
  rates: RateErrors[]
}

let lastRateKey = 0

export function newRateRow(annualPercent = '', effectiveFrom = todayIsoDate()): RateRow {
  lastRateKey += 1
  return { key: `rate-${lastRateKey}`, annualPercent, effectiveFrom }
}

export function initialSavingsValues(savings: SavingsTermsDto | null | undefined): SavingsFormValues {
  if (!savings) return { interestFrequency: 'Monthly', paymentDay: '', isTaxFree: false, rates: [newRateRow()] }
  return {
    interestFrequency: savings.interestFrequency as InterestFrequency,
    paymentDay: savings.paymentDay?.toString() ?? '',
    isTaxFree: savings.isTaxFree,
    rates: savings.rates.map((rate) => newRateRow(rate.annualPercent.toFixed(2), rate.effectiveFrom)),
  }
}

function paymentDayError(values: SavingsFormValues): string | undefined {
  if (values.interestFrequency !== 'Monthly') return undefined
  const day = Number(values.paymentDay)
  if (values.paymentDay.trim().length === 0) return 'Enter the day interest is paid.'
  if (!Number.isInteger(day) || day < MIN_PAYMENT_DAY || day > MAX_PAYMENT_DAY)
    return `Interest payment day must be between ${MIN_PAYMENT_DAY} and ${MAX_PAYMENT_DAY}.`
  return undefined
}

function annualPercentError(annualPercent: string): string | undefined {
  const percent = Number(annualPercent)
  if (annualPercent.trim().length === 0 || !Number.isFinite(percent)) return 'Enter the rate as a number, for example 4.50.'
  if (percent < 0 || percent > MAX_ANNUAL_PERCENT) return `Interest rate must be between 0% and ${MAX_ANNUAL_PERCENT}%.`
  const significantDecimals = (annualPercent.trim().split('.')[1] ?? '').replace(/0+$/, '')
  if (significantDecimals.length > 2) return 'Interest rate can have at most 2 decimal places.'
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

export function toSavingsTermsDto(values: SavingsFormValues): SavingsTermsDto {
  return {
    interestFrequency: values.interestFrequency,
    paymentDay: values.interestFrequency === 'Monthly' ? Number(values.paymentDay) : null,
    isTaxFree: values.isTaxFree,
    rates: values.rates.map((rate) => ({ annualPercent: Number(rate.annualPercent), effectiveFrom: rate.effectiveFrom })),
  }
}
