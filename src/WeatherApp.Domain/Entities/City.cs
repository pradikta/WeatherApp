namespace WeatherApp.Domain.Entities;

public class City
{
    public int Id { get; set; }
    public string CountryCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
