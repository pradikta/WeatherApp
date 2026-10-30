using System.ComponentModel.DataAnnotations;

namespace WeatherApp.Application.Options;

public sealed class CachingOptions
{
    public const string SectionName = "Caching";

    [Range(1, 1440)]
    public int WeatherCacheDurationMinutes { get; set; } = 1;
}
