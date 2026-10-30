namespace WeatherApp.Domain.Entities;

public class WeatherNote
{
    public Guid Id { get; set; }
    public string CityName { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
