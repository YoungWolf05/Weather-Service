using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Weather.Application.Abstractions;
using Weather.Infrastructure.Persistence;
using Weather.Infrastructure.Services;

namespace Weather.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<WeatherDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IWeatherQueryService, WeatherQueryService>();

        return services;
    }
}
