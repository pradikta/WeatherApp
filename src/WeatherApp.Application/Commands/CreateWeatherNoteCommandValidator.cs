using FluentValidation;

namespace WeatherApp.Application.Commands;

public sealed class CreateWeatherNoteCommandValidator : AbstractValidator<CreateWeatherNoteCommand>
{
    public CreateWeatherNoteCommandValidator()
    {
        RuleFor(x => x.CityName)
            .NotEmpty().WithMessage("City name is required.")
            .MaximumLength(100);

        RuleFor(x => x.Note)
            .NotEmpty().WithMessage("Note is required.")
            .MaximumLength(1000);
    }
}
