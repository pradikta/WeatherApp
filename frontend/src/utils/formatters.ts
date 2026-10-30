const compassPoints = ['N', 'NE', 'E', 'SE', 'S', 'SW', 'W', 'NW']

const kmPerMile = 1.609344

function trimDecimals(value: number): string {
  return String(Number(value.toFixed(1)))
}

export function formatTemperature(value: number): string {
  return trimDecimals(value)
}

export function formatUtcTime(iso: string): string {
  const date = new Date(iso)
  const weekday = date.toLocaleString('en-GB', { weekday: 'long', day: 'numeric', month: 'short', year: 'numeric', timeZone: 'UTC' })
  const time = date.toLocaleString('en-GB', { hour: '2-digit', minute: '2-digit', hour12: false, timeZone: 'UTC' })
  return `${weekday} · ${time} UTC`
}

export function toCompass(degrees: number): string {
  const index = Math.round((((degrees % 360) + 360) % 360) / 45) % 8
  return compassPoints[index]
}

export function formatWindDirection(degrees: number | null): string {
  return degrees === null ? 'N/A' : `${toCompass(degrees)} (${Math.round(degrees)}°)`
}

export function formatWindSpeed(mph: number | null): string {
  return mph === null ? 'N/A' : `${trimDecimals(mph * kmPerMile)} km/h`
}

export function formatVisibility(meters: number | null): string {
  if (meters === null) {
    return 'N/A'
  }
  return meters >= 1000 ? `${trimDecimals(meters / 1000)} km` : `${Math.round(meters)} m`
}

export function capitalize(text: string): string {
  return text.charAt(0).toUpperCase() + text.slice(1)
}
