using MediatR;
using WeatherApp.Application.Dtos;
using WeatherApp.Application.Interfaces;

namespace WeatherApp.Application.Queries;

public sealed class GetCitiesByCountryQueryHandler(ICountryRepository countryRepository)
    : IRequestHandler<GetCitiesByCountryQuery, IReadOnlyList<CityDto>>
{
    public async Task<IReadOnlyList<CityDto>> Handle(GetCitiesByCountryQuery request, CancellationToken cancellationToken)
    {
        var cities = await countryRepository.GetCitiesByCountryAsync(request.CountryCode, cancellationToken);

        return [.. cities.Select(c => new CityDto(c.Id, c.CountryCode, c.Name))];
    }
}
