namespace WeatherApp.Infrastructure.Services;

public sealed class OpenWeatherMapOptions
{
    public const string SectionName = "OpenWeatherMap";

    public string BaseAddress { get; set; } = "https://api.openweathermap.org/";
    public string ApiKey { get; set; } = string.Empty;
}
