using System.Reflection;
using FluentAssertions;
using WeatherApp.Application.Behaviors;
using WeatherApp.Domain.ValueObjects;
using WeatherApp.Infrastructure.Data;

namespace WeatherApp.Tests;

/// <summary>
/// Guards the Onion Architecture dependency rule: references only point inward.
/// </summary>
public class ArchitectureTests
{
    private static readonly Assembly Domain = typeof(Temperature).Assembly;
    private static readonly Assembly Application = typeof(LoggingBehavior<,>).Assembly;
    private static readonly Assembly Infrastructure = typeof(ApplicationDbContext).Assembly;

    [Fact]
    public void Domain_HasNoDependencyOnOtherLayersOrExternalPackages()
    {
        var references = ReferencedNames(Domain);

        references.Should().NotContain(n => n.StartsWith("WeatherApp.", StringComparison.Ordinal));
        references.Should().NotContain(n =>
            n.StartsWith("MediatR", StringComparison.Ordinal) ||
            n.StartsWith("FluentValidation", StringComparison.Ordinal) ||
            n.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) ||
            n.StartsWith("Microsoft.Extensions", StringComparison.Ordinal) ||
            n.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
    }

    [Fact]
    public void Application_DependsOnDomainOnly()
    {
        var references = ReferencedNames(Application);

        references.Should().Contain("WeatherApp.Domain");
        references.Should().NotContain("WeatherApp.Infrastructure");
        references.Should().NotContain("WeatherApp.Api");
        references.Should().NotContain(n => n.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
    }

    [Fact]
    public void Infrastructure_DoesNotDependOnApi()
    {
        ReferencedNames(Infrastructure).Should().NotContain("WeatherApp.Api");
    }

    private static List<string> ReferencedNames(Assembly assembly) =>
        [.. assembly.GetReferencedAssemblies().Select(a => a.Name!)];
}
