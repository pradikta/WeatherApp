using MediatR;
using WeatherApp.Application.Dtos;

namespace WeatherApp.Application.Queries;

public sealed record GetCurrentWeatherQuery(string CityName) : IRequest<WeatherResponseDto>;
