using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Moq;
using WeatherApp.Application.Dtos;
using WeatherApp.Application.Exceptions;

namespace WeatherApp.Tests.Api;

public class WeatherEndpointsTests : IClassFixture<WeatherApiFactory>
{
    private readonly WeatherApiFactory _factory;
    private readonly HttpClient _client;

    public WeatherEndpointsTests(WeatherApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetCountries_ReturnsSeededCountries()
    {
        var response = await _client.GetAsync("/api/countries");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var countries = await response.Content.ReadFromJsonAsync<List<CountryDto>>();
        countries.Should().Contain(c => c.Code == "ID" && c.Name == "Indonesia");
    }

    [Fact]
    public async Task GetCities_ReturnsCitiesOfCountry()
    {
        var response = await _client.GetAsync("/api/countries/JP/cities");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cities = await response.Content.ReadFromJsonAsync<List<CityDto>>();
        cities.Should().Contain(c => c.Name == "Tokyo").And.OnlyContain(c => c.CountryCode == "JP");
    }

    [Theory]
    [InlineData("XXX")]
    [InlineData("1D")]
    public async Task GetCities_InvalidCountryCode_ReturnsValidationProblem(string code)
    {
        var response = await _client.GetAsync($"/api/countries/{code}/cities");

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("CountryCode");
    }

    [Fact]
    public async Task GetWeather_ReturnsWeatherPayload()
    {
        _factory.WeatherService
            .Setup(s => s.GetCurrentWeatherAsync("Jakarta", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WeatherResponseDto
            {
                Location = "Jakarta, ID",
                TempFahrenheit = 86,
                TempCelsius = 30,
                SkyCondition = "clear sky",
                Humidity = 70
            });

        var response = await _client.GetAsync("/api/weather/Jakarta");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var weather = await response.Content.ReadFromJsonAsync<WeatherResponseDto>();
        weather!.Location.Should().Be("Jakarta, ID");
        weather.TempFahrenheit.Should().Be(86);
        weather.TempCelsius.Should().Be(30);
    }

    [Fact]
    public async Task GetWeather_UnknownCity_ReturnsNotFoundProblem()
    {
        _factory.WeatherService
            .Setup(s => s.GetCurrentWeatherAsync("Nowhere", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CityNotFoundException("Nowhere"));

        var response = await _client.GetAsync("/api/weather/Nowhere");

        await AssertProblemAsync(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetWeather_UpstreamFailure_ReturnsBadGatewayProblem()
    {
        _factory.WeatherService
            .Setup(s => s.GetCurrentWeatherAsync("Down", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("upstream unavailable"));

        var response = await _client.GetAsync("/api/weather/Down");

        await AssertProblemAsync(response, HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task GetWeather_UnexpectedFailure_ReturnsInternalServerErrorProblem()
    {
        _factory.WeatherService
            .Setup(s => s.GetCurrentWeatherAsync("Broken", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("secret internal detail"));

        var response = await _client.GetAsync("/api/weather/Broken");

        await AssertProblemAsync(response, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task PostNote_ValidPayload_ReturnsCreatedWithIdentifierAndLocation()
    {
        var response = await _client.PostAsJsonAsync("/api/weather/notes", new { cityName = "Jakarta", note = "Humid" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<CreateWeatherNoteDto>();
        created!.Id.Should().NotBeEmpty();
        created.CityName.Should().Be("Jakarta");
        created.Note.Should().Be("Humid");
        response.Headers.Location!.ToString().Should().Be($"/api/weather/notes/{created.Id}");
    }

    [Theory]
    [InlineData("", "note", "CityName")]
    [InlineData("Jakarta", "", "Note")]
    public async Task PostNote_InvalidPayload_ReturnsValidationProblem(string city, string note, string field)
    {
        var response = await _client.PostAsJsonAsync("/api/weather/notes", new { cityName = city, note });

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("errors").TryGetProperty(field, out _).Should().BeTrue();
    }

    [Fact]
    public async Task Cors_AllowsConfiguredFrontendOriginOnly()
    {
        using var allowed = new HttpRequestMessage(HttpMethod.Get, "/api/countries");
        allowed.Headers.Add("Origin", "http://localhost:5173");
        using var blocked = new HttpRequestMessage(HttpMethod.Get, "/api/countries");
        blocked.Headers.Add("Origin", "http://evil.example");

        var allowedResponse = await _client.SendAsync(allowed);
        var blockedResponse = await _client.SendAsync(blocked);

        allowedResponse.Headers.GetValues("Access-Control-Allow-Origin").Should().Equal("http://localhost:5173");
        blockedResponse.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        response.StatusCode.Should().Be(expected);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("status").GetInt32().Should().Be((int)expected);
    }
}
