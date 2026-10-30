import type { ReactNode } from 'react'
import type { WeatherResponse } from '../types/weather'
import {
  formatTemperature,
  formatVisibility,
  formatWindDirection,
  formatWindSpeed,
} from '../utils/formatters'
import { HumidityIcon, PressureIcon, WindIcon } from './Icons'

interface WeatherDetailsProps {
  weather: WeatherResponse
}

interface GroupProps {
  title: string
  icon: ReactNode
  rows: { label: string; value: string }[]
}

function Group({ title, icon, rows }: GroupProps) {
  return (
    <section className="rounded-2xl bg-white p-4 shadow-sm ring-1 ring-slate-100">
      <h2 className="flex items-center gap-2 text-sm font-semibold text-slate-900">
        <span className="flex h-8 w-8 items-center justify-center rounded-lg bg-blue-50 text-blue-600">{icon}</span>
        {title}
      </h2>
      <dl className="mt-3 divide-y divide-slate-100">
        {rows.map((row) => (
          <div key={row.label} className="flex items-baseline justify-between gap-3 py-2.5">
            <dt className="text-sm text-slate-600">{row.label}</dt>
            <dd className="text-right text-base font-semibold text-slate-900">{row.value}</dd>
          </div>
        ))}
      </dl>
    </section>
  )
}

export function WeatherDetails({ weather }: WeatherDetailsProps) {
  return (
    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      <Group
        title="Wind"
        icon={<WindIcon />}
        rows={[
          { label: 'Speed', value: formatWindSpeed(weather.windSpeed) },
          { label: 'Direction', value: formatWindDirection(weather.windDirection) },
        ]}
      />
      <Group
        title="Pressure & Visibility"
        icon={<PressureIcon />}
        rows={[
          { label: 'Pressure', value: `${weather.pressure} hPa` },
          { label: 'Visibility', value: formatVisibility(weather.visibility) },
        ]}
      />
      <Group
        title="Humidity & Dew Point"
        icon={<HumidityIcon />}
        rows={[
          { label: 'Relative humidity', value: `${weather.humidity}%` },
          { label: 'Dew point', value: `${formatTemperature(weather.dewPoint)}°C` },
        ]}
      />
    </div>
  )
}
