using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Options;
using WeatherApp.Application.Exceptions;
using WeatherApp.Infrastructure.Services;

namespace WeatherApp.Tests.Infrastructure;

public class OpenWeatherMapServiceTests
{
    private const string GeocodeJson = """[{"name":"Jakarta","lat":-6.2,"lon":106.8,"country":"ID"}]""";

    private const string WeatherJson = """
        {
          "weather":[{"id":800,"main":"Clear","description":"clear sky"}],
          "main":{"temp":68,"pressure":1013,"humidity":50},
          "visibility":10000,
          "wind":{"speed":5.5,"deg":270},
          "dt":1760000000
        }
        """;

    [Fact]
    public async Task GetCurrentWeatherAsync_MapsGeocodeAndWeatherPayload()
    {
        var handler = new StubHttpMessageHandler(request =>
            request.RequestUri!.AbsolutePath.StartsWith("/geo/") ? Json(GeocodeJson) : Json(WeatherJson));

        var result = await CreateService(handler).GetCurrentWeatherAsync("Jakarta", CancellationToken.None);

        result.Location.Should().Be("Jakarta, ID");
        result.TimeUtc.Should().Be(DateTimeOffset.FromUnixTimeSeconds(1760000000));
        result.TempFahrenheit.Should().Be(68);
        result.TempCelsius.Should().Be(20);
        result.WindSpeed.Should().Be(5.5);
        result.WindDirection.Should().Be(270);
        result.Visibility.Should().Be(10000);
        result.Pressure.Should().Be(1013);
        result.SkyCondition.Should().Be("clear sky");
        result.Humidity.Should().Be(50);
        result.DewPoint.Should().BeApproximately(9.3, 0.1);
    }

    [Fact]
    public async Task GetCurrentWeatherAsync_RequestsImperialUnitsAndGeocodedCoordinates()
    {
        var handler = new StubHttpMessageHandler(request =>
            request.RequestUri!.AbsolutePath.StartsWith("/geo/") ? Json(GeocodeJson) : Json(WeatherJson));

        await CreateService(handler).GetCurrentWeatherAsync("New York", CancellationToken.None);

        handler.Requests.Should().HaveCount(2);
        handler.Requests[0].Should().Contain("geo/1.0/direct").And.Contain("q=New%20York").And.Contain("appid=test-key");
        handler.Requests[1].Should().Contain("data/2.5/weather").And.Contain("units=imperial")
            .And.Contain("lat=-6.2").And.Contain("lon=106.8");
    }

    [Fact]
    public async Task GetCurrentWeatherAsync_UnknownCity_ThrowsCityNotFoundException()
    {
        var handler = new StubHttpMessageHandler(_ => Json("[]"));

        var act = () => CreateService(handler).GetCurrentWeatherAsync("Nowhere", CancellationToken.None);

        (await act.Should().ThrowAsync<CityNotFoundException>()).Which.CityName.Should().Be("Nowhere");
        handler.Requests.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetCurrentWeatherAsync_UpstreamError_ThrowsHttpRequestException()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));

        var act = () => CreateService(handler).GetCurrentWeatherAsync("Jakarta", CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetCurrentWeatherAsync_MissingApiKey_FailsWithoutCallingNetwork(string apiKey)
    {
        var handler = new StubHttpMessageHandler(_ => Json(GeocodeJson));

        var act = () => CreateService(handler, apiKey).GetCurrentWeatherAsync("Jakarta", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*ApiKey*");
        handler.Requests.Should().BeEmpty();
    }

    [Theory]
    [InlineData(20, 50, 9.3)]
    [InlineData(30, 100, 30)]
    [InlineData(0, 50, -9.2)]
    public void CalculateDewPoint_UsesMagnusFormula(double celsius, double humidity, double expected)
    {
        OpenWeatherMapService.CalculateDewPoint(celsius, humidity).Should().BeApproximately(expected, 0.1);
    }

    [Fact]
    public void CalculateDewPoint_ZeroHumidity_ReturnsFiniteValue()
    {
        double.IsFinite(OpenWeatherMapService.CalculateDewPoint(20, 0)).Should().BeTrue();
    }

    private static OpenWeatherMapService CreateService(StubHttpMessageHandler handler, string apiKey = "test-key")
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.openweathermap.test/") };
        var options = Options.Create(new OpenWeatherMapOptions { ApiKey = apiKey });
        return new OpenWeatherMapService(client, options);
    }

    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    // Answers every request from memory, so no test can reach the network.
    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public List<string> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri!.PathAndQuery);
            return Task.FromResult(responder(request));
        }
    }
}
