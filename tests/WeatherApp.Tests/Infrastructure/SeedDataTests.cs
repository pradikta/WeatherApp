using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WeatherApp.Tests.Support;

namespace WeatherApp.Tests.Infrastructure;

public class SeedDataTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Countries_HaveUniqueTwoLetterUppercaseCodes()
    {
        await using var context = _database.CreateContext();

        var countries = await context.Countries.AsNoTracking().ToListAsync();

        countries.Should().NotBeEmpty();
        countries.Select(c => c.Code).Should().OnlyHaveUniqueItems();
        countries.Should().OnlyContain(c => c.Code.Length == 2 && c.Code == c.Code.ToUpperInvariant());
    }

    [Fact]
    public async Task Cities_ReferenceExistingCountriesAndAreUniquePerCountry()
    {
        await using var context = _database.CreateContext();

        var countryCodes = (await context.Countries.AsNoTracking().Select(c => c.Code).ToListAsync()).ToHashSet();
        var cities = await context.Cities.AsNoTracking().ToListAsync();

        cities.Select(c => c.Id).Should().OnlyHaveUniqueItems();
        cities.Should().OnlyContain(c => countryCodes.Contains(c.CountryCode));
        cities.Select(c => (c.CountryCode, c.Name)).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task EveryCountry_HasAtLeastOneCity()
    {
        await using var context = _database.CreateContext();

        var countryCodes = await context.Countries.AsNoTracking().Select(c => c.Code).ToListAsync();
        var citiesPerCountry = await context.Cities.AsNoTracking().GroupBy(c => c.CountryCode)
            .Select(g => g.Key).ToListAsync();

        citiesPerCountry.Should().BeEquivalentTo(countryCodes);
    }
}
