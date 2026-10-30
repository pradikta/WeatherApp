using System.ComponentModel.DataAnnotations;

namespace WeatherApp.Infrastructure.Services;

public sealed class OpenWeatherMapOptions
{
    public const string SectionName = "OpenWeatherMap";

    [Required, Url]
    public string BaseAddress { get; set; } = "https://api.openweathermap.org/";

    // Not validated at startup on purpose: the app must boot without a key so health
    // checks and tests work; a missing key fails the weather call instead.
    public string ApiKey { get; set; } = string.Empty;
}
