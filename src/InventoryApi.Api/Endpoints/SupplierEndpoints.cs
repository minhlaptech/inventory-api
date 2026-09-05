using InventoryApi.Domain.Entities;
using InventoryApi.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InventoryApi.Api.Endpoints;

public static class SupplierEndpoints
{
    public static void MapSupplierEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/suppliers")
            .WithTags("Suppliers")
            .WithOpenApi();

        // GET: All suppliers
        group.MapGet("/", async (InventoryDbContext db, CancellationToken ct) =>
        {
            var suppliers = await db.Suppliers
                .AsNoTracking()
                .Where(s => s.IsActive)
                .OrderByDescending(s => s.Rating)
                .Select(s => new SupplierResponse(
                    s.Id,
                    s.Name,
                    s.ContactEmail,
                    s.ContactPhone,
                    s.Rating,
                    s.Products.Count(p => p.IsActive)
                ))
                .ToListAsync(ct);

            return Results.Ok(suppliers);
        })
        .WithName("GetSuppliers")
        .WithSummary("Retrieves all active vendors/suppliers ordered by performance rating");

        // GET: Supplier by ID
        group.MapGet("/{id:guid}", async (Guid id, InventoryDbContext db, CancellationToken ct) =>
        {
            var supplier = await db.Suppliers
                .AsNoTracking()
                .Include(s => s.Products.Where(p => p.IsActive))
                .FirstOrDefaultAsync(s => s.Id == id, ct);

            if (supplier is null) return Results.NotFound(new { error = $"Supplier '{id}' not found" });

            return Results.Ok(supplier);
        })
        .WithName("GetSupplierById")
        .WithSummary("Retrieves supplier profile and supplied catalog");

        // POST: Create Supplier
        group.MapPost("/", async (CreateSupplierRequest request, InventoryDbContext db, CancellationToken ct) =>
        {
            var supplier = new Supplier
            {
                Name = request.Name.Trim(),
                ContactEmail = request.ContactEmail.Trim().ToLowerInvariant(),
                ContactPhone = request.ContactPhone?.Trim(),
                Address = request.Address?.Trim(),
                Website = request.Website?.Trim(),
                Rating = request.Rating
            };

            db.Suppliers.Add(supplier);
            await db.SaveChangesAsync(ct);

            return Results.Created($"/api/v1/suppliers/{supplier.Id}", new { id = supplier.Id, name = supplier.Name });
        })
        .WithName("CreateSupplier")
        .WithSummary("Registers a new supplier vendor");
    }
}

public record CreateSupplierRequest(string Name, string ContactEmail, string? ContactPhone, string? Address, string? Website, double Rating = 3.0);
public record SupplierResponse(Guid Id, string Name, string ContactEmail, string? ContactPhone, double Rating, int ProductCount);
