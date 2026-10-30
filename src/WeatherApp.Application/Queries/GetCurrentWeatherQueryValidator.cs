using FluentValidation;

namespace WeatherApp.Application.Queries;

public sealed class GetCurrentWeatherQueryValidator : AbstractValidator<GetCurrentWeatherQuery>
{
    public GetCurrentWeatherQueryValidator()
    {
        RuleFor(x => x.CityName)
            .NotEmpty().WithMessage("City name is required.")
            .MaximumLength(100);
    }
}
