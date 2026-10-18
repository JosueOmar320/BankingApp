using System.Reflection;
using System.Text.Json.Serialization;
using Banking.API.Middleware;
using Banking.Application;
using Banking.Infrastructure;
using Banking.Infrastructure.Persistence;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// MVC + JSON: enums travel as their names ("Deposit", "M"), never as integers.
builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Every error (validation, business rule, not found, unexpected) is an RFC 7807 problem response.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Banking API",
        Version = "v1",
        Description = "Customers, bank accounts and ledger operations (deposits, withdrawals, interest).",
    });

    foreach (var xml in new[] { Assembly.GetExecutingAssembly().GetName().Name, typeof(Banking.Application.DependencyInjection).Assembly.GetName().Name })
    {
        var path = Path.Combine(AppContext.BaseDirectory, $"{xml}.xml");
        if (File.Exists(path))
        {
            options.IncludeXmlComments(path, includeControllerXmlComments: true);
        }
    }
});

builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.MapHealthChecks("/health");

// Create/upgrade the SQLite schema on start; sample data only when "Database:SeedSampleData" is true.
await app.Services.InitializeDatabaseAsync(seedSampleData: app.Configuration.GetValue<bool>("Database:SeedSampleData"));

await app.RunAsync();

/// <summary>Entry point marker so integration tests can host the API with <c>WebApplicationFactory</c>.</summary>
public partial class Program
{
}
