using Microsoft.Extensions.DependencyInjection;
using Weather.Application.Abstractions;
using Weather.Seeder.Services;

namespace Weather.Seeder;

public static class DependencyInjection
{
    public static IServiceCollection AddWeatherSeeder(this IServiceCollection services)
    {
        services.AddHttpClient("sg-weather", client =>
            client.BaseAddress = new Uri("https://api-open.data.gov.sg/v2/real-time/api/"));

        services.AddScoped<IWeatherDataSeeder, SingaporeWeatherSeeder>();

        return services;
    }
}
