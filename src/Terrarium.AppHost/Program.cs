var builder = DistributedApplication.CreateBuilder(args);

// Azure Container Apps environment for deployment
builder.AddAzureContainerAppEnvironment("terrarium-env");

// SQL Server — the Terrarium game database (Docker for dev, Azure SQL for prod)
var sql = builder.AddSqlServer("sql")
    .WithLifetime(ContainerLifetime.Persistent);

var terrariumDb = sql.AddDatabase("Terrarium");

// Terrarium Server — the game's API backend (peer discovery, species registration, etc.)
var server = builder.AddProject<Projects.Terrarium_Server>("server", o => o.LaunchProfileName = "http")
    .WithReference(terrariumDb)
    .WaitFor(terrariumDb);

// Terrarium Web — Blazor frontend for the creature ecosystem
builder.AddProject<Projects.Terrarium_Web>("web", o => o.LaunchProfileName = "http")
    .WithExternalHttpEndpoints()
    .WithReference(server)
    .WaitFor(server);

builder.Build().Run();
