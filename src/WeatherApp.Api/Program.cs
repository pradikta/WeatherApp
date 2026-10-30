using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using WeatherApp.Api.Endpoints;
using WeatherApp.Api.Middleware;
using WeatherApp.Application;
using WeatherApp.Application.Options;
using WeatherApp.Infrastructure;
using WeatherApp.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Untracked local overrides (e.g. OpenWeatherMap:ApiKey); see appsettings.Local.example.json.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddCommandLine(args);

// --- 1. Service Registrations ---

// Structured JSON console logging (container-friendly).
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options => options.IncludeScopes = true);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddOptions<CachingOptions>()
    .Bind(builder.Configuration.GetSection(CachingOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Standardizes unhandled exceptions and bare HTTP status codes into RFC 7807 Problem Details.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Exposes operational endpoints for container orchestrators (liveness/readiness probes).
builder.Services.AddHealthChecks();

// CORS policy for the frontend client, restricted to the configured origins.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

// Standard resilience pipeline (retry, circuit breaker, timeouts) for all HttpClient clients,
// tuned through the "Resilience" section because the defaults need 100 requests to open the breaker.
var resilienceSection = builder.Configuration.GetSection("Resilience");
builder.Services.ConfigureHttpClientDefaults(clientBuilder =>
    clientBuilder.AddStandardResilienceHandler(resilienceSection));

// Per-client fixed-window rate limit protecting the shared OpenWeatherMap quota. Health probes are exempt.
var rateLimiting = builder.Configuration.GetSection("RateLimiting");
var permitLimit = rateLimiting.GetValue("PermitLimit", 100);
var windowSeconds = rateLimiting.GetValue("WindowSeconds", 60);
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
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        httpContext.Request.Path.StartsWithSegments("/health")
            ? RateLimitPartition.GetNoLimiter("health")
            : RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = TimeSpan.FromSeconds(windowSeconds),
                    QueueLimit = 0
                }));
});

// Traces and metrics; exported over OTLP only when OTEL_EXPORTER_OTLP_ENDPOINT is set.
var otel = builder.Services.AddOpenTelemetry()
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
    otel.UseOtlpExporter();
}

// OpenAPI document generation (exposed only in Development below).
builder.Services.AddOpenApi();

var app = builder.Build();

// Create and seed the shared in-memory SQLite database.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated();
}

// --- 2. Middleware Pipeline ---

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors("AllowFrontend");
app.UseRateLimiter();

// Expose the OpenAPI specification only in Development to avoid information disclosure in Production.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthChecks("/health");

// --- 3. Endpoint Routing ---

// Maps domain-specific Minimal API endpoints via an extension method to keep Program.cs clean and modular.
app.MapWeatherEndpoints();

app.Run();


