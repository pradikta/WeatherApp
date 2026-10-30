using MediatR;
using WeatherApp.Application.Commands;
using WeatherApp.Application.Dtos;
using WeatherApp.Application.Queries;

namespace WeatherApp.Api.Endpoints;

public static class WeatherEndpoints
{
    public static IEndpointRouteBuilder MapWeatherEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/countries", async (ISender sender, CancellationToken cancellationToken) =>
        {
            var countries = await sender.Send(new GetCountriesQuery(), cancellationToken);
            return Results.Ok(countries);
        })
            .WithName("GetCountries")
            .WithTags("Countries")
            .WithSummary("Returns all available countries.")
            .WithDescription("Retrieves the complete list of supported countries with their ISO codes, ordered by name.")
            .Produces<IReadOnlyList<CountryDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        endpoints.MapGet("/api/countries/{countryCode}/cities", async (string countryCode, ISender sender, CancellationToken cancellationToken) =>
        {
            var cities = await sender.Send(new GetCitiesByCountryQuery(countryCode), cancellationToken);
            return Results.Ok(cities);
        })
            .WithName("GetCitiesByCountry")
            .WithTags("Countries")
            .WithSummary("Returns all cities for a specific country code.")
            .WithDescription("Retrieves the list of cities belonging to the given ISO 3166-1 alpha-2 country code, ordered by name.")
            .Produces<IReadOnlyList<CityDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        endpoints.MapGet("/api/weather/{cityName}", async (string cityName, ISender sender, CancellationToken cancellationToken) =>
        {
            var weather = await sender.Send(new GetCurrentWeatherQuery(cityName), cancellationToken);
            return Results.Ok(weather);
        })
            .WithName("GetCurrentWeather")
            .WithTags("Weather")
            .WithSummary("Retrieves the current weather for a city.")
            .WithDescription("Fetches the latest weather conditions for the given city from OpenWeatherMap, including temperature in Fahrenheit and Celsius, wind, visibility, pressure, sky conditions, dew point, and humidity.")
            .Produces<WeatherResponseDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        endpoints.MapPost("/api/weather/notes", async (CreateWeatherNoteCommand command, ISender sender, CancellationToken cancellationToken) =>
        {
            var note = await sender.Send(command, cancellationToken);
            return Results.Created($"/api/weather/notes/{note.Id}", note);
        })
            .WithName("CreateWeatherNote")
            .WithTags("Weather Notes")
            .WithSummary("Adds a custom note for a city's weather.")
            .WithDescription("Persists a user-defined note for a specific city and returns the created resource identifier.")
            .Produces<CreateWeatherNoteDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return endpoints;
    }
}

