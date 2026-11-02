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
    var count = DataSeeder.DefaultCount;
    if (args.Length > 1 && (!int.TryParse(args[1], CultureInfo.InvariantCulture, out count) || count < 0))
    {
        Console.Error.WriteLine("Usage: seed [count], where count is zero or more");
        Environment.ExitCode = 1;
        return;
    }

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
