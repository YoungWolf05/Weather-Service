using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Weather.Application.Abstractions;
using Weather.Infrastructure.Persistence;
using Weather.Infrastructure.Repositories;

namespace Weather.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<WeatherDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IWeatherReadRepository, WeatherReadRepository>();
        services.AddScoped<IAlertRepository, AlertRepository>();

        return services;
    }
}
