using Microsoft.EntityFrameworkCore;
using WeatherApp.Application.Interfaces;
using WeatherApp.Domain.Entities;
using WeatherApp.Infrastructure.Data;

namespace WeatherApp.Infrastructure.Repositories;

public sealed class WeatherNoteRepository(ApplicationDbContext dbContext) : IWeatherNoteRepository
{
    public async Task AddAsync(WeatherNote weatherNote, CancellationToken cancellationToken)
    {
        await dbContext.WeatherNotes.AddAsync(weatherNote, cancellationToken);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return await dbContext.SaveChangesAsync(cancellationToken);
    }
}
