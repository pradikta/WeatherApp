import type {
  City,
  Country,
  CreateWeatherNoteRequest,
  CreateWeatherNoteResponse,
  ProblemDetails,
  WeatherResponse,
} from '../types/weather'

export class ApiError extends Error {
  readonly status: number
  readonly fieldErrors: string[]

  constructor(status: number, message: string, fieldErrors: string[] = []) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.fieldErrors = fieldErrors
  }
}

const friendlyMessages: Record<number, string> = {
  429: 'Too many requests. Please wait a moment and try again.',
  502: 'The weather provider returned an error. Please try again later.',
  503: 'The weather service is temporarily unavailable. Please try again shortly.',
  504: 'The weather provider timed out. Please try again.',
}

async function toApiError(response: Response): Promise<ApiError> {
  let problem: ProblemDetails | null = null
  try {
    problem = (await response.json()) as ProblemDetails
  } catch {
    // Body is empty or not JSON; fall back to the status code.
  }

  const fieldErrors = problem?.errors ? Object.values(problem.errors).flat() : []
  const message =
    friendlyMessages[response.status] ??
    problem?.detail ??
    problem?.title ??
    `Request failed (${response.status}).`

  return new ApiError(response.status, message, fieldErrors)
}

async function request<T>(url: string, init?: RequestInit, signal?: AbortSignal): Promise<T> {
  const response = await fetch(url, { ...init, signal })
  if (!response.ok) {
    throw await toApiError(response)
  }
  return (await response.json()) as T
}

export const weatherApi = {
  getCountries: (signal?: AbortSignal) => request<Country[]>('/api/countries', undefined, signal),

  getCities: (countryCode: string, signal?: AbortSignal) =>
    request<City[]>(`/api/countries/${encodeURIComponent(countryCode)}/cities`, undefined, signal),

  getWeather: (cityName: string, signal?: AbortSignal) =>
    request<WeatherResponse>(`/api/weather/${encodeURIComponent(cityName)}`, undefined, signal),

  createNote: (payload: CreateWeatherNoteRequest) =>
    request<CreateWeatherNoteResponse>('/api/weather/notes', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload),
    }),
}
