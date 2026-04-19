using Weather.Application.Abstractions;

namespace Weather.Api.Services;

public sealed class WeatherSeedWorker(
    IServiceScopeFactory serviceScopeFactory,
    IConfiguration configuration,
    ILogger<WeatherSeedWorker> logger) : BackgroundService
{
    private readonly TimeSpan interval = TimeSpan.FromSeconds(
        Math.Max(configuration.GetValue<int?>("WeatherSeeder:IntervalSeconds") ?? 60, 15));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Weather seed worker started. Polling upstream every {IntervalSeconds} seconds.",
            interval.TotalSeconds);

        using var timer = new PeriodicTimer(interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken))
                    break;

                await using var scope = serviceScopeFactory.CreateAsyncScope();
                var seeder = scope.ServiceProvider.GetRequiredService<IWeatherDataSeeder>();
                await seeder.SeedAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Weather seed worker iteration failed.");
            }
        }
    }
}
