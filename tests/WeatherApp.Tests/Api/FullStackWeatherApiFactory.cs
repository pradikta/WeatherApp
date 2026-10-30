using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using WeatherApp.Application.Interfaces;
using WeatherApp.Infrastructure.Services;
using WeatherApp.Tests.Support;

namespace WeatherApp.Tests.Api;

/// <summary>
/// Hosts the real API with the real <see cref="OpenWeatherMapService"/>, but routes its
/// HttpClient to an in-memory handler returning fake OpenWeatherMap JSON.
/// </summary>
public sealed class FullStackWeatherApiFactory : WebApplicationFactory<Program>
{
    private const string GeocodeJson = """[{"name":"Jakarta","lat":-6.2,"lon":106.8,"country":"ID"}]""";

    private const string WeatherJson = """
        {
          "weather":[{"description":"clear sky"}],
          "main":{"temp":68,"pressure":1013,"humidity":50},
          "visibility":10000,
          "wind":{"speed":5.5,"deg":270},
          "dt":1760000000
        }
        """;

    public StubHttpMessageHandler Handler { get; } = new(request =>
    {
        var body = request.RequestUri!.AbsolutePath.StartsWith("/geo/") ? GeocodeJson : WeatherJson;
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    });

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("OpenWeatherMap:ApiKey", "test-key");

        builder.ConfigureServices(services =>
        {
            services.AddHttpClient<IWeatherService, OpenWeatherMapService>()
                .ConfigurePrimaryHttpMessageHandler(() => Handler);
        });
    }
}
