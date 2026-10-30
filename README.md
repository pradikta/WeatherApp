# WeatherApp

ASP.NET Core (.NET 10) Web API that lets a client pick a country and city, view current weather from OpenWeatherMap, and save custom weather notes. Built with Onion Architecture and CQRS (MediatR).

> **Status:** backend and automated tests are complete. The frontend client (`frontend/`) is not implemented yet.

## Architecture

The solution follows Onion Architecture: source dependencies point inward only.

```
Api ──► Application ──► Domain
 │          ▲
 └──► Infrastructure ──┘   (Infrastructure implements Application interfaces)
```

| Project | Responsibility |
|---|---|
| `WeatherApp.Domain` | Entities (`Country`, `City`, `WeatherNote`) and the `Temperature` value object. Pure C#, no NuGet packages, no references to other projects. |
| `WeatherApp.Application` | CQRS requests and handlers (MediatR), DTOs, FluentValidation validators, pipeline behaviors, and abstractions (`IWeatherService`, `ICountryRepository`, `IWeatherNoteRepository`). |
| `WeatherApp.Infrastructure` | EF Core `ApplicationDbContext` (SQLite in-memory), repositories, and `OpenWeatherMapService` (HTTP client). |
| `WeatherApp.Api` | Thin Minimal API layer. Endpoints only send requests through MediatR `ISender`. Also hosts DI wiring, CORS, logging, and exception handling. |

### CQRS decoupling

- **Queries** (read side) return DTOs and never mutate state. Read queries through EF Core use `.AsNoTracking()`.
- **Commands** (write side) validate input, then persist through a repository.
- Endpoints know nothing about EF Core or HTTP clients; they depend only on MediatR. Handlers know only interfaces, so they are unit-tested with mocks or an in-memory database.
- Two MediatR pipeline behaviors wrap every request: `ValidationBehavior` (FluentValidation, throws `ValidationException`) and `LoggingBehavior` (payload and elapsed time).

### Temperature conversion

OpenWeatherMap is called with `units=imperial`. The conversion to Celsius lives only in the domain `Temperature` value object: `Math.Round((fahrenheit - 32) * 5 / 9, 2)`.

## API

| Method | Route | Description |
|---|---|---|
| GET | `/api/countries` | All countries, ordered by name. |
| GET | `/api/countries/{countryCode}/cities` | Cities of a country (2-letter ISO code). |
| GET | `/api/weather/{cityName}` | Current weather for a city. |
| POST | `/api/weather/notes` | Saves a note. Body: `{ "cityName": "...", "note": "..." }`. Returns `201` with the created `id`. |
| GET | `/health` | Health check. |
| GET | `/openapi/v1.json` | OpenAPI document (Development only). |

The weather response contains: `location` ("City, Country"), `timeUtc`, `windSpeed`, `windDirection`, `visibility`, `pressure`, `skyCondition`, `tempFahrenheit`, `tempCelsius`, `dewPoint`, `humidity`.

### Errors

All errors use RFC 7807 Problem Details (`application/problem+json`):

| Cause | Status |
|---|---|
| Validation failure (includes per-field `errors`) | 400 |
| City not found | 404 |
| OpenWeatherMap unreachable or rejected the request | 502 |
| Any other unhandled exception | 500 |

## Setup

Requirements: [.NET 10 SDK](https://dotnet.microsoft.com/download) and a free [OpenWeatherMap API key](https://openweathermap.org/api).

### 1. Configure the API key (never commit it)

Copy the template and fill in your key:

```powershell
Copy-Item src/WeatherApp.Api/appsettings.Local.example.json src/WeatherApp.Api/appsettings.Local.json
```

```json
{
  "OpenWeatherMap": {
    "ApiKey": "REPLACE_WITH_OPENWEATHERMAP_API_KEY"
  }
}
```

`appsettings.Local.json` is git-ignored and loaded automatically when present. Environment variables and command-line arguments override it, for example `OpenWeatherMap__ApiKey=...`.

Other settings in `appsettings.json`:

| Key | Purpose | Default |
|---|---|---|
| `OpenWeatherMap:BaseAddress` | OpenWeatherMap base URL | `https://api.openweathermap.org/` |
| `Caching:WeatherCacheDurationMinutes` | How long a city's weather is cached | `1` |
| `Cors:AllowedOrigins` | Origins allowed to call the API | `http://localhost:5173` |

Without an API key, the weather endpoint returns `500`; the log explains that the key is missing.

### 2. Build, run, test

```powershell
dotnet restore
dotnet build
dotnet run --project src/WeatherApp.Api   # http://localhost:5177
dotnet test
```

## Testing

Tests live in `tests/WeatherApp.Tests` (xUnit, Moq, FluentAssertions) and run **fully offline**: no test calls the network. External HTTP is replaced by an in-memory `HttpMessageHandler`.

| Area | What is verified |
|---|---|
| Domain | `Temperature` at freezing (32°F), boiling (212°F), negative values, absolute zero, and rounding. |
| Query handlers | Isolated with mocked dependencies, including weather caching. |
| Command handler | Against SQLite in-memory: the note is persisted, read back through a fresh context, and gets a unique id. |
| Repositories and seed data | Ordering, `AsNoTracking`, and seed integrity. |
| `OpenWeatherMapService` | Mocked JSON payloads, request URLs, error and edge cases. |
| API endpoints | `WebApplicationFactory`: status codes, problem details, CORS, and a full-stack weather flow. |
| Architecture | Dependency rule between layers. |

Known gap: retry and circuit breaker behavior of the resilience handler is not covered by tests.

## Design notes

### EF Core and state management

- `ApplicationDbContext` uses SQLite with `DataSource=:memory:`. A single connection is opened once at startup and shared, because every new in-memory connection would otherwise get its own empty database.
- The schema is created with `EnsureCreated()` at startup and seeded (countries and cities) in `OnModelCreating`. The seed is a representative sample, not a worldwide dataset.
- **Data is lost when the process stops.** This is intentional for the assessment; switching to a file database only requires changing the connection string in `AddInfrastructure`.
- Repositories are scoped; the in-memory connection is a singleton.

### Weather integration

- `IWeatherService` hides OpenWeatherMap from the rest of the app. `OpenWeatherMapService` geocodes the city name to coordinates first (the by-city-name weather query is deprecated), then fetches weather by latitude and longitude.
- OpenWeatherMap does not return a dew point, so it is computed with the Magnus formula (humidity is clamped to at least 1% to avoid `log(0)`).
- Results are cached with `IMemoryCache`; failures are not cached.
- `AddStandardResilienceHandler()` (retry, circuit breaker, timeout) is applied to all `HttpClient` registrations through `ConfigureHttpClientDefaults`.

### Operations

- Structured JSON console logging (`AddJsonConsole`), suitable for containers.
- CORS is limited to the configured frontend origins.

## AI assistant configuration

[`AGENTS.md`](AGENTS.md) is the project-level instruction file for AI coding assistants. It defines the architectural guidelines, layer boundary rules, CQRS patterns, naming conventions, testing constraints, and commit style. [`CLAUDE.md`](CLAUDE.md) points to it so that Claude, Copilot, Cursor, and Cline share one source of truth.
