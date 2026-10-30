using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WeatherApp.Application.Interfaces;
using WeatherApp.Infrastructure.Services;
using Moq;

namespace WeatherApp.Tests.Api;

public class HardeningTests
{
    private static WebApplicationFactory<Program> CreateFactory(params (string Key, string Value)[] settings) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            foreach (var (key, value) in settings)
            {
                builder.UseSetting(key, value);
            }

            builder.ConfigureServices(services =>
            {
                services.AddSingleton(new Mock<IWeatherService>().Object);
            });
        });

    [Fact]
    public async Task RateLimiter_RejectsRequestsOverLimitWithProblemDetails()
    {
        await using var factory = CreateFactory(("RateLimiting:PermitLimit", "2"), ("RateLimiting:WindowSeconds", "60"));
        using var client = factory.CreateClient();

        (await client.GetAsync("/api/countries")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync("/api/countries")).StatusCode.Should().Be(HttpStatusCode.OK);
        var rejected = await client.GetAsync("/api/countries");

        rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        rejected.Headers.RetryAfter.Should().NotBeNull();
        rejected.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task RateLimiter_DoesNotLimitHealthChecks()
    {
        await using var factory = CreateFactory(("RateLimiting:PermitLimit", "1"));
        using var client = factory.CreateClient();

        for (var i = 0; i < 5; i++)
        {
            (await client.GetAsync("/health")).StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    [Fact]
    public void Startup_InvalidOpenWeatherMapBaseAddress_FailsFast()
    {
        using var factory = CreateFactory(("OpenWeatherMap:BaseAddress", "not-a-url"));

        var act = () => factory.CreateClient();

        act.Should().Throw<OptionsValidationException>().WithMessage("*BaseAddress*");
    }

    [Fact]
    public void Startup_InvalidCacheDuration_FailsFast()
    {
        using var factory = CreateFactory(("Caching:WeatherCacheDurationMinutes", "0"));

        var act = () => factory.CreateClient();

        act.Should().Throw<OptionsValidationException>().WithMessage("*WeatherCacheDurationMinutes*");
    }

    [Fact]
    public void Startup_MissingApiKey_StillBoots()
    {
        using var factory = CreateFactory();

        var options = factory.Services.GetRequiredService<IOptions<OpenWeatherMapOptions>>().Value;

        options.ApiKey.Should().BeEmpty();
    }
}
