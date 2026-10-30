using WeatherApp.Domain.Entities;

namespace WeatherApp.Application.Interfaces;

public interface ICountryRepository
{
    Task<IReadOnlyList<Country>> GetCountriesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<City>> GetCitiesByCountryAsync(string countryCode, CancellationToken cancellationToken);
}
