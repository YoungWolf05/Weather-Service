using Microsoft.EntityFrameworkCore;
using Weather.Domain.Entities;

namespace Weather.Infrastructure.Persistence;

public class WeatherDbContext(DbContextOptions<WeatherDbContext> options) : DbContext(options)
{
    public DbSet<Location>           Locations           => Set<Location>();
    public DbSet<WeatherObservation> WeatherObservations => Set<WeatherObservation>();
    public DbSet<ForecastEntry>      ForecastEntries     => Set<ForecastEntry>();
    public DbSet<AlertSubscription>  AlertSubscriptions  => Set<AlertSubscription>();
    public DbSet<TriggeredAlert>     TriggeredAlerts     => Set<TriggeredAlert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Location>(entity =>
        {
            entity.ToTable("locations");
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Type).HasMaxLength(10).IsRequired();
            entity.Property(x => x.ExternalId).HasMaxLength(50);
            entity.Property(x => x.Latitude).HasPrecision(9, 6);
            entity.Property(x => x.Longitude).HasPrecision(9, 6);
            entity.Property(x => x.Region).HasMaxLength(10);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(x => x.UpdatedAt).HasDefaultValueSql("NOW()");
            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasIndex(x => x.ExternalId);
        });

        modelBuilder.Entity<WeatherObservation>(entity =>
        {
            entity.ToTable("weather_observations");
            entity.Property(x => x.TemperatureCelsius).HasPrecision(5, 2);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
            entity.HasIndex(x => new { x.LocationId, x.ObservedAt }).IsUnique();
            entity.HasIndex(x => x.ObservedAt);
            entity.HasOne(x => x.Location)
                .WithMany(x => x.Observations)
                .HasForeignKey(x => x.LocationId);
        });

        modelBuilder.Entity<ForecastEntry>(entity =>
        {
            entity.ToTable("weather_forecasts");
            entity.Property(x => x.ForecastType).HasMaxLength(10).IsRequired();
            entity.Property(x => x.ForecastText).HasMaxLength(100).IsRequired();
            entity.Property(x => x.TempLowCelsius).HasPrecision(5, 2);
            entity.Property(x => x.TempHighCelsius).HasPrecision(5, 2);
            entity.Property(x => x.HumidityLowPct).HasPrecision(5, 2);
            entity.Property(x => x.HumidityHighPct).HasPrecision(5, 2);
            entity.Property(x => x.WindDirection).HasMaxLength(10);
            entity.Property(x => x.WindSpeedLowKmh).HasPrecision(6, 2);
            entity.Property(x => x.WindSpeedHighKmh).HasPrecision(6, 2);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
            entity.HasIndex(x => new { x.LocationId, x.IssuedAt, x.ValidFrom, x.ForecastType }).IsUnique();
            entity.HasIndex(x => new { x.ValidFrom, x.LocationId });
            entity.HasOne(x => x.Location)
                .WithMany(x => x.Forecasts)
                .HasForeignKey(x => x.LocationId);
        });

        modelBuilder.Entity<AlertSubscription>(entity =>
        {
            entity.ToTable("alert_subscriptions");
            entity.Property(x => x.Email).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Condition).HasMaxLength(10).IsRequired();
            entity.Property(x => x.ThresholdCelsius).HasPrecision(5, 2);
            entity.Property(x => x.IsActive).HasDefaultValue(true);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
            entity.HasIndex(x => new { x.Email, x.LocationId, x.Condition }).IsUnique();
            entity.HasOne(x => x.Location)
                .WithMany(x => x.AlertSubscriptions)
                .HasForeignKey(x => x.LocationId);
        });

        modelBuilder.Entity<TriggeredAlert>(entity =>
        {
            entity.ToTable("alert_triggered");
            entity.Property(x => x.TemperatureCelsius).HasPrecision(5, 2);
            entity.Property(x => x.TriggeredAt).HasDefaultValueSql("NOW()");
            entity.HasIndex(x => new { x.AlertSubscriptionId, x.ObservationId }).IsUnique();
            entity.HasOne(x => x.AlertSubscription)
                .WithMany(x => x.TriggeredAlerts)
                .HasForeignKey(x => x.AlertSubscriptionId);
            entity.HasOne(x => x.Observation)
                .WithMany()
                .HasForeignKey(x => x.ObservationId);
        });
    }
}
