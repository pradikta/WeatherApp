using MediatR;
using WeatherApp.Application.Dtos;
using WeatherApp.Application.Interfaces;

namespace WeatherApp.Application.Queries;

public sealed class GetCountriesQueryHandler(ICountryRepository countryRepository)
    : IRequestHandler<GetCountriesQuery, IReadOnlyList<CountryDto>>
{
    public async Task<IReadOnlyList<CountryDto>> Handle(GetCountriesQuery request, CancellationToken cancellationToken)
    {
        var countries = await countryRepository.GetCountriesAsync(cancellationToken);

        return [.. countries.Select(c => new CountryDto(c.Code, c.Name))];
    }
}
