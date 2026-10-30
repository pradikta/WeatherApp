namespace WeatherApp.Application.Dtos;

public sealed record WeatherResponseDto
{
    public string Location { get; init; } = string.Empty;
    public DateTimeOffset TimeUtc { get; init; }
    public double WindSpeed { get; init; }
    public double WindDirection { get; init; }
    public double Visibility { get; init; }
    public double Pressure { get; init; }
    public string SkyCondition { get; init; } = string.Empty;
    public double TempFahrenheit { get; init; }
    public double TempCelsius { get; init; }
    public double DewPoint { get; init; }
    public int Humidity { get; init; }
}
