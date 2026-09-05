using InventoryApi.Domain.Entities;
using InventoryApi.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InventoryApi.Api.Endpoints;

/// <summary>
/// Minimal API endpoint definitions for Product operations.
/// Follows the vertical slice pattern — each endpoint is a self-contained handler.
/// </summary>
public static class ProductEndpoints
{
    public static void MapProductEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/products")
            .WithTags("Products")
            .WithOpenApi();

        // ─── GET: List Products (Paginated) ──────────
        group.MapGet("/", async (
            InventoryDbContext db,
            int pageNumber = 1,
            int pageSize = 20,
            string? search = null,
            Guid? categoryId = null,
            bool? lowStockOnly = null,
            CancellationToken ct = default) =>
        {
            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = db.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .AsNoTracking()
                .Where(p => p.IsActive);

            // Apply filters
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(term) ||
                    p.Sku.ToLower().Contains(term));
            }

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (lowStockOnly == true)
                query = query.Where(p => p.QuantityInStock <= p.ReorderLevel);

            var totalCount = await query.CountAsync(ct);
            var items = await query
                .OrderBy(p => p.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductResponse(
                    p.Id, p.Name, p.Sku, p.Description,
                    p.UnitPrice, p.CostPrice,
                    p.QuantityInStock, p.ReorderLevel,
                    p.QuantityInStock <= p.ReorderLevel,
                    p.Unit, p.ImageUrl,
                    p.Category.Name,
                    p.Supplier != null ? p.Supplier.Name : null,
                    p.CreatedAt
                ))
                .ToListAsync(ct);

            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            return Results.Ok(new PagedResult<ProductResponse>(
                items, pageNumber, pageSize, totalCount, totalPages));
        })
        .WithName("GetProducts")
        .WithSummary("Retrieves a paginated list of products with optional search and filtering");

        // ─── GET: Product by ID ──────────────────────
        group.MapGet("/{id:guid}", async (Guid id, InventoryDbContext db, CancellationToken ct) =>
        {
            var product = await db.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .Include(p => p.StockMovements.OrderByDescending(sm => sm.CreatedAt).Take(10))
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id, ct);

            if (product is null) return Results.NotFound(new { error = $"Product '{id}' not found" });

            var response = new ProductDetailResponse(
                product.Id, product.Name, product.Sku, product.Description,
                product.UnitPrice, product.CostPrice,
                product.QuantityInStock, product.ReorderLevel,
                product.QuantityInStock <= product.ReorderLevel,
                product.UnitPrice > 0
                    ? Math.Round((product.UnitPrice - product.CostPrice) / product.UnitPrice * 100, 2)
                    : 0,
                product.Unit, product.ImageUrl,
                product.Category.Name,
                product.Supplier?.Name,
                product.StockMovements.Select(sm => new StockMovementResponse(
                    sm.Id, sm.Type.ToString(), sm.Quantity,
                    sm.PreviousStock, sm.NewStock,
                    sm.ReferenceNumber, sm.Notes, sm.PerformedBy, sm.CreatedAt
                )).ToList(),
                product.CreatedAt, product.UpdatedAt
            );

            return Results.Ok(response);
        })
        .WithName("GetProductById")
        .WithSummary("Retrieves detailed product information including recent stock movements");

        // ─── POST: Create Product ────────────────────
        group.MapPost("/", async (CreateProductRequest request, InventoryDbContext db, CancellationToken ct) =>
        {
            // Validate unique SKU
            var skuExists = await db.Products.AnyAsync(p => p.Sku == request.Sku, ct);
            if (skuExists)
                return Results.Conflict(new { error = $"SKU '{request.Sku}' already exists" });

            // Validate category
            var categoryExists = await db.Categories.AnyAsync(c => c.Id == request.CategoryId, ct);
            if (!categoryExists)
                return Results.BadRequest(new { error = $"Category '{request.CategoryId}' not found" });

            var product = new Product
            {
                Name = request.Name.Trim(),
                Sku = request.Sku.Trim().ToUpperInvariant(),
                Description = request.Description?.Trim(),
                UnitPrice = request.UnitPrice,
                CostPrice = request.CostPrice,
                QuantityInStock = request.InitialStock,
                ReorderLevel = request.ReorderLevel,
                Unit = request.Unit ?? "pcs",
                CategoryId = request.CategoryId,
                SupplierId = request.SupplierId
            };

            db.Products.Add(product);

            // Record initial stock as inbound movement
            if (request.InitialStock > 0)
            {
                db.StockMovements.Add(new StockMovement
                {
                    ProductId = product.Id,
                    Type = MovementType.Inbound,
                    Quantity = request.InitialStock,
                    PreviousStock = 0,
                    NewStock = request.InitialStock,
                    Notes = "Initial stock on product creation",
                    PerformedBy = "system"
                });
            }

            await db.SaveChangesAsync(ct);

            return Results.Created($"/api/v1/products/{product.Id}", new { id = product.Id, sku = product.Sku });
        })
        .WithName("CreateProduct")
        .WithSummary("Creates a new product with optional initial stock");

        // ─── POST: Record Stock Movement ─────────────
        group.MapPost("/{id:guid}/stock", async (
            Guid id,
            StockMovementRequest request,
            InventoryDbContext db,
            CancellationToken ct) =>
        {
            var product = await db.Products.FindAsync([id], ct);
            if (product is null) return Results.NotFound(new { error = $"Product '{id}' not found" });

            var previousStock = product.QuantityInStock;
            var quantityDelta = request.Type switch
            {
                MovementType.Inbound or MovementType.Return => Math.Abs(request.Quantity),
                MovementType.Outbound => -Math.Abs(request.Quantity),
                MovementType.Adjustment => request.Quantity, // Can be positive or negative
                _ => throw new ArgumentOutOfRangeException()
            };

            var newStock = previousStock + quantityDelta;
            if (newStock < 0)
                return Results.BadRequest(new { error = $"Insufficient stock. Current: {previousStock}, Requested: {Math.Abs(request.Quantity)}" });

            product.QuantityInStock = newStock;

            var movement = new StockMovement
            {
                ProductId = id,
                Type = request.Type,
                Quantity = request.Quantity,
                PreviousStock = previousStock,
                NewStock = newStock,
                ReferenceNumber = request.ReferenceNumber,
                Notes = request.Notes,
                PerformedBy = request.PerformedBy ?? "api-user"
            };

            db.StockMovements.Add(movement);
            await db.SaveChangesAsync(ct);

            return Results.Ok(new
            {
                movementId = movement.Id,
                productId = id,
                previousStock,
                newStock,
                isLowStock = newStock <= product.ReorderLevel
            });
        })
        .WithName("RecordStockMovement")
        .WithSummary("Records a stock movement (inbound, outbound, adjustment, or return)");

        // ─── GET: Low Stock Alerts ───────────────────
        group.MapGet("/alerts/low-stock", async (InventoryDbContext db, CancellationToken ct) =>
        {
            var lowStockItems = await db.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .AsNoTracking()
                .Where(p => p.IsActive && p.QuantityInStock <= p.ReorderLevel)
                .OrderBy(p => p.QuantityInStock)
                .Select(p => new LowStockAlert(
                    p.Id, p.Name, p.Sku,
                    p.QuantityInStock, p.ReorderLevel,
                    p.Category.Name,
                    p.Supplier != null ? p.Supplier.Name : "No supplier",
                    p.QuantityInStock == 0 ? "Critical" : "Warning"
                ))
                .ToListAsync(ct);

            return Results.Ok(new { count = lowStockItems.Count, items = lowStockItems });
        })
        .WithName("GetLowStockAlerts")
        .WithSummary("Retrieves all products below their reorder threshold");
    }
}

// ─── Request/Response Records ─────────────────────

public record CreateProductRequest(
    string Name, string Sku, string? Description,
    decimal UnitPrice, decimal CostPrice,
    int InitialStock, int ReorderLevel,
    string? Unit, Guid CategoryId, Guid? SupplierId);

public record StockMovementRequest(
    MovementType Type, int Quantity,
    string? ReferenceNumber, string? Notes, string? PerformedBy);

public record ProductResponse(
    Guid Id, string Name, string Sku, string? Description,
    decimal UnitPrice, decimal CostPrice,
    int QuantityInStock, int ReorderLevel, bool IsLowStock,
    string Unit, string? ImageUrl,
    string CategoryName, string? SupplierName,
    DateTime CreatedAt);

public record ProductDetailResponse(
    Guid Id, string Name, string Sku, string? Description,
    decimal UnitPrice, decimal CostPrice,
    int QuantityInStock, int ReorderLevel, bool IsLowStock,
    decimal ProfitMarginPercent,
    string Unit, string? ImageUrl,
    string CategoryName, string? SupplierName,
    IReadOnlyList<StockMovementResponse> RecentMovements,
    DateTime CreatedAt, DateTime? UpdatedAt);

public record StockMovementResponse(
    Guid Id, string Type, int Quantity,
    int PreviousStock, int NewStock,
    string? ReferenceNumber, string? Notes, string? PerformedBy,
    DateTime CreatedAt);

public record LowStockAlert(
    Guid ProductId, string Name, string Sku,
    int CurrentStock, int ReorderLevel,
    string Category, string Supplier, string Severity);

public record PagedResult<T>(
    IReadOnlyList<T> Items, int PageNumber, int PageSize,
    int TotalCount, int TotalPages)
{
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
