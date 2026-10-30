using Microsoft.EntityFrameworkCore;
using WeatherApp.Application.Interfaces;
using WeatherApp.Domain.Entities;
using WeatherApp.Infrastructure.Data;

namespace WeatherApp.Infrastructure.Repositories;

public sealed class CountryRepository(ApplicationDbContext dbContext) : ICountryRepository
{
    public async Task<IReadOnlyList<Country>> GetCountriesAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Countries
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<City>> GetCitiesByCountryAsync(string countryCode, CancellationToken cancellationToken)
    {
        return await dbContext.Cities
            .AsNoTracking()
            .Where(c => c.CountryCode == countryCode)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<City?> FindCityByNameAsync(string cityName, CancellationToken cancellationToken)
    {
        var normalized = cityName.Trim().ToLower();
        return await dbContext.Cities
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Name.ToLower() == normalized, cancellationToken);
    }
}
