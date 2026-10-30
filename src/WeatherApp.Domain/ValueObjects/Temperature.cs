namespace WeatherApp.Domain.ValueObjects;

/// <summary>
/// Value object representing a temperature measured in Fahrenheit.
/// </summary>
/// <param name="Fahrenheit">The temperature in degrees Fahrenheit.</param>
public sealed record Temperature(double Fahrenheit)
{
    /// <summary>
    /// Gets the temperature in degrees Celsius, converted from Fahrenheit.
    /// </summary>
    public double Celsius => Math.Round((Fahrenheit - 32) * 5 / 9, 2);
}
