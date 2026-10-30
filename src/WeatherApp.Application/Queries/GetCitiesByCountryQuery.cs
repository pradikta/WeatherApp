using MediatR;
using WeatherApp.Application.Dtos;

namespace WeatherApp.Application.Queries;

public sealed record GetCitiesByCountryQuery(string CountryCode) : IRequest<IReadOnlyList<CityDto>>;
