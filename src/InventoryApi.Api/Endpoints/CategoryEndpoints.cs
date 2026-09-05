using InventoryApi.Domain.Entities;
using InventoryApi.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InventoryApi.Api.Endpoints;

public static class CategoryEndpoints
{
    public static void MapCategoryEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/categories")
            .WithTags("Categories")
            .WithOpenApi();

        // GET: All categories with product count
        group.MapGet("/", async (InventoryDbContext db, CancellationToken ct) =>
        {
            var categories = await db.Categories
                .AsNoTracking()
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .Select(c => new CategoryResponse(
                    c.Id,
                    c.Name,
                    c.Description,
                    c.Slug,
                    c.SortOrder,
                    c.Products.Count(p => p.IsActive)
                ))
                .ToListAsync(ct);

            return Results.Ok(categories);
        })
        .WithName("GetCategories")
        .WithSummary("Retrieves all active categories with product counts");

        // GET: Category by ID
        group.MapGet("/{id:guid}", async (Guid id, InventoryDbContext db, CancellationToken ct) =>
        {
            var category = await db.Categories
                .AsNoTracking()
                .Include(c => c.Products.Where(p => p.IsActive))
                .FirstOrDefaultAsync(c => c.Id == id, ct);

            if (category is null) return Results.NotFound(new { error = $"Category '{id}' not found" });

            return Results.Ok(new CategoryDetailResponse(
                category.Id,
                category.Name,
                category.Description,
                category.Slug,
                category.SortOrder,
                category.Products.Select(p => new CategoryProductItem(p.Id, p.Name, p.Sku, p.UnitPrice, p.QuantityInStock)).ToList()
            ));
        })
        .WithName("GetCategoryById")
        .WithSummary("Retrieves category details and its active products");

        // POST: Create Category
        group.MapPost("/", async (CreateCategoryRequest request, InventoryDbContext db, CancellationToken ct) =>
        {
            var slug = request.Name.Trim().ToLower().Replace(" ", "-");
            var slugExists = await db.Categories.AnyAsync(c => c.Slug == slug, ct);
            if (slugExists) return Results.Conflict(new { error = $"Category slug '{slug}' already exists" });

            var category = new Category
            {
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                Slug = slug,
                SortOrder = request.SortOrder
            };

            db.Categories.Add(category);
            await db.SaveChangesAsync(ct);

            return Results.Created($"/api/v1/categories/{category.Id}", new { id = category.Id, name = category.Name });
        })
        .WithName("CreateCategory")
        .WithSummary("Creates a new product category");
    }
}

public record CreateCategoryRequest(string Name, string? Description, int SortOrder = 0);
public record CategoryResponse(Guid Id, string Name, string? Description, string Slug, int SortOrder, int ProductCount);
public record CategoryDetailResponse(Guid Id, string Name, string? Description, string Slug, int SortOrder, IReadOnlyList<CategoryProductItem> Products);
public record CategoryProductItem(Guid Id, string Name, string Sku, decimal UnitPrice, int QuantityInStock);
