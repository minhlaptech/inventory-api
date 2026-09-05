using InventoryApi.Domain.Entities;
using Xunit;

namespace InventoryApi.UnitTests;

public class ProductDomainTests
{
    [Fact]
    public void ProfitMargin_ShouldCalculateCorrectPercentage_WhenCostAndPriceAreValid()
    {
        // Arrange
        var product = new Product
        {
            Name = "Ergonomic Mechanical Keyboard",
            Sku = "PRD-KEY-001",
            UnitPrice = 100m,
            CostPrice = 60m
        };

        // Act
        var margin = product.ProfitMargin;

        // Assert
        Assert.Equal(40.00m, margin);
    }

    [Fact]
    public void ProfitMargin_ShouldReturnZero_WhenUnitPriceIsZero()
    {
        // Arrange
        var product = new Product
        {
            Name = "Promotional Sticker Pack",
            Sku = "PRD-STK-001",
            UnitPrice = 0m,
            CostPrice = 10m
        };

        // Act
        var margin = product.ProfitMargin;

        // Assert
        Assert.Equal(0m, margin);
    }

    [Theory]
    [InlineData(5, 10, true)]   // Below reorder level
    [InlineData(10, 10, true)]  // Exactly at reorder level
    [InlineData(11, 10, false)] // Above reorder level
    public void IsLowStock_ShouldReturnExpectedFlag_BasedOnQuantityAndReorderLevel(
        int stock, int reorderLevel, bool expectedLowStock)
    {
        // Arrange
        var product = new Product
        {
            Name = "USB-C Fast Charging Cable",
            Sku = "PRD-CAB-001",
            QuantityInStock = stock,
            ReorderLevel = reorderLevel
        };

        // Act & Assert
        Assert.Equal(expectedLowStock, product.IsLowStock);
    }

    [Fact]
    public void StockMovement_Inbound_ShouldTrackPreviousAndNewQuantitiesAccurately()
    {
        // Arrange
        var productId = Guid.NewGuid();
        int initialStock = 50;
        int inboundQty = 25;

        // Act
        var movement = new StockMovement
        {
            ProductId = productId,
            Type = MovementType.Inbound,
            Quantity = inboundQty,
            PreviousStock = initialStock,
            NewStock = initialStock + inboundQty,
            ReferenceNumber = "PO-2026-0901",
            Notes = "Restock shipment received from supplier"
        };

        // Assert
        Assert.Equal(75, movement.NewStock);
        Assert.Equal(MovementType.Inbound, movement.Type);
        Assert.Equal("PO-2026-0901", movement.ReferenceNumber);
    }

    [Fact]
    public void StockMovement_Outbound_ShouldDecreaseQuantityAccurately()
    {
        // Arrange
        var productId = Guid.NewGuid();
        int initialStock = 80;
        int outboundQty = 30;

        // Act
        var movement = new StockMovement
        {
            ProductId = productId,
            Type = MovementType.Outbound,
            Quantity = outboundQty,
            PreviousStock = initialStock,
            NewStock = initialStock - outboundQty,
            ReferenceNumber = "SO-2026-8842",
            Notes = "Fulfillment of customer purchase order"
        };

        // Assert
        Assert.Equal(50, movement.NewStock);
        Assert.Equal(MovementType.Outbound, movement.Type);
    }
}
