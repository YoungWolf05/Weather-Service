namespace Weather.Application.Abstractions;

public interface IWeatherDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
