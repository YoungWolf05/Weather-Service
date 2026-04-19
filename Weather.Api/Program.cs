using System.Reflection;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Weather.Api.Exceptions;
using Weather.Api.Hubs;
using Weather.Api.Services;
using Weather.Application;
using Weather.Application.Alerts.Commands.EvaluateAlerts;
using Weather.Application.Abstractions;
using Weather.Infrastructure;
using Weather.Infrastructure.Persistence;
using Weather.Seeder;
using Weather.Seeder.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("weatherdb")
    ?? throw new InvalidOperationException("Connection string 'weatherdb' is required.");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddWeatherSeeder();

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddSingleton<IAlertNotifier, SignalRAlertNotifier>();
builder.Services.AddHostedService<WeatherSeedWorker>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Weather API", Version = "v1" });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseExceptionHandler();
app.UseStaticFiles();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WeatherDbContext>();
    var seeder = scope.ServiceProvider.GetRequiredService<SingaporeWeatherSeeder>();
    var sender = scope.ServiceProvider.GetRequiredService<ISender>();

    await db.Database.MigrateAsync();
    await seeder.SeedAsync();
    await sender.Send(new EvaluateAlertsCommand());
}

app.MapControllers();
app.MapHub<AlertsHub>("/hubs/alerts");

app.MapGet("/alerts/demo", () => Results.Redirect("/alerts-demo.html")).ExcludeFromDescription();
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run();
