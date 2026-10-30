using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using WeatherApp.Application.Dtos;
using WeatherApp.Application.Interfaces;
using WeatherApp.Application.Options;

namespace WeatherApp.Application.Queries;

public sealed class GetCurrentWeatherQueryHandler(
    IWeatherService weatherService,
    IMemoryCache cache,
    IOptions<CachingOptions> cachingOptions)
    : IRequestHandler<GetCurrentWeatherQuery, WeatherResponseDto>
{
    public async Task<WeatherResponseDto> Handle(GetCurrentWeatherQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"weather:{request.CityName.Trim().ToLowerInvariant()}";

        if (cache.TryGetValue(cacheKey, out WeatherResponseDto? cached) && cached is not null)
        {
            WeatherTelemetry.CacheHits.Add(1);
            Activity.Current?.SetTag("weather.cache.hit", true);
            return cached;
        }

        WeatherTelemetry.CacheMisses.Add(1);
        Activity.Current?.SetTag("weather.cache.hit", false);

        using var activity = WeatherTelemetry.ActivitySource.StartActivity("FetchCurrentWeather");
        var weather = await weatherService.GetCurrentWeatherAsync(request.CityName, cancellationToken);

        var cacheDuration = TimeSpan.FromMinutes(cachingOptions.Value.WeatherCacheDurationMinutes);
        cache.Set(cacheKey, weather, cacheDuration);

        return weather;
    }
}
