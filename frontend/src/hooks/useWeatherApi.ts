import { useCallback } from 'react'
import { weatherApi } from '../services/weatherApi'
import { useAsync } from './useAsync'

export function useCountries() {
  return useAsync('countries', weatherApi.getCountries)
}

export function useCities(countryCode: string) {
  const load = useCallback((signal: AbortSignal) => weatherApi.getCities(countryCode, signal), [countryCode])
  return useAsync(countryCode || null, load)
}

export function useWeather(cityName: string) {
  const load = useCallback((signal: AbortSignal) => weatherApi.getWeather(cityName, signal), [cityName])
  return useAsync(cityName || null, load)
}
