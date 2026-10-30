using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using WeatherApp.Api.Endpoints;
using WeatherApp.Api.Middleware;
using WeatherApp.Api.Options;
using WeatherApp.Application;
using WeatherApp.Application.Options;
using WeatherApp.Infrastructure;
using WeatherApp.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Configuration ---

// Untracked local overrides (e.g. OpenWeatherMap:ApiKey); see appsettings.Local.example.json.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddCommandLine(args);

// --- 2. Service Registrations ---

// Observability: structured JSON logs (container-friendly) with scopes, so each line carries TraceId/SpanId.
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options => options.IncludeScopes = true);

// Traces and metrics. OTEL_EXPORTER_OTLP_ENDPOINT is the standard OpenTelemetry key and is read by the SDK
// itself (appsettings.json or environment); telemetry is exported over OTLP only when it is set.
var telemetry = builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("WeatherApp.Api"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource(WeatherTelemetry.Name))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter(WeatherTelemetry.Name, "Polly"));
if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
{
    telemetry.UseOtlpExporter();
}

// Application and infrastructure layers (MediatR, validators, DbContext, repositories, weather HttpClient).
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Options validated at startup, so a bad configuration fails fast instead of at the first request.
builder.Services.AddOptions<CachingOptions>()
    .Bind(builder.Configuration.GetSection(CachingOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<RateLimitingOptions>()
    .Bind(builder.Configuration.GetSection(RateLimitingOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Standard resilience pipeline (retry, circuit breaker, timeouts) for all HttpClient clients,
// tuned through the "Resilience" section because the defaults need 100 requests to open the breaker.
var resilienceSection = builder.Configuration.GetSection("Resilience");
builder.Services.ConfigureHttpClientDefaults(clientBuilder =>
    clientBuilder.AddStandardResilienceHandler(resilienceSection));

// Standardizes unhandled exceptions and bare HTTP status codes into RFC 7807 Problem Details.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Per-client fixed-window rate limit protecting the shared OpenWeatherMap quota. Health probes are exempt.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = (context, _) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        return ValueTask.CompletedTask;
    };
});
builder.Services.AddOptions<RateLimiterOptions>()
    .Configure<IOptions<RateLimitingOptions>>((limiter, settings) =>
        limiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            httpContext.Request.Path.StartsWithSegments("/health")
                ? RateLimitPartition.GetNoLimiter("health")
                : RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = settings.Value.PermitLimit,
                        Window = TimeSpan.FromSeconds(settings.Value.WindowSeconds),
                        QueueLimit = 0
                    })));

// CORS policy for the frontend client, restricted to the configured origins.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

// Exposes operational endpoints for container orchestrators (liveness/readiness probes).
builder.Services.AddHealthChecks();

// OpenAPI document generation (exposed only in Development below).
builder.Services.AddOpenApi();

var app = builder.Build();

// Create and seed the shared in-memory SQLite database.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated();
}

// --- 3. Middleware Pipeline ---

// Order matters: errors first so they are formatted for every later stage, then CORS (so rejected
// preflight and 429 responses still carry CORS headers), then rate limiting before any endpoint work.
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors("AllowFrontend");
app.UseRateLimiter();

// Expose the OpenAPI specification only in Development to avoid information disclosure in Production.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// --- 4. Endpoint Routing ---

app.MapHealthChecks("/health");

// Maps domain-specific Minimal API endpoints via an extension method to keep Program.cs clean and modular.
app.MapWeatherEndpoints();

app.Run();
