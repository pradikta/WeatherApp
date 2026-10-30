using MediatR;
using WeatherApp.Application.Dtos;

namespace WeatherApp.Application.Queries;

public sealed record GetCountriesQuery : IRequest<IReadOnlyList<CountryDto>>;
