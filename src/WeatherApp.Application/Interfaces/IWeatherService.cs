using WeatherApp.Application.Dtos;

namespace WeatherApp.Application.Interfaces;

public interface IWeatherService
{
    Task<WeatherResponseDto> GetCurrentWeatherAsync(string cityName, CancellationToken cancellationToken);
}
