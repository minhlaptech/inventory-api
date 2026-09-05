using InventoryApi.Domain.Entities;
using InventoryApi.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InventoryApi.Api.Endpoints;

public static class StockMovementEndpoints
{
    public static void MapStockMovementEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/stock-movements")
            .WithTags("Stock Movements")
            .WithOpenApi();

        // GET: Audit trail of stock movements
        group.MapGet("/", async (
            InventoryDbContext db,
            int pageNumber = 1,
            int pageSize = 20,
            Guid? productId = null,
            MovementType? type = null,
            CancellationToken ct = default) =>
        {
            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = db.StockMovements
                .Include(sm => sm.Product)
                .AsNoTracking()
                .AsQueryable();

            if (productId.HasValue)
                query = query.Where(sm => sm.ProductId == productId.Value);

            if (type.HasValue)
                query = query.Where(sm => sm.Type == type.Value);

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(sm => sm.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(sm => new DetailedStockMovementResponse(
                    sm.Id,
                    sm.ProductId,
                    sm.Product.Name,
                    sm.Product.Sku,
                    sm.Type.ToString(),
                    sm.Quantity,
                    sm.PreviousStock,
                    sm.NewStock,
                    sm.ReferenceNumber,
                    sm.Notes,
                    sm.PerformedBy,
                    sm.CreatedAt
                ))
                .ToListAsync(ct);

            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            return Results.Ok(new PagedResult<DetailedStockMovementResponse>(
                items, pageNumber, pageSize, totalCount, totalPages));
        })
        .WithName("GetStockMovementsAuditTrail")
        .WithSummary("Retrieves historical audit trail of all warehouse stock movements");
    }
}

public record DetailedStockMovementResponse(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string ProductSku,
    string Type,
    int Quantity,
    int PreviousStock,
    int NewStock,
    string? ReferenceNumber,
    string? Notes,
    string? PerformedBy,
    DateTime CreatedAt);
