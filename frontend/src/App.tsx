import { useState } from 'react'
import { SkyIcon } from './components/Icons'
import { LocationSelector } from './components/LocationSelector'
import { WeatherCard } from './components/WeatherCard'
import { WeatherDetails } from './components/WeatherDetails'
import { WeatherNoteForm } from './components/WeatherNoteForm'
import { useCities, useCountries, useWeather } from './hooks/useWeatherApi'

function App() {
  const [countryCode, setCountryCode] = useState('')
  const [cityName, setCityName] = useState('')

  const countries = useCountries()
  const cities = useCities(countryCode)
  const weather = useWeather(cityName)

  const loadError = countries.error ?? cities.error

  function handleCountryChange(code: string) {
    setCountryCode(code)
    setCityName('')
  }

  return (
    <main className="mx-auto my-4 max-w-3xl space-y-4 rounded-3xl bg-slate-50 p-4 shadow-sm ring-1 ring-slate-200 sm:my-8 sm:space-y-5 sm:p-6">
      <header className="flex items-center gap-3 border-b border-slate-200 pb-4">
        <span className="flex h-12 w-12 items-center justify-center rounded-xl bg-blue-600 text-white">
          <SkyIcon className="h-6 w-6" />
        </span>
        <div>
          <h1 className="text-base font-semibold text-slate-900">Weather Dashboard</h1>
          <p className="text-xs text-slate-600">Current weather at a glance</p>
        </div>
      </header>

      <LocationSelector
        countries={countries.data ?? []}
        cities={cities.data ?? []}
        countryCode={countryCode}
        cityName={cityName}
        countriesLoading={countries.loading}
        citiesLoading={cities.loading}
        onCountryChange={handleCountryChange}
        onCityChange={setCityName}
      />

      {loadError && (
        <p role="alert" className="rounded-xl bg-red-50 px-4 py-3 text-sm text-red-700">
          {loadError}
        </p>
      )}

      {weather.loading && (
        <p role="status" className="text-sm text-slate-600">
          Loading weather…
        </p>
      )}

      {weather.error && (
        <p role="alert" className="rounded-xl bg-red-50 px-4 py-3 text-sm text-red-700">
          {weather.error}
        </p>
      )}

      {weather.data && (
        <>
          <WeatherCard weather={weather.data} />
          <WeatherDetails weather={weather.data} />
          <WeatherNoteForm key={cityName} cityName={cityName} />
        </>
      )}
    </main>
  )
}

export default App
