using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using WeatherApp.Application.Dtos;
using WeatherApp.Application.Interfaces;
using WeatherApp.Application.Options;
using WeatherApp.Application.Queries;
using WeatherApp.Domain.Entities;

namespace WeatherApp.Tests.Application;

public class QueryHandlerTests
{
    [Fact]
    public async Task GetCountries_MapsRepositoryResultToDtos()
    {
        var repository = new Mock<ICountryRepository>();
        repository.Setup(r => r.GetCountriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Country { Code = "ID", Name = "Indonesia" }, new Country { Code = "JP", Name = "Japan" }]);

        var result = await new GetCountriesQueryHandler(repository.Object)
            .Handle(new GetCountriesQuery(), CancellationToken.None);

        result.Should().Equal(new CountryDto("ID", "Indonesia"), new CountryDto("JP", "Japan"));
    }

    [Fact]
    public async Task GetCitiesByCountry_PassesCountryCodeAndMapsResult()
    {
        var repository = new Mock<ICountryRepository>();
        repository.Setup(r => r.GetCitiesByCountryAsync("ID", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new City { Id = 1, CountryCode = "ID", Name = "Jakarta" }]);

        var result = await new GetCitiesByCountryQueryHandler(repository.Object)
            .Handle(new GetCitiesByCountryQuery("ID"), CancellationToken.None);

        result.Should().ContainSingle().Which.Should().Be(new CityDto(1, "ID", "Jakarta"));
        repository.Verify(r => r.GetCitiesByCountryAsync("ID", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCurrentWeather_CachesResultForSubsequentCalls()
    {
        var weather = new WeatherResponseDto { Location = "Jakarta, ID", TempCelsius = 30 };
        var service = new Mock<IWeatherService>();
        service.Setup(s => s.GetCurrentWeatherAsync("Jakarta", It.IsAny<CancellationToken>())).ReturnsAsync(weather);
        var handler = CreateWeatherHandler(service.Object, out var cache);

        var first = await handler.Handle(new GetCurrentWeatherQuery("Jakarta"), CancellationToken.None);
        var second = await handler.Handle(new GetCurrentWeatherQuery("  JAKARTA "), CancellationToken.None);

        first.Should().BeSameAs(weather);
        second.Should().BeSameAs(weather);
        service.Verify(s => s.GetCurrentWeatherAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        cache.Dispose();
    }

    [Fact]
    public async Task GetCurrentWeather_DifferentCities_CallServiceForEach()
    {
        var service = new Mock<IWeatherService>();
        service.Setup(s => s.GetCurrentWeatherAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WeatherResponseDto());
        var handler = CreateWeatherHandler(service.Object, out var cache);

        await handler.Handle(new GetCurrentWeatherQuery("Jakarta"), CancellationToken.None);
        await handler.Handle(new GetCurrentWeatherQuery("Tokyo"), CancellationToken.None);

        service.Verify(s => s.GetCurrentWeatherAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        cache.Dispose();
    }

    [Fact]
    public async Task GetCurrentWeather_ServiceFailure_IsNotCached()
    {
        var service = new Mock<IWeatherService>();
        service.SetupSequence(s => s.GetCurrentWeatherAsync("Jakarta", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("boom"))
            .ReturnsAsync(new WeatherResponseDto { Location = "Jakarta, ID" });
        var handler = CreateWeatherHandler(service.Object, out var cache);

        await handler.Invoking(h => h.Handle(new GetCurrentWeatherQuery("Jakarta"), CancellationToken.None))
            .Should().ThrowAsync<HttpRequestException>();
        var retry = await handler.Handle(new GetCurrentWeatherQuery("Jakarta"), CancellationToken.None);

        retry.Location.Should().Be("Jakarta, ID");
        cache.Dispose();
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("ID", true)]
    [InlineData("idn", false)]
    [InlineData("1D", false)]
    public void CitiesValidator_ChecksCountryCodeFormat(string code, bool expectedValid)
    {
        new GetCitiesByCountryQueryValidator().Validate(new GetCitiesByCountryQuery(code))
            .IsValid.Should().Be(expectedValid);
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("Jakarta", true)]
    [InlineData("MAX", true)]
    [InlineData("TOO_LONG", false)]
    public void WeatherValidator_RequiresCityNameWithinMaxLength(string city, bool expectedValid)
    {
        city = city switch
        {
            "MAX" => new string('a', 100),
            "TOO_LONG" => new string('a', 101),
            _ => city
        };

        new GetCurrentWeatherQueryValidator().Validate(new GetCurrentWeatherQuery(city))
            .IsValid.Should().Be(expectedValid);
    }

    private static GetCurrentWeatherQueryHandler CreateWeatherHandler(IWeatherService service, out MemoryCache cache)
    {
        cache = new MemoryCache(new MemoryCacheOptions());
        var options = Microsoft.Extensions.Options.Options.Create(new CachingOptions { WeatherCacheDurationMinutes = 5 });
        return new GetCurrentWeatherQueryHandler(service, cache, options);
    }
}
