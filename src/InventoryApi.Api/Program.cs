using InventoryApi.Api.Endpoints;
using InventoryApi.Infrastructure;
using InventoryApi.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Database Configuration - PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=inventory_db;Username=postgres;Password=postgres;";

builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseNpgsql(connectionString));

// 2. Distributed Caching (In-memory fallback or Redis)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSingleton<ICacheService, CacheService>();

// 3. API Explorer & Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Inventory & Stock Movement API",
        Version = "v1",
        Description = "Enterprise Inventory Management REST API - ASP.NET Core 8 Minimal APIs, PostgreSQL & Redis"
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Inventory API v1"));
}

app.UseHttpsRedirection();

// 4. Map Minimal API Vertical Slices
app.MapProductEndpoints();
app.MapCategoryEndpoints();
app.MapSupplierEndpoints();
app.MapStockMovementEndpoints();

app.Run();
