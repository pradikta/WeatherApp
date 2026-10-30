using MediatR;
using Microsoft.Extensions.Caching.Memory;
using WeatherApp.Application.Dtos;
using WeatherApp.Application.Interfaces;

namespace WeatherApp.Application.Queries;

public sealed class GetCurrentWeatherQueryHandler(IWeatherService weatherService, IMemoryCache cache)
    : IRequestHandler<GetCurrentWeatherQuery, WeatherResponseDto>
{
    public async Task<WeatherResponseDto> Handle(GetCurrentWeatherQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"weather:{request.CityName.Trim().ToLowerInvariant()}";

        if (cache.TryGetValue(cacheKey, out WeatherResponseDto? cached) && cached is not null)
        {
            return cached;
        }

        var weather = await weatherService.GetCurrentWeatherAsync(request.CityName, cancellationToken);

        cache.Set(cacheKey, weather, TimeSpan.FromMinutes(10));

        return weather;
    }
}
