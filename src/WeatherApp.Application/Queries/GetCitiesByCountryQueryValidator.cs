using FluentValidation;

namespace WeatherApp.Application.Queries;

public sealed class GetCitiesByCountryQueryValidator : AbstractValidator<GetCitiesByCountryQuery>
{
    public GetCitiesByCountryQueryValidator()
    {
        RuleFor(x => x.CountryCode)
            .NotEmpty().WithMessage("Country code is required.");
    }
}
