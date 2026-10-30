using System.Net.Http.Json;
using FluentAssertions;
using WeatherApp.Application.Dtos;

namespace WeatherApp.Tests.Api;

public class WeatherEndToEndTests(FullStackWeatherApiFactory factory) : IClassFixture<FullStackWeatherApiFactory>
{
    [Fact]
    public async Task GetWeather_ReturnsAllRequiredMetricsWithFahrenheitConvertedToCelsius()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/weather/Jakarta");

        response.EnsureSuccessStatusCode();
        var weather = await response.Content.ReadFromJsonAsync<WeatherResponseDto>();
        weather.Should().NotBeNull();
        weather!.Location.Should().Be("Jakarta, ID");
        weather.TimeUtc.Should().Be(DateTimeOffset.FromUnixTimeSeconds(1760000000));
        weather.WindSpeed.Should().Be(5.5);
        weather.WindDirection.Should().Be(270);
        weather.Visibility.Should().Be(10000);
        weather.Pressure.Should().Be(1013);
        weather.SkyCondition.Should().Be("clear sky");
        weather.TempFahrenheit.Should().Be(68);
        weather.TempCelsius.Should().Be(20);
        weather.Humidity.Should().Be(50);
        weather.DewPoint.Should().BeApproximately(9.3, 0.1);
    }

    [Fact]
    public async Task GetWeather_UsesOnlyTheInMemoryHandler()
    {
        using var client = factory.CreateClient();

        await client.GetAsync("/api/weather/Surabaya");

        factory.Handler.Requests.Should().Contain(r => r.Contains("geo/1.0/direct") && r.Contains("Surabaya"));
        factory.Handler.Requests.Should().Contain(r => r.Contains("data/2.5/weather") && r.Contains("units=imperial"));
    }
}
