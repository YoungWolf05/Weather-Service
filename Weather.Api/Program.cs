using Weather.Application.Abstractions;
using Weather.Api.Endpoints;
using Weather.Infrastructure;
using Weather.Infrastructure.Persistence;
using Weather.Seeder;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("weatherdb")
    ?? throw new InvalidOperationException("Connection string 'weatherdb' is required.");

builder.Services.AddInfrastructure(connectionString);
builder.Services.AddWeatherSeeder();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
    c.SwaggerDoc("v1", new() { Title = "Weather API", Version = "v1" }));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// Create schema and seed on startup
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WeatherDbContext>();
    var seeder = scope.ServiceProvider.GetRequiredService<IWeatherDataSeeder>();

    await db.Database.EnsureCreatedAsync();
    await seeder.SeedAsync();
}

app.MapWeatherEndpoints();

app.Run();
