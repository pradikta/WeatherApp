using System.Diagnostics;
using System.Diagnostics.Metrics;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using WeatherApp.Application;
using WeatherApp.Application.Dtos;
using WeatherApp.Application.Interfaces;
using WeatherApp.Application.Options;
using WeatherApp.Application.Queries;

namespace WeatherApp.Tests.Application;

public sealed class TelemetryTests
{
    [Fact]
    public async Task GetCurrentWeather_RecordsCacheMissThenHitAndStartsActivity()
    {
        var recorded = new List<(string Instrument, string City)>();
        var activities = new List<string>();
        var city = $"Telemetry-{Guid.NewGuid():N}";

        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == WeatherTelemetry.Name)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<long>((instrument, _, _, _) =>
        {
            lock (recorded)
            {
                recorded.Add((instrument.Name, city));
            }
        });
        meterListener.Start();

        using var activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == WeatherTelemetry.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity => { lock (activities) { activities.Add(activity.OperationName); } }
        };
        ActivitySource.AddActivityListener(activityListener);

        var service = new Mock<IWeatherService>();
        service.Setup(s => s.GetCurrentWeatherAsync(city, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WeatherResponseDto { Location = "X, Y" });
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var handler = new GetCurrentWeatherQueryHandler(
            service.Object, cache, Options.Create(new CachingOptions { WeatherCacheDurationMinutes = 1 }));

        await handler.Handle(new GetCurrentWeatherQuery(city), CancellationToken.None);
        await handler.Handle(new GetCurrentWeatherQuery(city), CancellationToken.None);

        recorded.Select(r => r.Instrument).Should().Contain(["weather.cache.misses", "weather.cache.hits"]);
        activities.Should().Contain("FetchCurrentWeather");
    }
}
