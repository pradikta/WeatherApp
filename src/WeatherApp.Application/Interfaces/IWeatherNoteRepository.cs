using WeatherApp.Domain.Entities;

namespace WeatherApp.Application.Interfaces;

public interface IWeatherNoteRepository
{
    Task AddAsync(WeatherNote weatherNote, CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
