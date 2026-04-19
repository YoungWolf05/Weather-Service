using System.Reflection;
using Weather.Api.Exceptions;
using Weather.Application;
using Weather.Application.Abstractions;
using Weather.Infrastructure;
using Weather.Infrastructure.Persistence;
using Weather.Seeder;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("weatherdb")
    ?? throw new InvalidOperationException("Connection string 'weatherdb' is required.");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddWeatherSeeder();

builder.Services.AddControllers();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Weather API", Version = "v1" });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseExceptionHandler();

// Create schema and seed on startup
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WeatherDbContext>();
    var seeder = scope.ServiceProvider.GetRequiredService<IWeatherDataSeeder>();

    await db.Database.EnsureCreatedAsync();
    await seeder.SeedAsync();
}

app.MapControllers();

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run();
