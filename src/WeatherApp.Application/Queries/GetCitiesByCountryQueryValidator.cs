using FluentValidation;

namespace WeatherApp.Application.Queries;

public sealed class GetCitiesByCountryQueryValidator : AbstractValidator<GetCitiesByCountryQuery>
{
    public GetCitiesByCountryQueryValidator()
    {
        RuleFor(x => x.CountryCode)
            .NotEmpty().WithMessage("Country code is required.")
            .Matches("^[A-Za-z]{2}$").WithMessage("Country code must be a 2-letter ISO 3166-1 alpha-2 code.");
    }
}
