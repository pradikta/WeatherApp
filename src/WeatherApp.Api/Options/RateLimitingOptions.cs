using System.ComponentModel.DataAnnotations;

namespace WeatherApp.Api.Options;

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    [Range(1, 100_000)]
    public int PermitLimit { get; set; } = 100;

    [Range(1, 86_400)]
    public int WindowSeconds { get; set; } = 60;
}
