using System.Text.Json.Serialization;
using CrossBorderPayments.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using CrossBorderPayments.Api.Services;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "CrossBorderPayments.Session";
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = false;
        options.Events.OnValidatePrincipal = context =>
        {
            if (!builder.Environment.IsDevelopment() &&
                context.Principal?.GetWorkspaceId() == Guid.Empty)
            {
                context.RejectPrincipal();
            }

            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();

// Register SQL Server and Entity Framework Core.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Register controller-based API endpoints.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter());
    });

// Generate OpenAPI documentation.
builder.Services.AddOpenApi();

builder.Services.AddScoped<CurrencyQuoteService>();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<
    IExchangeRateProvider,
    BankOfCanadaExchangeRateProvider>(client =>
{
    client.BaseAddress =
        new Uri("https://www.bankofcanada.ca/valet/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowCredentials()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// Expose OpenAPI documentation during development.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

// Connect controller routes such as /api/beneficiaries.
app.MapControllers();

app.Run();
