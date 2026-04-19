using Microsoft.EntityFrameworkCore;
using WeatherApi.Data.Models;

namespace WeatherApi.Data;

public class WeatherDbContext(DbContextOptions<WeatherDbContext> options) : DbContext(options)
{
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<WeatherObservation> WeatherObservations => Set<WeatherObservation>();
    public DbSet<ForecastEntry> ForecastEntries => Set<ForecastEntry>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Location>(e =>
        {
            e.ToTable("locations");
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Type).HasMaxLength(10).IsRequired();
            e.Property(x => x.ExternalId).HasMaxLength(50);
            e.Property(x => x.Latitude).HasPrecision(9, 6);
            e.Property(x => x.Longitude).HasPrecision(9, 6);
            e.Property(x => x.Region).HasMaxLength(10);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("NOW()");
            e.HasIndex(x => x.Name).IsUnique();
            e.HasIndex(x => x.ExternalId);
        });

        mb.Entity<WeatherObservation>(e =>
        {
            e.ToTable("weather_observations");
            e.Property(x => x.TemperatureCelsius).HasPrecision(5, 2);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
            e.HasIndex(x => new { x.LocationId, x.ObservedAt }).IsUnique();
            e.HasIndex(x => x.ObservedAt);
            e.HasOne(x => x.Location)
             .WithMany(l => l.Observations)
             .HasForeignKey(x => x.LocationId);
        });

        mb.Entity<ForecastEntry>(e =>
        {
            e.ToTable("weather_forecasts");
            e.Property(x => x.ForecastType).HasMaxLength(10).IsRequired();
            e.Property(x => x.ForecastText).HasMaxLength(100).IsRequired();
            e.Property(x => x.TempLowCelsius).HasPrecision(5, 2);
            e.Property(x => x.TempHighCelsius).HasPrecision(5, 2);
            e.Property(x => x.HumidityLowPct).HasPrecision(5, 2);
            e.Property(x => x.HumidityHighPct).HasPrecision(5, 2);
            e.Property(x => x.WindDirection).HasMaxLength(10);
            e.Property(x => x.WindSpeedLowKmh).HasPrecision(6, 2);
            e.Property(x => x.WindSpeedHighKmh).HasPrecision(6, 2);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
            e.HasIndex(x => new { x.LocationId, x.IssuedAt, x.ValidFrom, x.ForecastType }).IsUnique();
            e.HasIndex(x => new { x.ValidFrom, x.LocationId });
            e.HasOne(x => x.Location)
             .WithMany(l => l.Forecasts)
             .HasForeignKey(x => x.LocationId);
        });
    }
}
