import type { City, Country } from '../types/weather'

interface LocationSelectorProps {
  countries: Country[]
  cities: City[]
  countryCode: string
  cityName: string
  countriesLoading: boolean
  citiesLoading: boolean
  onCountryChange: (countryCode: string) => void
  onCityChange: (cityName: string) => void
}

const selectClass =
  'w-full rounded-xl border border-slate-500 bg-white px-4 py-3 text-sm outline-none transition focus:border-blue-500 focus:ring-2 focus:ring-blue-200 disabled:cursor-not-allowed disabled:bg-slate-50 disabled:text-slate-400'

const labelClass = 'mb-1.5 block text-xs font-medium text-slate-600'

export function LocationSelector({
  countries,
  cities,
  countryCode,
  cityName,
  countriesLoading,
  citiesLoading,
  onCountryChange,
  onCityChange,
}: LocationSelectorProps) {
  return (
    <section className="space-y-4 rounded-2xl bg-white p-4 shadow-sm">
      <h2 className="text-sm font-semibold text-slate-900">Select a location</h2>

      <div className="grid gap-4 sm:grid-cols-2">
      <label className="block">
        <span className={labelClass}>Country</span>
        <select
          className={selectClass}
          value={countryCode}
          disabled={countriesLoading}
          onChange={(event) => onCountryChange(event.target.value)}
        >
          <option value="">{countriesLoading ? 'Loading countries…' : 'Select a country'}</option>
          {countries.map((country) => (
            <option key={country.code} value={country.code}>
              {country.name}
            </option>
          ))}
        </select>
      </label>

      <label className="block">
        <span className={labelClass}>City</span>
        <select
          className={selectClass}
          value={cityName}
          disabled={!countryCode || citiesLoading}
          onChange={(event) => onCityChange(event.target.value)}
        >
          <option value="">{citiesLoading ? 'Loading cities…' : 'Select a city'}</option>
          {cities.map((city) => (
            <option key={city.id} value={city.name}>
              {city.name}
            </option>
          ))}
        </select>
      </label>
      </div>

      <p className="text-xs text-slate-600">Selecting a city automatically loads its current weather.</p>
    </section>
  )
}
