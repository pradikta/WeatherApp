using WeatherApp.Api.Endpoints;
using WeatherApp.Application;
using WeatherApp.Application.Options;
using WeatherApp.Infrastructure;
using WeatherApp.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Service Registrations ---

// Structured JSON console logging (container-friendly).
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.Configure<CachingOptions>(builder.Configuration.GetSection(CachingOptions.SectionName));

// Standardizes unhandled exceptions and bare HTTP status codes into RFC 7807 Problem Details.
builder.Services.AddProblemDetails();

// Exposes operational endpoints for container orchestrators (liveness/readiness probes).
builder.Services.AddHealthChecks();

// CORS policy for the frontend client.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
});

// Apply the standard resilience pipeline (retry + circuit breaker) to all HttpClient clients.
builder.Services.ConfigureHttpClientDefaults(clientBuilder =>
    clientBuilder.AddStandardResilienceHandler());

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


