export interface Country {
  code: string
  name: string
}

export interface City {
  id: number
  countryCode: string
  name: string
}

export interface WeatherResponse {
  location: string
  timeUtc: string
  windSpeed: number | null
  windDirection: number | null
  visibility: number | null
  pressure: number
  skyCondition: string
  tempFahrenheit: number
  tempCelsius: number
  dewPoint: number
  humidity: number
}

export interface CreateWeatherNoteRequest {
  cityName: string
  note: string
}

export interface CreateWeatherNoteResponse {
  id: string
  cityName: string
  note: string
  createdAtUtc: string
}

export interface ProblemDetails {
  title?: string
  status?: number
  detail?: string
  errors?: Record<string, string[]>
}
