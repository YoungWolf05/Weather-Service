var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithImage("postgres:18")
    .WithHostPort(5432);

builder.Build().Run();
