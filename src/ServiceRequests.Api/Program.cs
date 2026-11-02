using System.Globalization;
using ServiceRequests.Infrastructure;
using ServiceRequests.Infrastructure.Persistence.Seeding;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddInfrastructure(builder.Configuration);
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// `dotnet run -- seed [count]` recreates the demo data and exits without starting the server
if (args is ["seed", ..])
{
    if (args.Length > 1 && !int.TryParse(args[1], CultureInfo.InvariantCulture, out _))
    {
        Console.Error.WriteLine("Usage: seed [count]");
        Environment.ExitCode = 1;
        return;
    }

    var count = args.Length > 1 ? int.Parse(args[1], CultureInfo.InvariantCulture) : DataSeeder.DefaultCount;
    await DatabaseInitializer.ReseedAsync(app.Services, count, CancellationToken.None);
    return;
}

if (app.Configuration.GetValue("Database:InitializeOnStartup", true))
{
    await DatabaseInitializer.InitializeAsync(app.Services, CancellationToken.None);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
