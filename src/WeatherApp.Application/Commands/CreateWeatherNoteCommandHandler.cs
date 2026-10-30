using MediatR;
using WeatherApp.Application.Dtos;
using WeatherApp.Application.Interfaces;
using WeatherApp.Domain.Entities;

namespace WeatherApp.Application.Commands;

public sealed class CreateWeatherNoteCommandHandler(IWeatherNoteRepository weatherNoteRepository, TimeProvider timeProvider)
    : IRequestHandler<CreateWeatherNoteCommand, CreateWeatherNoteDto>
{
    public async Task<CreateWeatherNoteDto> Handle(CreateWeatherNoteCommand request, CancellationToken cancellationToken)
    {
        var weatherNote = new WeatherNote
        {
            Id = Guid.NewGuid(),
            CityName = request.CityName,
            Note = request.Note,
            CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime
        };

        await weatherNoteRepository.AddAsync(weatherNote, cancellationToken);
        await weatherNoteRepository.SaveChangesAsync(cancellationToken);
        WeatherTelemetry.NotesCreated.Add(1);

        return new CreateWeatherNoteDto
        {
            Id = weatherNote.Id,
            CityName = weatherNote.CityName,
            Note = weatherNote.Note,
            CreatedAtUtc = weatherNote.CreatedAtUtc
        };
    }
}
