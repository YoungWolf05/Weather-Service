var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithImage("postgres:18")
    .WithHostPort(5432);

var weatherDb = postgres.AddDatabase("weatherdb");

builder.AddProject<Projects.Weather_Api>("weatherapi")
    .WithReference(weatherDb)
    .WaitFor(postgres);

builder.Build().Run();
