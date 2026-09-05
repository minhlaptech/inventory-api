namespace InventoryApi.Domain.Entities;

/// <summary>
/// Base entity with audit fields and soft-delete support.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}

/// <summary>
/// Represents a product category for organizing inventory.
/// Example: "Electronics", "Office Supplies", "Raw Materials".
/// </summary>
public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Slug { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    // Navigation
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

/// <summary>
/// Represents a physical product in the warehouse inventory.
/// Tracks quantity, pricing, and reorder thresholds.
/// </summary>
public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Stock Keeping Unit — unique product identifier for warehouse operations.
    /// Format: "PRD-XXXX" (e.g., "PRD-0001").
    /// </summary>
    public string Sku { get; set; } = string.Empty;

    public string? Description { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal CostPrice { get; set; }

    /// <summary>
    /// Current quantity available in warehouse.
    /// Updated automatically via StockMovement records.
    /// </summary>
    public int QuantityInStock { get; set; }

    /// <summary>
    /// Minimum stock level that triggers a low-stock alert.
    /// When QuantityInStock drops below this, the system flags the product.
    /// </summary>
    public int ReorderLevel { get; set; } = 10;

    /// <summary>
    /// Unit of measurement (e.g., "pcs", "kg", "liters").
    /// </summary>
    public string Unit { get; set; } = "pcs";

    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;

    public Guid CategoryId { get; set; }
    public Guid? SupplierId { get; set; }

    // Computed
    public bool IsLowStock => QuantityInStock <= ReorderLevel;
    public decimal ProfitMargin => UnitPrice > 0 ? Math.Round((UnitPrice - CostPrice) / UnitPrice * 100, 2) : 0;

    // Navigation
    public Category Category { get; set; } = null!;
    public Supplier? Supplier { get; set; }
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}

/// <summary>
/// Represents a supplier/vendor that provides products.
/// </summary>
public class Supplier : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public string? Address { get; set; }
    public string? Website { get; set; }

    /// <summary>
    /// Performance rating from 1-5 based on delivery reliability.
    /// </summary>
    public double Rating { get; set; } = 3.0;

    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

/// <summary>
/// Tracks every stock change — inbound (purchase), outbound (sale), adjustment, or return.
/// Provides a complete audit trail of inventory quantity changes.
/// </summary>
public class StockMovement : BaseEntity
{
    public Guid ProductId { get; set; }

    /// <summary>
    /// Type of stock movement: Inbound, Outbound, Adjustment, Return.
    /// </summary>
    public MovementType Type { get; set; }

    /// <summary>
    /// Quantity changed. Positive for inbound, negative for outbound.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Stock quantity before this movement was applied.
    /// </summary>
    public int PreviousStock { get; set; }

    /// <summary>
    /// Stock quantity after this movement was applied.
    /// </summary>
    public int NewStock { get; set; }

    /// <summary>
    /// Reference number (e.g., PO number, invoice number).
    /// </summary>
    public string? ReferenceNumber { get; set; }

    public string? Notes { get; set; }
    public string? PerformedBy { get; set; }

    // Navigation
    public Product Product { get; set; } = null!;
}

/// <summary>
/// Types of stock movements for audit tracking.
/// </summary>
public enum MovementType
{
    /// <summary>Goods received from supplier (increases stock).</summary>
    Inbound = 1,

    /// <summary>Goods shipped to customer (decreases stock).</summary>
    Outbound = 2,

    /// <summary>Manual inventory correction (increase or decrease).</summary>
    Adjustment = 3,

    /// <summary>Customer return (increases stock).</summary>
    Return = 4
}
