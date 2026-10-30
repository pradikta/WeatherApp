using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace WeatherApp.Application;

/// <summary>
/// Application-level telemetry. Uses only BCL types so the layer stays free of
/// OpenTelemetry dependencies; the Api layer subscribes to <see cref="Name"/>.
/// </summary>
public static class WeatherTelemetry
{
    public const string Name = "WeatherApp";

    public static readonly ActivitySource ActivitySource = new(Name);

    private static readonly Meter Meter = new(Name);

    public static readonly Counter<long> CacheHits =
        Meter.CreateCounter<long>("weather.cache.hits", description: "Weather queries served from the memory cache.");

    public static readonly Counter<long> CacheMisses =
        Meter.CreateCounter<long>("weather.cache.misses", description: "Weather queries that called the upstream service.");

    public static readonly Counter<long> NotesCreated =
        Meter.CreateCounter<long>("weather.notes.created", description: "Weather notes persisted.");
}
