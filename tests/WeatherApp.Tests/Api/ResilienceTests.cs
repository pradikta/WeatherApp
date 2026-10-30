using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using WeatherApp.Application.Interfaces;
using WeatherApp.Infrastructure.Services;
using WeatherApp.Tests.Support;

namespace WeatherApp.Tests.Api;

/// <summary>
/// Exercises the resilience pipeline end to end against a failing in-memory upstream.
/// Timings are shrunk through the "Resilience" settings so the tests stay fast and offline.
/// </summary>
public class ResilienceTests
{
    private static WebApplicationFactory<Program> CreateFactory(
        HttpMessageHandler upstream,
        params (string Key, string Value)[] settings) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("OpenWeatherMap:ApiKey", "test-key");
            builder.UseSetting("Resilience:Retry:Delay", "00:00:00.001");
            foreach (var (key, value) in settings)
            {
                builder.UseSetting(key, value);
            }

            builder.ConfigureServices(services =>
            {
                services.AddHttpClient<IWeatherService, OpenWeatherMapService>()
                    .ConfigurePrimaryHttpMessageHandler(() => upstream);
            });
        });

    private static StubHttpMessageHandler AlwaysUnavailable() =>
        new(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

    [Fact]
    public async Task UpstreamFailure_IsRetriedThenReturnsBadGateway()
    {
        var upstream = AlwaysUnavailable();
        await using var factory = CreateFactory(
            upstream,
            ("Resilience:Retry:MaxRetryAttempts", "2"),
            ("Resilience:CircuitBreaker:MinimumThroughput", "100"));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/weather/Jakarta");

        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        upstream.Requests.Should().HaveCount(3, "one initial attempt plus two retries");
    }

    [Fact]
    public async Task RepeatedFailures_OpenCircuitAndFailFastWithoutCallingUpstream()
    {
        var upstream = AlwaysUnavailable();
        await using var factory = CreateFactory(
            upstream,
            ("Resilience:Retry:MaxRetryAttempts", "1"),
            ("Resilience:CircuitBreaker:MinimumThroughput", "2"),
            ("Resilience:CircuitBreaker:FailureRatio", "0.5"),
            ("Resilience:CircuitBreaker:BreakDuration", "00:00:30"));
        using var client = factory.CreateClient();

        // Failures open the breaker; wait until a request is rejected without reaching the upstream.
        HttpResponseMessage? rejected = null;
        for (var i = 0; i < 5 && rejected is null; i++)
        {
            var response = await client.GetAsync("/api/weather/Jakarta");
            if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
            {
                rejected = response;
            }
        }

        rejected.Should().NotBeNull("the circuit should open after repeated failures");
        rejected!.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        var callsBefore = upstream.Requests.Count;
        var failFast = await client.GetAsync("/api/weather/Jakarta");

        failFast.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        upstream.Requests.Count.Should().Be(callsBefore, "an open circuit must not call the upstream");
    }

    [Fact]
    public async Task SlowUpstream_TimesOutWithGatewayTimeout()
    {
        await using var factory = CreateFactory(
            new HangingHandler(),
            ("Resilience:AttemptTimeout:Timeout", "00:00:00.200"),
            ("Resilience:Retry:MaxRetryAttempts", "1"),
            ("Resilience:CircuitBreaker:MinimumThroughput", "100"));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/weather/Jakarta");

        response.StatusCode.Should().Be(HttpStatusCode.GatewayTimeout);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    private sealed class HangingHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
