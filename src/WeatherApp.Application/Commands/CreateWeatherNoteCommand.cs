using MediatR;
using WeatherApp.Application.Dtos;

namespace WeatherApp.Application.Commands;

public sealed record CreateWeatherNoteCommand(string CityName, string Note) : IRequest<CreateWeatherNoteDto>;
