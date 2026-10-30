using Microsoft.EntityFrameworkCore;
using WeatherApp.Domain.Entities;

namespace WeatherApp.Infrastructure.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<WeatherNote> WeatherNotes => Set<WeatherNote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Country>(entity =>
        {
            entity.HasKey(c => c.Code);
            entity.Property(c => c.Code).HasMaxLength(2);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<City>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.CountryCode).IsRequired().HasMaxLength(2);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(100);

            entity.HasOne<Country>()
                .WithMany()
                .HasForeignKey(c => c.CountryCode);
        });

        modelBuilder.Entity<WeatherNote>(entity =>
        {
            entity.HasKey(n => n.Id);
            entity.Property(n => n.CityName).IsRequired().HasMaxLength(100);
            entity.Property(n => n.Note).IsRequired().HasMaxLength(1000);
            entity.Property(n => n.CreatedAtUtc).IsRequired();
        });

        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        // Representative seed data: five countries per continent with their major cities. The
        // complete worldwide country/city dataset is intentionally omitted; for full production,
        // extend the dataset via a config/JSON-driven seeding strategy.
        modelBuilder.Entity<Country>().HasData(
            new Country { Code = "ID", Name = "Indonesia" },
            new Country { Code = "JP", Name = "Japan" },
            new Country { Code = "CN", Name = "China" },
            new Country { Code = "IN", Name = "India" },
            new Country { Code = "KR", Name = "South Korea" },
            new Country { Code = "GB", Name = "United Kingdom" },
            new Country { Code = "FR", Name = "France" },
            new Country { Code = "DE", Name = "Germany" },
            new Country { Code = "ES", Name = "Spain" },
            new Country { Code = "IT", Name = "Italy" },
            new Country { Code = "US", Name = "United States" },
            new Country { Code = "CA", Name = "Canada" },
            new Country { Code = "MX", Name = "Mexico" },
            new Country { Code = "CU", Name = "Cuba" },
            new Country { Code = "DO", Name = "Dominican Republic" },
            new Country { Code = "BR", Name = "Brazil" },
            new Country { Code = "AR", Name = "Argentina" },
            new Country { Code = "CO", Name = "Colombia" },
            new Country { Code = "CL", Name = "Chile" },
            new Country { Code = "PE", Name = "Peru" },
            new Country { Code = "EG", Name = "Egypt" },
            new Country { Code = "ZA", Name = "South Africa" },
            new Country { Code = "NG", Name = "Nigeria" },
            new Country { Code = "KE", Name = "Kenya" },
            new Country { Code = "MA", Name = "Morocco" },
            new Country { Code = "AU", Name = "Australia" },
            new Country { Code = "NZ", Name = "New Zealand" },
            new Country { Code = "FJ", Name = "Fiji" },
            new Country { Code = "PG", Name = "Papua New Guinea" },
            new Country { Code = "WS", Name = "Samoa" });

        modelBuilder.Entity<City>().HasData(
            new City { Id = 1, CountryCode = "ID", Name = "Jakarta" },
            new City { Id = 2, CountryCode = "ID", Name = "Bandung" },
            new City { Id = 3, CountryCode = "ID", Name = "Surabaya" },
            new City { Id = 4, CountryCode = "JP", Name = "Tokyo" },
            new City { Id = 5, CountryCode = "JP", Name = "Osaka" },
            new City { Id = 6, CountryCode = "JP", Name = "Kyoto" },
            new City { Id = 7, CountryCode = "CN", Name = "Beijing" },
            new City { Id = 8, CountryCode = "CN", Name = "Shanghai" },
            new City { Id = 9, CountryCode = "CN", Name = "Guangzhou" },
            new City { Id = 10, CountryCode = "IN", Name = "Mumbai" },
            new City { Id = 11, CountryCode = "IN", Name = "Delhi" },
            new City { Id = 12, CountryCode = "IN", Name = "Bengaluru" },
            new City { Id = 13, CountryCode = "KR", Name = "Seoul" },
            new City { Id = 14, CountryCode = "KR", Name = "Busan" },
            new City { Id = 15, CountryCode = "KR", Name = "Incheon" },
            new City { Id = 16, CountryCode = "GB", Name = "London" },
            new City { Id = 17, CountryCode = "GB", Name = "Manchester" },
            new City { Id = 18, CountryCode = "GB", Name = "Edinburgh" },
            new City { Id = 19, CountryCode = "FR", Name = "Paris" },
            new City { Id = 20, CountryCode = "FR", Name = "Marseille" },
            new City { Id = 21, CountryCode = "FR", Name = "Lyon" },
            new City { Id = 22, CountryCode = "DE", Name = "Berlin" },
            new City { Id = 23, CountryCode = "DE", Name = "Munich" },
            new City { Id = 24, CountryCode = "DE", Name = "Hamburg" },
            new City { Id = 25, CountryCode = "ES", Name = "Madrid" },
            new City { Id = 26, CountryCode = "ES", Name = "Barcelona" },
            new City { Id = 27, CountryCode = "ES", Name = "Valencia" },
            new City { Id = 28, CountryCode = "IT", Name = "Rome" },
            new City { Id = 29, CountryCode = "IT", Name = "Milan" },
            new City { Id = 30, CountryCode = "IT", Name = "Naples" },
            new City { Id = 31, CountryCode = "US", Name = "New York" },
            new City { Id = 32, CountryCode = "US", Name = "Los Angeles" },
            new City { Id = 33, CountryCode = "US", Name = "Chicago" },
            new City { Id = 34, CountryCode = "CA", Name = "Toronto" },
            new City { Id = 35, CountryCode = "CA", Name = "Vancouver" },
            new City { Id = 36, CountryCode = "CA", Name = "Montreal" },
            new City { Id = 37, CountryCode = "MX", Name = "Mexico City" },
            new City { Id = 38, CountryCode = "MX", Name = "Guadalajara" },
            new City { Id = 39, CountryCode = "MX", Name = "Monterrey" },
            new City { Id = 40, CountryCode = "CU", Name = "Havana" },
            new City { Id = 41, CountryCode = "CU", Name = "Santiago de Cuba" },
            new City { Id = 42, CountryCode = "CU", Name = "Camagüey" },
            new City { Id = 43, CountryCode = "DO", Name = "Santo Domingo" },
            new City { Id = 44, CountryCode = "DO", Name = "Santiago de los Caballeros" },
            new City { Id = 45, CountryCode = "DO", Name = "La Romana" },
            new City { Id = 46, CountryCode = "BR", Name = "São Paulo" },
            new City { Id = 47, CountryCode = "BR", Name = "Rio de Janeiro" },
            new City { Id = 48, CountryCode = "BR", Name = "Brasília" },
            new City { Id = 49, CountryCode = "AR", Name = "Buenos Aires" },
            new City { Id = 50, CountryCode = "AR", Name = "Córdoba" },
            new City { Id = 51, CountryCode = "AR", Name = "Rosario" },
            new City { Id = 52, CountryCode = "CO", Name = "Bogotá" },
            new City { Id = 53, CountryCode = "CO", Name = "Medellín" },
            new City { Id = 54, CountryCode = "CO", Name = "Cali" },
            new City { Id = 55, CountryCode = "CL", Name = "Santiago" },
            new City { Id = 56, CountryCode = "CL", Name = "Valparaíso" },
            new City { Id = 57, CountryCode = "CL", Name = "Concepción" },
            new City { Id = 58, CountryCode = "PE", Name = "Lima" },
            new City { Id = 59, CountryCode = "PE", Name = "Arequipa" },
            new City { Id = 60, CountryCode = "PE", Name = "Cusco" },
            new City { Id = 61, CountryCode = "EG", Name = "Cairo" },
            new City { Id = 62, CountryCode = "EG", Name = "Alexandria" },
            new City { Id = 63, CountryCode = "EG", Name = "Giza" },
            new City { Id = 64, CountryCode = "ZA", Name = "Johannesburg" },
            new City { Id = 65, CountryCode = "ZA", Name = "Cape Town" },
            new City { Id = 66, CountryCode = "ZA", Name = "Durban" },
            new City { Id = 67, CountryCode = "NG", Name = "Lagos" },
            new City { Id = 68, CountryCode = "NG", Name = "Abuja" },
            new City { Id = 69, CountryCode = "NG", Name = "Kano" },
            new City { Id = 70, CountryCode = "KE", Name = "Nairobi" },
            new City { Id = 71, CountryCode = "KE", Name = "Mombasa" },
            new City { Id = 72, CountryCode = "KE", Name = "Kisumu" },
            new City { Id = 73, CountryCode = "MA", Name = "Casablanca" },
            new City { Id = 74, CountryCode = "MA", Name = "Rabat" },
            new City { Id = 75, CountryCode = "MA", Name = "Marrakesh" },
            new City { Id = 76, CountryCode = "AU", Name = "Sydney" },
            new City { Id = 77, CountryCode = "AU", Name = "Melbourne" },
            new City { Id = 78, CountryCode = "AU", Name = "Brisbane" },
            new City { Id = 79, CountryCode = "NZ", Name = "Auckland" },
            new City { Id = 80, CountryCode = "NZ", Name = "Wellington" },
            new City { Id = 81, CountryCode = "NZ", Name = "Christchurch" },
            new City { Id = 82, CountryCode = "FJ", Name = "Suva" },
            new City { Id = 83, CountryCode = "FJ", Name = "Nadi" },
            new City { Id = 84, CountryCode = "FJ", Name = "Lautoka" },
            new City { Id = 85, CountryCode = "PG", Name = "Port Moresby" },
            new City { Id = 86, CountryCode = "PG", Name = "Lae" },
            new City { Id = 87, CountryCode = "PG", Name = "Mount Hagen" },
            new City { Id = 88, CountryCode = "WS", Name = "Apia" },
            new City { Id = 89, CountryCode = "WS", Name = "Salelologa" },
            new City { Id = 90, CountryCode = "WS", Name = "Asau" });
    }
}
