using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Weather.Infrastructure.Persistence;

namespace Weather.Infrastructure;

/// <summary>
/// Used exclusively by EF Core tooling (migrations) at design time.
/// Points at a local development database — not used at runtime.
/// </summary>
internal sealed class WeatherDbContextFactory : IDesignTimeDbContextFactory<WeatherDbContext>
{
    public WeatherDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<WeatherDbContext>()
            .UseNpgsql("Host=localhost;Database=weatherdb;Username=postgres;Password=postgres")
            .Options;

        return new WeatherDbContext(options);
    }
}


