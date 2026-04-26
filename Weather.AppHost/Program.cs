var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithImage("postgres:18")
    .WithHostPort(5432);

var weatherDb = postgres.AddDatabase("weatherdb");

var weatherApi = builder.AddProject<Projects.Weather_Api>("weatherapi")
    .WithReference(weatherDb)
    .WaitFor(postgres);

weatherApi.WithUrl("http://localhost:5000/alerts/demo", "Alerts Demo");

builder.Build().Run();


