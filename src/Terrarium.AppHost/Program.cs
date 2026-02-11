var builder = DistributedApplication.CreateBuilder(args);

// Terrarium Server — the game's API backend (peer discovery, species registration, etc.)
// Uncomment when Terrarium.Server project is created:
// var server = builder.AddProject<Projects.Terrarium_Server>("server");

// Terrarium Web — Blazor frontend for the creature ecosystem
// Uncomment when Terrarium.Web project is created:
// builder.AddProject<Projects.Terrarium_Web>("web")
//     .WithExternalHttpEndpoints()
//     .WithReference(server)
//     .WaitFor(server);

builder.Build().Run();
