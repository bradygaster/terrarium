var builder = DistributedApplication.CreateBuilder(args);

// SQL Server — the Terrarium game database (Docker for dev, Azure SQL for prod)
var sqlPassword = builder.AddParameter("sql-password", secret: true);
var sql = builder.AddSqlServer("sql", password: sqlPassword)
    .WithLifetime(ContainerLifetime.Persistent);

var terrariumDb = sql.AddDatabase("Terrarium");

// Terrarium Server — the game's API backend (peer discovery, species registration, etc.)
var server = builder.AddProject<Projects.Terrarium_Server>("server")
    .WithReference(terrariumDb)
    .WaitFor(terrariumDb);

// Terrarium Web — Blazor frontend for the creature ecosystem
builder.AddProject<Projects.Terrarium_Web>("web")
    .WithExternalHttpEndpoints()
    .WithReference(server)
    .WaitFor(server);

builder.Build().Run();
