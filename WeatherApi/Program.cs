using WeatherApi.Data;
using WeatherApi.Endpoints;
using WeatherApi.Services;

var builder = WebApplication.CreateBuilder(args);

// PostgreSQL via Aspire (connection string injected as "weatherdb")
builder.AddNpgsqlDbContext<WeatherDbContext>("weatherdb");

// Named HttpClient for Singapore data.gov.sg APIs
builder.Services.AddHttpClient("sg-weather", c =>
    c.BaseAddress = new Uri("https://api-open.data.gov.sg/v2/real-time/api/"));

builder.Services.AddScoped<WeatherSeeder>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
    c.SwaggerDoc("v1", new() { Title = "Weather API", Version = "v1" }));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// Create schema and seed on startup
await using (var scope = app.Services.CreateAsyncScope())
{
    var db     = scope.ServiceProvider.GetRequiredService<WeatherDbContext>();
    var seeder = scope.ServiceProvider.GetRequiredService<WeatherSeeder>();

    await db.Database.EnsureCreatedAsync();
    await seeder.SeedAsync();
}

app.MapWeatherEndpoints();

app.Run();
