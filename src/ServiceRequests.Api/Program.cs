using System.Globalization;
using System.Text.Json.Serialization;
using ServiceRequests.Api.ErrorHandling;
using ServiceRequests.Api.Identity;
using ServiceRequests.Api.Logging;
using ServiceRequests.Application;
using ServiceRequests.Application.Common;
using ServiceRequests.Infrastructure;
using ServiceRequests.Infrastructure.Persistence;
using ServiceRequests.Infrastructure.Persistence.Seeding;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Enums travel as names ("InProgress"), both in controller responses and in ProblemDetails
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .ConfigureApiBehaviorOptions(options =>
    {
        // Validation failures are logged with the field names only, never the values the client sent
        var createProblem = options.InvalidModelStateResponseFactory;
        options.InvalidModelStateResponseFactory = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            Log.ValidationFailed(logger, string.Join(", ", context.ModelState.Where(e => e.Value?.Errors.Count > 0).Select(e => e.Key)));
            return createProblem(context);
        };
    });
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HeaderCurrentUser>();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks().AddDbContextCheck<ServiceRequestsDbContext>();
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
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
