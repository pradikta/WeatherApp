using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using WeatherApp.Application.Dtos;
using WeatherApp.Application.Exceptions;
using WeatherApp.Application.Interfaces;
using WeatherApp.Domain.ValueObjects;

namespace WeatherApp.Infrastructure.Services;

public sealed class OpenWeatherMapService(HttpClient httpClient, IOptions<OpenWeatherMapOptions> options) : IWeatherService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<WeatherResponseDto> GetCurrentWeatherAsync(string cityName, CancellationToken cancellationToken)
    {
        var apiKey = options.Value.ApiKey;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "OpenWeatherMap:ApiKey is not configured. Set it in appsettings.Local.json or via environment variables.");
        }

        // The by-city-name weather query is deprecated, so geocode the city name to
        // coordinates first and then query current weather by latitude/longitude.
        var geocodeUrl = $"geo/1.0/direct?q={Uri.EscapeDataString(cityName)}&limit=1&appid={apiKey}";
        var locations = await httpClient.GetFromJsonAsync<List<GeocodeResponse>>(geocodeUrl, JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Received an empty response from the OpenWeatherMap geocoding API.");

        var location = locations.FirstOrDefault()
            ?? throw new CityNotFoundException(cityName);

        var lat = location.Lat.ToString(CultureInfo.InvariantCulture);
        var lon = location.Lon.ToString(CultureInfo.InvariantCulture);
        var weatherUrl = $"data/2.5/weather?lat={lat}&lon={lon}&appid={apiKey}&units=imperial";
        var response = await httpClient.GetFromJsonAsync<OpenWeatherMapResponse>(weatherUrl, JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Received an empty response from OpenWeatherMap.");

        var temperature = new Temperature(response.Main.Temp);

        return new WeatherResponseDto
        {
            // The geocoder may return a long official name (e.g. "Special Capital Region of Jakarta"),
            // so keep the requested city name for a concise "City, Country" label.
            Location = $"{cityName.Trim()}, {location.Country}",
            TimeUtc = DateTimeOffset.FromUnixTimeSeconds(response.Dt),
            WindSpeed = response.Wind?.Speed,
            WindDirection = response.Wind?.Deg,
            Visibility = response.Visibility,
            Pressure = response.Main.Pressure,
            SkyCondition = response.Weather.FirstOrDefault()?.Description ?? string.Empty,
            TempFahrenheit = response.Main.Temp,
            TempCelsius = temperature.Celsius,
            DewPoint = CalculateDewPoint(temperature.Celsius, response.Main.Humidity),
            Humidity = response.Main.Humidity
        };
    }

    /// <summary>
    /// Approximates the dew point in Celsius using the Magnus-Tetens formula, since the
    /// OpenWeatherMap current-weather endpoint does not return it.
    /// </summary>
    /// <param name="celsius">Air temperature in degrees Celsius.</param>
    /// <param name="humidity">Relative humidity as a percentage (0-100).</param>
    internal static double CalculateDewPoint(double celsius, double humidity)
    {
        const double a = 17.625;
        const double b = 243.04;

        // Clamp to avoid Math.Log(0) producing NaN/-Infinity at 0% humidity.
        var gamma = Math.Log(Math.Clamp(humidity, 1, 100) / 100) + (a * celsius) / (b + celsius);

        return b * gamma / (a - gamma);
    }

    private sealed record GeocodeResponse
    {
        public string Name { get; init; } = string.Empty;
        public double Lat { get; init; }
        public double Lon { get; init; }
        public string Country { get; init; } = string.Empty;
    }

    private sealed record OpenWeatherMapResponse
    {
        public MainInfo Main { get; init; } = new();
        public List<WeatherInfo> Weather { get; init; } = [];
        public WindInfo? Wind { get; init; }
        public long Dt { get; init; }
        public int? Visibility { get; init; }
    }

    private sealed record MainInfo
    {
        public double Temp { get; init; }
        public double Pressure { get; init; }
        public int Humidity { get; init; }
    }

    private sealed record WeatherInfo
    {
        public string Description { get; init; } = string.Empty;
    }

    private sealed record WindInfo
    {
        public double? Speed { get; init; }
        public double? Deg { get; init; }
    }
}
