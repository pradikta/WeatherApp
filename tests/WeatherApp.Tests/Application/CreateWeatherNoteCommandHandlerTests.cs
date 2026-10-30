using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WeatherApp.Application.Commands;
using WeatherApp.Infrastructure.Repositories;
using WeatherApp.Tests.Support;

namespace WeatherApp.Tests.Application;

public class CreateWeatherNoteCommandHandlerTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Handle_PersistsNoteAndReturnsCreatedIdentifier()
    {
        await using var writeContext = _database.CreateContext();
        var handler = new CreateWeatherNoteCommandHandler(new WeatherNoteRepository(writeContext));

        var result = await handler.Handle(
            new CreateWeatherNoteCommand("Jakarta", "Humid afternoon"),
            CancellationToken.None);

        result.Id.Should().NotBeEmpty();
        result.CityName.Should().Be("Jakarta");
        result.Note.Should().Be("Humid afternoon");
        result.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Read through a fresh context to prove the row is really stored, not just tracked.
        await using var readContext = _database.CreateContext();
        var stored = await readContext.WeatherNotes.AsNoTracking().SingleAsync();
        stored.Id.Should().Be(result.Id);
        stored.CityName.Should().Be("Jakarta");
        stored.Note.Should().Be("Humid afternoon");
        stored.CreatedAtUtc.Should().Be(result.CreatedAtUtc);
    }

    [Fact]
    public async Task Handle_CalledTwice_PersistsTwoNotesWithDistinctIds()
    {
        await using var context = _database.CreateContext();
        var handler = new CreateWeatherNoteCommandHandler(new WeatherNoteRepository(context));

        var first = await handler.Handle(new CreateWeatherNoteCommand("Tokyo", "One"), CancellationToken.None);
        var second = await handler.Handle(new CreateWeatherNoteCommand("Tokyo", "Two"), CancellationToken.None);

        first.Id.Should().NotBe(second.Id);

        await using var readContext = _database.CreateContext();
        (await readContext.WeatherNotes.AsNoTracking().CountAsync()).Should().Be(2);
    }

    [Theory]
    [InlineData("", "note", "CityName")]
    [InlineData("Jakarta", "", "Note")]
    public void Validator_RejectsEmptyFields(string city, string note, string expectedProperty)
    {
        var result = new CreateWeatherNoteCommandValidator().Validate(new CreateWeatherNoteCommand(city, note));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == expectedProperty);
    }

    [Theory]
    [InlineData(100, true)]
    [InlineData(101, false)]
    public void Validator_EnforcesCityNameMaxLength(int length, bool expectedValid)
    {
        var command = new CreateWeatherNoteCommand(new string('a', length), "ok");

        new CreateWeatherNoteCommandValidator().Validate(command).IsValid.Should().Be(expectedValid);
    }

    [Theory]
    [InlineData(1000, true)]
    [InlineData(1001, false)]
    public void Validator_EnforcesNoteMaxLength(int length, bool expectedValid)
    {
        var command = new CreateWeatherNoteCommand("Jakarta", new string('x', length));

        new CreateWeatherNoteCommandValidator().Validate(command).IsValid.Should().Be(expectedValid);
    }

    [Fact]
    public void Validator_AcceptsValidCommand()
    {
        new CreateWeatherNoteCommandValidator()
            .Validate(new CreateWeatherNoteCommand("Jakarta", "ok"))
            .IsValid.Should().BeTrue();
    }
}
