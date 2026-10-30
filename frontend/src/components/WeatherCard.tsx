import type { WeatherResponse } from '../types/weather'
import { capitalize, formatTemperature, formatUtcTime } from '../utils/formatters'
import { RainIcon, SkyIcon, SunIcon } from './Icons'

interface WeatherCardProps {
  weather: WeatherResponse
}

function ConditionIcon({ condition }: { condition: string }) {
  const text = condition.toLowerCase()
  if (/rain|drizzle|thunder|storm/.test(text)) {
    return <RainIcon className="h-4 w-4" />
  }
  if (/cloud|overcast|mist|fog|haze/.test(text)) {
    return <SkyIcon className="h-4 w-4" />
  }
  return <SunIcon className="h-4 w-4 text-amber-300" />
}

export function WeatherCard({ weather }: WeatherCardProps) {
  return (
    <section
      aria-label="Current conditions"
      className="rounded-2xl bg-linear-to-br from-blue-700 to-blue-900 p-5 text-white shadow-md sm:p-6"
    >
      <div className="flex flex-col gap-5 sm:flex-row sm:items-center sm:justify-between">
        <div className="min-w-0">
          <p className="text-xs font-semibold uppercase tracking-wider text-blue-50">Location</p>
          <h2 className="mt-1 text-xl font-semibold leading-snug sm:text-2xl">{weather.location}</h2>
          <p className="mt-1 text-sm text-blue-50">{formatUtcTime(weather.timeUtc)}</p>
          <p className="mt-3 inline-flex items-center gap-2 rounded-full bg-white/15 px-3 py-1 text-sm font-medium">
            <ConditionIcon condition={weather.skyCondition} />
            {capitalize(weather.skyCondition) || 'N/A'}
          </p>
        </div>

        <div className="flex items-stretch gap-4">
          <div>
            <p className="text-5xl font-bold leading-none sm:text-6xl">{formatTemperature(weather.tempCelsius)}°C</p>
            <p className="mt-2 text-sm text-blue-50">Celsius</p>
          </div>
          <div className="w-px bg-blue-300/50" aria-hidden="true" />
          <div className="self-center">
            <p className="text-2xl font-semibold leading-none">{formatTemperature(weather.tempFahrenheit)}°F</p>
            <p className="mt-2 text-sm text-blue-50">Fahrenheit</p>
          </div>
        </div>
      </div>
    </section>
  )
}
