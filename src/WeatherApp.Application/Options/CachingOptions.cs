namespace WeatherApp.Application.Options;

public sealed class CachingOptions
{
    public const string SectionName = "Caching";

    public int WeatherCacheDurationMinutes { get; set; } = 1;
}
