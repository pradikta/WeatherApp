namespace WeatherApp.Application.Dtos;

public sealed record CreateWeatherNoteDto
{
    public Guid Id { get; init; }
    public string CityName { get; init; } = string.Empty;
    public string Note { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
}
