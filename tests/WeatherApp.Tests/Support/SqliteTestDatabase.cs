using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WeatherApp.Infrastructure.Data;

namespace WeatherApp.Tests.Support;

/// <summary>
/// Owns an open in-memory SQLite connection so the database lives as long as the instance.
/// </summary>
public sealed class SqliteTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteTestDatabase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public ApplicationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options);

    public void Dispose() => _connection.Dispose();
}
