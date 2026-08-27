using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using web_app_Csharp.Data;
using web_app_Csharp.Features.Accounts;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("BankDatabase");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'BankDatabase' is required. Configure it with user secrets or the ConnectionStrings__BankDatabase environment variable.");
}

builder.Services.AddDbContext<BankContext>(options =>
    options.UseSqlServer(
        connectionString,
        sqlServerOptions => sqlServerOptions.EnableRetryOnFailure()));
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Local development applies migrations automatically; production deployments should run them separately.
    await using var scope = app.Services.CreateAsyncScope();
    var context = scope.ServiceProvider.GetRequiredService<BankContext>();
    await context.Database.MigrateAsync();
}

app.MapAccountEndpoints();

app.Run();

public partial class Program;
