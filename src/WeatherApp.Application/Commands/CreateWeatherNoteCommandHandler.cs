using MediatR;
using WeatherApp.Application.Dtos;
using WeatherApp.Application.Interfaces;
using WeatherApp.Domain.Entities;

namespace WeatherApp.Application.Commands;

public sealed class CreateWeatherNoteCommandHandler(IWeatherNoteRepository weatherNoteRepository)
    : IRequestHandler<CreateWeatherNoteCommand, CreateWeatherNoteDto>
{
    public async Task<CreateWeatherNoteDto> Handle(CreateWeatherNoteCommand request, CancellationToken cancellationToken)
    {
        var weatherNote = new WeatherNote
        {
            Id = Guid.NewGuid(),
            CityName = request.CityName,
            Note = request.Note,
            CreatedAtUtc = DateTime.UtcNow
        };

        await weatherNoteRepository.AddAsync(weatherNote, cancellationToken);
        await weatherNoteRepository.SaveChangesAsync(cancellationToken);

        return new CreateWeatherNoteDto
        {
            Id = weatherNote.Id,
            CityName = weatherNote.CityName,
            Note = weatherNote.Note,
            CreatedAtUtc = weatherNote.CreatedAtUtc
        };
    }
}
