using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WeatherApp.Application.Interfaces;
using WeatherApp.Infrastructure.Data;
using WeatherApp.Infrastructure.Repositories;
using WeatherApp.Infrastructure.Services;

namespace WeatherApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OpenWeatherMapOptions>(configuration.GetSection(OpenWeatherMapOptions.SectionName));

        // Keep a single open connection so the in-memory SQLite database persists
        // across DbContext instances (each new connection gets its own empty database).
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection));

        services.AddScoped<ICountryRepository, CountryRepository>();
        services.AddScoped<IWeatherNoteRepository, WeatherNoteRepository>();

        services.AddHttpClient<IWeatherService, OpenWeatherMapService>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<OpenWeatherMapOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseAddress);
        });

        return services;
    }
}
