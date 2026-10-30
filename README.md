# WeatherApp - Clean Architecture .NET 10 API

ASP.NET Core (.NET 10) Web API plus a React + TypeScript client that let a user pick a country and city, view current weather from OpenWeatherMap, and save custom weather notes. Built with Onion Architecture and CQRS (MediatR).

## Architecture

The solution follows Onion Architecture: source dependencies point inward only.

```text
Api ──► Application ──► Domain
 │          ▲
 └──► Infrastructure ──┘   (Infrastructure implements Application interfaces)
```

| Project | Responsibility |
| --- | --- |
| `WeatherApp.Domain` | Entities (`Country`, `City`, `WeatherNote`) and the `Temperature` value object. Pure C#, no NuGet packages, no references to other projects. |
| `WeatherApp.Application` | CQRS requests and handlers (MediatR), DTOs, FluentValidation validators, pipeline behaviors, and abstractions (`IWeatherService`, `ICountryRepository`, `IWeatherNoteRepository`). |
| `WeatherApp.Infrastructure` | EF Core `ApplicationDbContext` (SQLite in-memory), repositories, and `OpenWeatherMapService` (HTTP client). |
| `WeatherApp.Api` | Thin Minimal API layer. Endpoints only send requests through MediatR `ISender`. Also hosts DI wiring, CORS, logging, and exception handling. |

### CQRS decoupling

- **Queries** (read side) return DTOs and never mutate state. Read queries through EF Core use `.AsNoTracking()`.
- **Commands** (write side) validate input, then persist through a repository.
- Endpoints know nothing about EF Core or HTTP clients; they depend only on MediatR. Handlers know only interfaces, so they are unit-tested with mocks or an in-memory database.
- Two MediatR pipeline behaviors wrap every request: `ValidationBehavior` (FluentValidation, throws `ValidationException`) and `LoggingBehavior` (payload and elapsed time).

### Naming conventions

| Item | Convention | Example |
| --- | --- | --- |
| Projects and namespaces | PascalCase, even for acronyms of 3+ letters | `WeatherApp.Api` (not `WeatherApp.API`) |
| Classes, records, properties | PascalCase | `WeatherResponseDto`, `Temperature` |
| Interfaces | `I` prefix | `IWeatherService` |
| Private fields | `_camelCase` | `_dbContext` |
| Async methods | `Async` suffix | `GetCurrentWeatherAsync` |
| CQRS objects | `Command` / `Query` / `Handler` suffix | `GetCurrentWeatherQuery`, `GetCurrentWeatherQueryHandler` |
| Dependency injection | Primary constructors | `GetCurrentWeatherQueryHandler(IWeatherService weatherService)` |

### Domain business logic

OpenWeatherMap is called with `units=imperial`. The Fahrenheit-to-Celsius conversion lives only in the domain `Temperature` value object (`Math.Round((fahrenheit - 32) * 5 / 9, 2)`), so no other layer re-implements it.

### Performance, Resilience and Observability

- **Caching**: `IMemoryCache` caches weather results per city (`Caching:WeatherCacheDurationMinutes`); failures are never cached.
- **Resilience**: `Microsoft.Extensions.Http.Resilience` (`AddStandardResilienceHandler()`) gives every external `HttpClient` a retry policy, a circuit breaker, and timeouts.
- **Pipeline logging**: the MediatR `LoggingBehavior` logs every request and response with elapsed time.
- **Structured logging**: JSON console logging to stdout (container friendly), with `TraceId`/`SpanId` on each line.
- **Telemetry**: OpenTelemetry traces, metrics, and logs over OTLP (section 5 of Setup).

### Error handling and security

- Every error is returned as RFC 7807 Problem Details (`application/problem+json`); see [Errors](#errors).
- CORS only allows the origins in `Cors:AllowedOrigins`. Per-IP rate limiting protects the OpenWeatherMap quota. The API key is never committed (git-ignored `appsettings.Local.json` or environment variables) and does not appear in logs or trace URLs.

### Frontend best practices

The React client in `frontend/src` is split into `components/` (PascalCase files such as `WeatherCard.tsx`), `hooks/` and `utils/` (camelCase such as `useWeatherApi.ts`, `formatters.ts`), `services/` (typed API client), and `types/` (strict TypeScript interfaces that mirror the backend DTOs). Styling uses Tailwind CSS utility classes.

## API

| Method | Route | Description |
| --- | --- | --- |
| GET | `/api/countries` | All countries, ordered by name. |
| GET | `/api/countries/{countryCode}/cities` | Cities of a country (2-letter ISO code). |
| GET | `/api/weather/{cityName}` | Current weather for a city. |
| POST | `/api/weather/notes` | Saves a note. Body: `{ "cityName": "...", "note": "..." }`. Returns `201` with the created `id`. The city must exist in the seeded data (matched case-insensitively and stored with its canonical name); otherwise `404`. |
| GET | `/health` | Health check. |
| GET | `/openapi/v1.json` | OpenAPI document (Development only). |

The weather response contains: `location` ("City, Country"), `timeUtc`, `windSpeed`, `windDirection`, `visibility`, `pressure`, `skyCondition`, `tempFahrenheit`, `tempCelsius`, `dewPoint`, `humidity`. `windSpeed`, `windDirection`, and `visibility` are `null` when OpenWeatherMap omits them, instead of a misleading `0`.

### Errors

All errors use RFC 7807 Problem Details (`application/problem+json`):

| Cause | Status |
| --- | --- |
| Validation failure (includes per-field `errors`) | 400 |
| City not found | 404 |
| OpenWeatherMap unreachable or rejected the request (after retries) | 502 |
| Circuit breaker open (calls paused after repeated failures) | 503 |
| OpenWeatherMap did not respond in time | 504 |
| Rate limit exceeded (includes `Retry-After`) | 429 |
| Any other unhandled exception | 500 |

## Setup

Requirements: [.NET 10 SDK](https://dotnet.microsoft.com/download), Node.js 20+ for the frontend, Docker (only for the optional observability stack and image), and a free [OpenWeatherMap API key](https://openweathermap.org/api).

### Quick start (local)

1. Create `src/WeatherApp.Api/appsettings.Local.json` with your API key (section 1).
2. *(Optional)* Start the observability stack: `docker compose -f observability/docker-compose.yml up -d` (section 5). Skip it if you do not need traces, metrics, or logs.
3. Run the API from the repository root: `dotnet run --project src/WeatherApp.Api`.
4. Run the frontend in a second terminal: `npm --prefix frontend install`, then `npm --prefix frontend run dev`.
5. Open the app.

| What | URL |
| --- | --- |
| Frontend | <http://localhost:5173> |
| API | <http://localhost:5000> (health check at `/health`) |
| Grafana (logs, traces, metrics) | <http://localhost:3001> |
| Jaeger / Prometheus | <http://localhost:16686> / <http://localhost:9090> |

If `OTEL_EXPORTER_OTLP_ENDPOINT` is set but the stack is not running, the API still works; telemetry is simply not delivered.

### 1. Configure the API key (never commit it)

Copy the template and fill in your key:

```powershell
Copy-Item src/WeatherApp.Api/appsettings.Local.example.json src/WeatherApp.Api/appsettings.Local.json
```

```json
{
  "OpenWeatherMap": {
    "ApiKey": "REPLACE_WITH_OPENWEATHERMAP_API_KEY"
  },
  "OTEL_EXPORTER_OTLP_ENDPOINT": ""
}
```

`appsettings.Local.json` is git-ignored and loaded automatically when present. Environment variables and command-line arguments override it, for example `OpenWeatherMap__ApiKey=...`. Leave `OTEL_EXPORTER_OTLP_ENDPOINT` empty to disable telemetry export, or set it to `http://localhost:4317` to send it to the local observability stack (section 5).

Other settings in `appsettings.json`:

| Key | Purpose | Default |
| --- | --- | --- |
| `OpenWeatherMap:BaseAddress` | OpenWeatherMap base URL | `https://api.openweathermap.org/` |
| `Caching:WeatherCacheDurationMinutes` | How long a city's weather is cached | `1` |
| `Cors:AllowedOrigins` | Origins allowed to call the API | `http://localhost:5173` |
| `Resilience:*` | Retry, circuit breaker, and timeout tuning (see below) | 2 retries, breaker opens at 50% failures over 10+ calls |
| `RateLimiting:PermitLimit` / `WindowSeconds` | Fixed-window limit per client IP (`/health` is exempt) | `100` / `60` |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Standard OpenTelemetry key, read by the SDK from `appsettings.json` or the environment. OTLP collector for traces, metrics, and logs; nothing is exported when empty | empty |

Invalid `OpenWeatherMap:BaseAddress` or `Caching` values stop the app at startup (`ValidateOnStart`). A missing API key does not: the app boots, and the weather endpoint returns `500` with the reason in the log.

### 2. Build, run, test

```powershell
dotnet restore
dotnet build
dotnet run --project src/WeatherApp.Api   # http://localhost:5000
dotnet test
```

Sample requests for every endpoint are in [src/WeatherApp.Api/WeatherApp.Api.http](src/WeatherApp.Api/WeatherApp.Api.http) (VS Code needs the REST Client extension `humao.rest-client`, which is recommended in `.vscode/extensions.json`; Visual Studio and Rider support `.http` files natively). Run the API first.

### 3. Frontend

Requires Node.js 20+ and a running API on `http://localhost:5000`.

```powershell
cd frontend
npm install
npm run dev     # http://localhost:5173
npm run build   # type-check and production build into frontend/dist
npm test        # Vitest unit tests (offline, fetch is mocked)
```

The client is built with Vite, React, TypeScript, and Tailwind CSS. The dev server proxies `/api` to `http://localhost:5000` (see `frontend/vite.config.ts`), so no CORS setup is needed during development. Layout:

- `src/components/` presentational components: `LocationSelector` (cascading country/city dropdowns), `WeatherCard` (hero card and metric tiles), `WeatherDetails`, `WeatherNoteForm`.
- `src/services/weatherApi.ts` typed `fetch` client that turns RFC 7807 problem details (400, 404, 429, 502, 503, 504) into user-facing messages.
- `src/hooks/` data hooks, `src/types/` DTO interfaces, `src/utils/formatters.ts` display helpers.

Wind speed is converted from the API's mph to km/h, and visibility is shown in km or m. Wind and visibility show `N/A` when OpenWeatherMap omits them.

### 4. Docker (optional)

```powershell
docker build -t weatherapp .
docker run --rm -p 8080:8080 -e OpenWeatherMap__ApiKey=<your key> weatherapp
```

The image runs as a non-root user on port 8080. TLS is expected to terminate at a reverse proxy or ingress, so the app itself does not redirect to HTTPS.

#### Docker Compose (API + frontend)

The root `docker-compose.yml` runs the API and the frontend (nginx serving the built client and proxying `/api` to the API, so no CORS is needed). Provide the key through the environment or a git-ignored `.env` file (template: `.env.example`):

```powershell
Copy-Item .env.example .env   # then edit the key
docker compose up --build     # http://localhost:5173
docker compose down
```

### 5. Local observability (optional)

A Docker Compose stack in `observability/` runs an OpenTelemetry Collector, Jaeger (traces), Prometheus (metrics), Loki (logs), and Grafana (UI over all three) so telemetry can be inspected locally. It is for development only: no real authentication (Grafana allows anonymous admin access) and no persistence.

```powershell
docker compose -f observability/docker-compose.yml up -d
```

Then enable the OTLP endpoint (gRPC, the SDK default) either in the git-ignored `src/WeatherApp.Api/appsettings.Local.json`, so it is active every time the API starts:

```json
{ "OTEL_EXPORTER_OTLP_ENDPOINT": "http://localhost:4317" }
```

or for a single PowerShell session only:

```powershell
$env:OTEL_EXPORTER_OTLP_ENDPOINT = 'http://localhost:4317'
dotnet run --project src/WeatherApp.Api
```

| Tool | URL | What to look at |
| --- | --- | --- |
| Grafana | <http://localhost:3001> | No login. **Explore** with the Loki datasource, for example `{service_name="WeatherApp.Api"}` or `{service_name="WeatherApp.Api"} \| detected_level="error"`. The `TraceId` field on a log line links to the trace in Jaeger. Prometheus and Jaeger are provisioned as datasources too. |
| Jaeger | <http://localhost:16686> | Service `WeatherApp.Api`. One weather request is one trace: the request, `FetchCurrentWeather`, the geocoding call, and the weather call as separate spans (one per retry). |
| Prometheus | <http://localhost:9090> | `weather_cache_hits_total`, `weather_cache_misses_total`, `http_server_request_duration_seconds_count`, `http_client_request_duration_seconds_count`. |

Grafana is published on host port 3001 because 3000 is commonly taken by other local apps. Metrics are exported every 60 seconds, so allow up to a minute before they appear in Prometheus. Logs also stay on the API's stdout as JSON. Loki has no UI of its own (its API is on port 3100), so use Grafana to read logs. Stop the stack with `docker compose -f observability/docker-compose.yml down`.

## Testing

Tests live in `tests/WeatherApp.Tests` (xUnit, Moq, FluentAssertions) and run **fully offline**: no test calls the network. External HTTP is replaced by an in-memory `HttpMessageHandler`.

| Area | What is verified |
| --- | --- |
| Domain | `Temperature` at freezing (32°F), boiling (212°F), negative values, absolute zero, and rounding. |
| Query handlers | Isolated with mocked dependencies, including weather caching. |
| Command handler | Against SQLite in-memory: the note is persisted, read back through a fresh context, and gets a unique id. |
| Repositories and seed data | Ordering, `AsNoTracking`, and seed integrity. |
| `OpenWeatherMapService` | Mocked JSON payloads, request URLs, error and edge cases. |
| API endpoints | `WebApplicationFactory`: status codes, problem details, CORS, and a full-stack weather flow. |
| Architecture | Dependency rule between layers. |
| Hardening | Rate limiting (429 + `Retry-After`, `/health` exempt), startup option validation, telemetry counters/activity, and OTLP log export against a loopback collector. |

CI (`.github/workflows/ci.yml`) runs on every push and pull request to `main`: `dotnet restore`, `dotnet build --no-restore`, `dotnet test --no-build` on `ubuntu-24.04`, plus frontend lint, tests, and build, and a `docker compose build` check.

Retry, circuit breaker, and timeout behavior is verified against a failing in-memory upstream (`ResilienceTests`).

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
- `AddStandardResilienceHandler()` (retry, circuit breaker, timeout) is applied to all `HttpClient` registrations through `ConfigureHttpClientDefaults` and bound to the `Resilience` section. The library defaults need 100 requests in 30 seconds before the breaker can open, which this app would never reach, so `appsettings.json` lowers that to 10. Each weather request makes two upstream calls (geocoding, then weather), so keep `Retry:MaxRetryAttempts` low to protect the API key quota.

### Operations

- Structured JSON console logging (`AddJsonConsole` with scopes) is always on and writes to stdout, so each log line carries `TraceId` and `SpanId` for correlation with traces. Container platforms and log shippers can collect stdout directly.
- OpenTelemetry traces (ASP.NET Core, `HttpClient`, and a `FetchCurrentWeather` activity). One request is one trace: the geocoding call and the weather call are separate `HttpClient` spans (with status code, error status, and one span per retry), so a failing upstream call can be pinpointed. Metrics (ASP.NET Core, `HttpClient`, runtime, resilience, plus `weather.cache.hits`, `weather.cache.misses`, `weather.notes.created`), and logs. Application-layer telemetry uses only BCL types (`ActivitySource`, `Meter`), so that layer has no OpenTelemetry dependency. All three signals are exported over OTLP, only when `OTEL_EXPORTER_OTLP_ENDPOINT` is set; point it at an OpenTelemetry Collector or a backend that accepts OTLP and forward from there to the aggregator of your choice.
- Per-IP rate limiting protects the OpenWeatherMap quota. Behind a reverse proxy, configure forwarded headers so the client IP is not the proxy IP.
- `TimeProvider` is injected wherever the current time is needed, so time is deterministic in tests.
- CORS is limited to the configured frontend origins.
- `Directory.Build.props` centralizes shared settings and treats warnings as errors.

## AI assistant configuration

[`AGENTS.md`](AGENTS.md) is the project-level instruction file for AI coding assistants. It defines the architectural guidelines, layer boundary rules, CQRS patterns, naming conventions, testing constraints, and commit style. [`CLAUDE.md`](CLAUDE.md) points to it so that Claude, Copilot, Cursor, and Cline share one source of truth.
