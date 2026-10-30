using FluentAssertions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using WeatherApp.Application.Behaviors;
using WeatherApp.Application.Commands;
using WeatherApp.Application.Dtos;

namespace WeatherApp.Tests.Application;

public class PipelineBehaviorTests
{
    private static readonly CreateWeatherNoteCommand ValidCommand = new("Jakarta", "ok");
    private static readonly CreateWeatherNoteCommand InvalidCommand = new("", "");
    private static readonly CreateWeatherNoteDto Response = new() { CityName = "Jakarta" };

    [Fact]
    public async Task ValidationBehavior_ValidRequest_CallsNext()
    {
        var behavior = new ValidationBehavior<CreateWeatherNoteCommand, CreateWeatherNoteDto>(
            [new CreateWeatherNoteCommandValidator()]);
        var nextCalled = false;

        var result = await behavior.Handle(ValidCommand, _ => { nextCalled = true; return Task.FromResult(Response); }, CancellationToken.None);

        nextCalled.Should().BeTrue();
        result.Should().BeSameAs(Response);
    }

    [Fact]
    public async Task ValidationBehavior_InvalidRequest_ThrowsAndSkipsNext()
    {
        var behavior = new ValidationBehavior<CreateWeatherNoteCommand, CreateWeatherNoteDto>(
            [new CreateWeatherNoteCommandValidator()]);
        var nextCalled = false;

        var act = () => behavior.Handle(InvalidCommand, _ => { nextCalled = true; return Task.FromResult(Response); }, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors.Select(e => e.PropertyName).Should().BeEquivalentTo(["CityName", "Note"]);
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task ValidationBehavior_NoValidators_CallsNext()
    {
        var behavior = new ValidationBehavior<CreateWeatherNoteCommand, CreateWeatherNoteDto>([]);

        var result = await behavior.Handle(InvalidCommand, _ => Task.FromResult(Response), CancellationToken.None);

        result.Should().BeSameAs(Response);
    }

    [Fact]
    public async Task LoggingBehavior_Success_ReturnsResponseAndLogsRequestAndCompletion()
    {
        var logger = new Mock<ILogger<LoggingBehavior<CreateWeatherNoteCommand, CreateWeatherNoteDto>>>();
        var behavior = new LoggingBehavior<CreateWeatherNoteCommand, CreateWeatherNoteDto>(logger.Object);

        var result = await behavior.Handle(ValidCommand, _ => Task.FromResult(Response), CancellationToken.None);

        result.Should().BeSameAs(Response);
        VerifyLogged(logger, LogLevel.Information, Times.Exactly(2));
        VerifyLogged(logger, LogLevel.Error, Times.Never());
    }

    [Fact]
    public async Task LoggingBehavior_Failure_LogsErrorAndRethrows()
    {
        var logger = new Mock<ILogger<LoggingBehavior<CreateWeatherNoteCommand, CreateWeatherNoteDto>>>();
        var behavior = new LoggingBehavior<CreateWeatherNoteCommand, CreateWeatherNoteDto>(logger.Object);

        var act = () => behavior.Handle(ValidCommand, _ => throw new InvalidOperationException("boom"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
        VerifyLogged(logger, LogLevel.Error, Times.Once());
    }

    private static void VerifyLogged<T>(Mock<ILogger<T>> logger, LogLevel level, Times times) =>
        logger.Verify(l => l.Log(
            level,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), times);
}
