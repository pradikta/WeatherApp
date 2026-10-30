namespace WeatherApp.Application.Exceptions;

public sealed class CityNotFoundException(string cityName)
    : Exception($"City '{cityName}' was not found.")
{
    public string CityName { get; } = cityName;
}
