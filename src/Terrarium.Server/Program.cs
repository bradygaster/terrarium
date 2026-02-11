var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Terrarium Server");

app.Run();

// Make Program visible to integration tests using WebApplicationFactory<Program>
public partial class Program { }
