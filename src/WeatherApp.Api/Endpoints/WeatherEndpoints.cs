using MediatR;
using WeatherApp.Application.Commands;
using WeatherApp.Application.Queries;

namespace WeatherApp.Api.Endpoints;

public static class WeatherEndpoints
{
    public static void MapWeatherEndpoints(this WebApplication app)
    {
        app.MapGet("/api/countries", async (ISender sender, CancellationToken cancellationToken) =>
        {
            var countries = await sender.Send(new GetCountriesQuery(), cancellationToken);
            return Results.Ok(countries);
        });

        app.MapGet("/api/countries/{countryCode}/cities", async (string countryCode, ISender sender, CancellationToken cancellationToken) =>
        {
            var cities = await sender.Send(new GetCitiesByCountryQuery(countryCode), cancellationToken);
            return Results.Ok(cities);
        });

        app.MapGet("/api/weather/{cityName}", async (string cityName, ISender sender, CancellationToken cancellationToken) =>
        {
            var weather = await sender.Send(new GetCurrentWeatherQuery(cityName), cancellationToken);
            return Results.Ok(weather);
        });

        app.MapPost("/api/weather/notes", async (CreateWeatherNoteCommand command, ISender sender, CancellationToken cancellationToken) =>
        {
            var note = await sender.Send(command, cancellationToken);
            return Results.Created($"/api/weather/notes/{note.Id}", note);
        });
    }
}
