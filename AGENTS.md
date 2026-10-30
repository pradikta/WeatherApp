# AGENTS.md - AI Agent Development Guide for WeatherApp

> **Note**: This file serves as the Single Source of Truth for AI coding agents (Cline, Cursor, Claude Code) working on WeatherApp.
> `CLAUDE.md` references this file.

## Quick Reference (MANDATORY RULES)

1. **Verification Discipline**: NEVER report build/test state based on cached output. Always execute a fresh `dotnet build` and `dotnet test` before declaring a task complete.
2. **Offline Testing First**: ALL automated unit and integration tests MUST run 100% offline with zero live HTTP calls.
3. **Strict Onion Boundaries**: `WeatherApp.Domain` MUST contain ZERO external NuGet dependencies and NO references to Infrastructure or Api packages.
4. **CQRS Read Optimization**: ALL EF Core read queries in MediatR Query Handlers MUST explicitly invoke `.AsNoTracking()`.
5. **Fahrenheit-to-Celsius Conversion**: Imperial weather payloads from OpenWeatherMap (`units=imperial`) MUST be converted strictly inside the `Temperature` Value Object in the Domain layer (`Math.Round((fahrenheit - 32) * 5 / 9, 2)`).
6. **Resilience & Caching**: All external HTTP calls MUST be cached (`IMemoryCache`) and wrapped in resilience policies (Retry/Circuit Breaker) to prevent rate-limiting and handle network faults.
7. **Code Hygiene**: Clean up unused `using` directives (`IDE0005`) on touched C# files prior to commit.
8. **Commits**: Follow Conventional Commits format (`feat(scope):`, `fix:`, `test:`, `chore:`). DO NOT use superlative adjectives.
9. **Observability**: Implement a MediatR `IPipelineBehavior` for global logging (Request/Response). Configure the Api to use structured JSON console logging (container-friendly). Traces, metrics, and logs use OpenTelemetry in the Api layer (logs also stay on the console); Application-layer telemetry uses only BCL `ActivitySource`/`Meter` (`WeatherTelemetry`).
10. **Time & Options**: Inject `TimeProvider` instead of calling `DateTime.UtcNow`. Options classes use data annotations with `ValidateOnStart()`.
11. **Shared Build Settings**: Put common project properties (target framework, nullable, warnings-as-errors) in `Directory.Build.props`, not in individual `.csproj` files.

---

## Naming Conventions & Code Style

- **Projects & Namespaces**: Use PascalCase for acronyms ≥ 3 letters (e.g., `WeatherApp.Api`, NOT `WeatherApp.API`).
- **Backend Classes/Records/Properties**: `PascalCase` (e.g., `WeatherResponseDto`, `Temperature`).
- **Backend Interfaces**: `PascalCase` prefixed with `I` (e.g., `IWeatherService`).
- **Backend Private Fields**: `_camelCase` prefixed with underscore (e.g., `_dbContext`).
- **Dependency Injection**: prefer primary constructors for DI-injected classes (e.g., `GetCurrentWeatherQueryHandler(IWeatherService weatherService)`).
- **Frontend Components**: `PascalCase` for component files and function names (e.g., `WeatherDashboard.tsx`, `LocationSelector.tsx`).
- **Frontend Hooks/Utils**: `camelCase` for non-component files, hooks, and helpers (e.g., `useWeatherApi.ts`, `formatters.ts`).
- **Async Methods**: MUST end with `Async` suffix in backend (e.g., `GetCurrentWeatherAsync`).
- **CQRS Objects**: MUST be suffixed explicitly with `Command`, `Query`, and `Handler`.

---

## Build & Verification Commands

- **Target Framework**: .NET 10 (`net10.0`) across all projects.
- **Solution**: `WeatherApp.sln` uses the classic `.sln` format (create with `dotnet new sln -n WeatherApp --format sln`; the .NET 10 SDK defaults to `.slnx`).
- **Restore**: `dotnet restore`
- **Build**: `dotnet build`
- **Run Api**: `dotnet run --project src/WeatherApp.Api`
- **Run Frontend**: `npm --prefix frontend run dev`
- **Tests**: `dotnet test`

---

## Layer Implementation Standards

### 1. Domain Layer (`src/WeatherApp.Domain/`)

- Pure C# only. **NO external NuGet packages allowed.**
- `Temperature` Value Object encapsulates calculation.
- Entities: `Country`, `City`, `WeatherNote`.

### 2. Application Layer (`src/WeatherApp.Application/`)

- CQRS via MediatR. Queries return DTOs.
- **Caching**: Inject and apply `IMemoryCache` for external API query results (cache duration dynamically configurable via `Caching` options).
- **LoggingBehavior**: Global interception of CQRS requests logging payload and stopwatch metrics.
- Commands mutate state and enforce validation via FluentValidation.

### 3. Infrastructure Layer (`src/WeatherApp.Infrastructure/`)

- `ApplicationDbContext` uses SQLite In-Memory (`DataSource=:memory:`) with a shared open connection.
- `OnModelCreating` seeds sample country and city data.
- All Read Queries MUST call `.AsNoTracking()`.
- `OpenWeatherMapService` implements `IWeatherService` via `HttpClient` (`units=imperial`), configured via the `OpenWeatherMap` config section (`BaseAddress`, `ApiKey`). Geocodes the city name to coordinates (the by-city-name query is deprecated), maps `Location` as "City, Country", and computes `DewPoint` (Magnus formula) because the API does not return it.

### 4. Api Layer (`src/WeatherApp.Api/`)

- Minimal APIs routing directly to MediatR pipeline.
- **Resilience**: MUST apply `Microsoft.Extensions.Http.Resilience` (`.AddStandardResilienceHandler()`) to all external `HttpClient` DI registrations, tuned via the `Resilience` config section (do not rely on library defaults).
- **Error Handling**: Map exceptions globally to RFC 7807 Problem Details format (`application/problem+json`) via `GlobalExceptionHandler` (validation → 400, `CityNotFoundException` → 404, upstream `HttpRequestException` → 502).
- **Local Secrets**: NEVER commit API keys. Set `OpenWeatherMap:ApiKey` in the git-ignored `src/WeatherApp.Api/appsettings.Local.json` (copy from `appsettings.Local.example.json`) or via environment variables.
- **CORS**: Configure and enable CORS policy to support frontend client integration.

### 5. Automated Tests (`tests/WeatherApp.Tests/`)

- Framework: xUnit (`[Fact]` / `[Theory]`).
- **Domain Logic Unit Tests**: test `Temperature` conversion edge cases (freezing 32°F → 0°C, boiling 212°F → 100°C, negative values).
- **CQRS Handler Tests**:
  - Query Handlers: unit test in isolation with mocked dependencies.
  - Command Handlers: unit test against EF Core In-Memory, verifying state changes and persistence rules.
- **API Endpoint / Integration Tests**: test Minimal API endpoints.
- **Execution Constraint**: ALL tests MUST run 100% offline with zero live network calls (mock `HttpClient` via a custom `HttpMessageHandler`).

---

## Frontend Architecture Standards (`frontend/src/`)

- **Structure**: Modular structure separating components (`components/`), types (`types/`), and services/api (`services/`).
- **Styling**: Tailwind CSS utility classes with clean card-based layouts, soft shadows, rounded corners, and consistent color themes.
- **Type Safety**: Strict TypeScript interfaces mapping directly to backend DTO payloads.
