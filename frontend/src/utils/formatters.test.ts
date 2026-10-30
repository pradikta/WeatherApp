import { describe, expect, it } from 'vitest'
import {
  capitalize,
  formatTemperature,
  formatUtcTime,
  formatVisibility,
  formatWindDirection,
  formatWindSpeed,
  toCompass,
} from './formatters'

describe('formatTemperature', () => {
  it.each([
    [0, '0'],
    [32, '32'],
    [-17.78, '-17.8'],
    [30.04, '30'],
    [100, '100'],
  ])('formats %d as %s', (value, expected) => {
    expect(formatTemperature(value)).toBe(expected)
  })
})

describe('formatWindSpeed', () => {
  it('converts mph to km/h', () => {
    expect(formatWindSpeed(10)).toBe('16.1 km/h')
  })

  it('returns N/A when missing', () => {
    expect(formatWindSpeed(null)).toBe('N/A')
  })
})

describe('formatVisibility', () => {
  it.each([
    [10000, '10 km'],
    [1000, '1 km'],
    [850, '850 m'],
  ])('formats %d m as %s', (meters, expected) => {
    expect(formatVisibility(meters)).toBe(expected)
  })

  it('returns N/A when missing', () => {
    expect(formatVisibility(null)).toBe('N/A')
  })
})

describe('toCompass', () => {
  it.each([
    [0, 'N'],
    [45, 'NE'],
    [90, 'E'],
    [180, 'S'],
    [270, 'W'],
    [350, 'N'],
    [360, 'N'],
    [-90, 'W'],
  ])('maps %d degrees to %s', (degrees, expected) => {
    expect(toCompass(degrees)).toBe(expected)
  })
})

describe('formatWindDirection', () => {
  it('combines compass point and degrees', () => {
    expect(formatWindDirection(90)).toBe('E (90°)')
  })

  it('returns N/A when missing', () => {
    expect(formatWindDirection(null)).toBe('N/A')
  })
})

describe('formatUtcTime', () => {
  it('renders the date and time in UTC regardless of local zone', () => {
    expect(formatUtcTime('2026-10-29T12:30:00Z')).toBe('Thursday, 29 Oct 2026 · 12:30 UTC')
  })
})

describe('capitalize', () => {
  it('uppercases the first letter only', () => {
    expect(capitalize('clear sky')).toBe('Clear sky')
  })

  it('handles an empty string', () => {
    expect(capitalize('')).toBe('')
  })
})
