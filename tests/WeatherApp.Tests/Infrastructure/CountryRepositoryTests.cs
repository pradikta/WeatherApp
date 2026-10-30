using FluentAssertions;
using WeatherApp.Infrastructure.Repositories;
using WeatherApp.Tests.Support;

namespace WeatherApp.Tests.Infrastructure;

public class CountryRepositoryTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task GetCountries_ReturnsSeededCountriesOrderedByNameWithoutTracking()
    {
        await using var context = _database.CreateContext();

        var countries = await new CountryRepository(context).GetCountriesAsync(CancellationToken.None);

        countries.Should().NotBeEmpty();
        countries.Select(c => c.Name).Should().BeInAscendingOrder();
        countries.Should().Contain(c => c.Code == "ID" && c.Name == "Indonesia");
        context.ChangeTracker.Entries().Should().BeEmpty("read queries must use AsNoTracking");
    }

    [Fact]
    public async Task GetCitiesByCountry_ReturnsOnlyCitiesOfThatCountryWithoutTracking()
    {
        await using var context = _database.CreateContext();

        var cities = await new CountryRepository(context).GetCitiesByCountryAsync("ID", CancellationToken.None);

        cities.Should().Contain(c => c.Name == "Jakarta");
        cities.Should().OnlyContain(c => c.CountryCode == "ID");
        cities.Select(c => c.Name).Should().BeInAscendingOrder();
        context.ChangeTracker.Entries().Should().BeEmpty("read queries must use AsNoTracking");
    }

    [Fact]
    public async Task GetCitiesByCountry_UnknownCountry_ReturnsEmpty()
    {
        await using var context = _database.CreateContext();

        var cities = await new CountryRepository(context).GetCitiesByCountryAsync("ZZ", CancellationToken.None);

        cities.Should().BeEmpty();
    }
}
