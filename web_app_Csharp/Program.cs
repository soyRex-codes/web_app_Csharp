using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using web_app_Csharp.Data;
using web_app_Csharp.Features.Accounts;
using web_app_Csharp.Features.Identity;
using web_app_Csharp.Features.Transfers;

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
builder.Services
    .AddIdentityCore<ApplicationUser>(options => options.User.RequireUniqueEmail = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<BankContext>()
    .AddSignInManager();
builder.Services
    .AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/access-denied";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});
builder.Services.AddAuthorization();
builder.Services.AddScoped<AccountOperationsService>();
builder.Services.AddRazorPages();
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
// Authentication and empty endpoint results should use the same RFC 7807 contract as explicit Problem results.
app.UseStatusCodePages(async statusCodeContext =>
{
    var response = statusCodeContext.HttpContext.Response;
    await Results.Problem(statusCode: response.StatusCode).ExecuteAsync(statusCodeContext.HttpContext);
});
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

if (!app.Environment.IsEnvironment("Testing"))
{
    await using var scope = app.Services.CreateAsyncScope();
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();

        // Local development applies migrations automatically; production deployments should run them separately.
        var context = scope.ServiceProvider.GetRequiredService<BankContext>();
        await context.Database.MigrateAsync();
    }

    await IdentityDataSeeder.EnsureRolesAsync(scope.ServiceProvider);

    if (app.Environment.IsDevelopment())
    {
        await IdentityDataSeeder.SeedDevelopmentAdminAsync(scope.ServiceProvider, app.Configuration);
    }
}

app.MapAccountEndpoints();
app.MapTransferEndpoints();
app.MapIdentityEndpoints();
app.MapRazorPages();

app.Run();

public partial class Program;
