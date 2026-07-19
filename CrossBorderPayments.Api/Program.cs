using System.Text.Json.Serialization;
using CrossBorderPayments.Api.Data;
using Microsoft.EntityFrameworkCore;
using CrossBorderPayments.Api.Services;


var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
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

app.UseCors("Frontend");

// Connect controller routes such as /api/beneficiaries.
app.MapControllers();

app.Run();