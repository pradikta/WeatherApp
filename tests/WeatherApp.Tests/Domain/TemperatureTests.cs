using FluentAssertions;
using WeatherApp.Domain.ValueObjects;

namespace WeatherApp.Tests.Domain;

public class TemperatureTests
{
    [Theory]
    [InlineData(32, 0)]
    [InlineData(212, 100)]
    [InlineData(-40, -40)]
    [InlineData(0, -17.78)]
    [InlineData(-459.67, -273.15)]
    [InlineData(98.6, 37)]
    public void Celsius_ConvertsFahrenheitCorrectly(double fahrenheit, double expectedCelsius)
    {
        var temperature = new Temperature(fahrenheit);

        temperature.Celsius.Should().Be(expectedCelsius);
    }

    [Theory]
    [InlineData(70, 21.11)]
    [InlineData(50, 10)]
    [InlineData(33, 0.56)]
    [InlineData(100.5, 38.06)]
    public void Celsius_RoundsToTwoDecimalPlaces(double fahrenheit, double expectedCelsius)
    {
        new Temperature(fahrenheit).Celsius.Should().Be(expectedCelsius);
    }

    [Fact]
    public void Fahrenheit_ReturnsOriginalValue()
    {
        new Temperature(75.5).Fahrenheit.Should().Be(75.5);
    }

    [Fact]
    public void Temperatures_WithSameFahrenheit_AreEqual()
    {
        new Temperature(32).Should().Be(new Temperature(32));
    }
}
