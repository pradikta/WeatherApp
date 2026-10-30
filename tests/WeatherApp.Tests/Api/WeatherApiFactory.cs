using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using WeatherApp.Application.Interfaces;

namespace WeatherApp.Tests.Api;

/// <summary>
/// Hosts the real API in memory with the external weather service replaced by a mock,
/// so endpoint tests never reach the network.
/// </summary>
public sealed class WeatherApiFactory : WebApplicationFactory<Program>
{
    public Mock<IWeatherService> WeatherService { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IWeatherService>();
            services.AddSingleton(WeatherService.Object);
        });
    }
}
