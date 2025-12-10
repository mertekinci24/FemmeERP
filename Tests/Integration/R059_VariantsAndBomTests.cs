using System;
using System.Linq;
using System.Threading.Tasks;
using InventoryERP.Domain.Entities;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Persistence;
using Tests.Infrastructure;
using Xunit;

namespace Tests.Integration;

/// <summary>
/// R-059: Variants and BOM Integration Tests
/// Verifies that ProductVariant and BomItem (R-014 backend) persist correctly
/// </summary>
public class R059_VariantsAndBomTests : IDisposable
{
    private SqliteConnection? _connection;

    [Fact]
    public async Task WhenSavingProductVariants_ShouldPersistCorrectly()
    {
        // Arrange
        var (provider, conn) = TestServiceProviderFactory.CreateWithInMemoryDb();
        _connection = conn;
        var db = provider.GetRequiredService<AppDbContext>();

        var product = new Product
        {
            Sku = "TEST-VARIANT",
            Name = "Test Product with Variants",
            BaseUom = "EA",
            VatRate = 20,
            Active = true,
            Cost = 0,
            ReservedQty = 0
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        // Act: Add variants (simulating UI behavior)
        var variant1 = new ProductVariant
        {
            ProductId = product.Id,
            Code = "TEST-VARIANT-RED-L"
        };
        var variant2 = new ProductVariant
        {
            ProductId = product.Id,
            Code = "TEST-VARIANT-BLUE-M"
        };
        db.ProductVariants.Add(variant1);
        db.ProductVariants.Add(variant2);
        await db.SaveChangesAsync();

        // Assert
        var savedVariants = await db.ProductVariants
            .Where(v => v.ProductId == product.Id)
            .ToListAsync();

        savedVariants.Should().HaveCount(2);
        savedVariants.Should().Contain(v => v.Code == "TEST-VARIANT-RED-L");
        savedVariants.Should().Contain(v => v.Code == "TEST-VARIANT-BLUE-M");
    }

    [Fact]
    public async Task WhenSavingBomItems_ShouldPersistCorrectly()
    {
        // Arrange
        var (provider, conn) = TestServiceProviderFactory.CreateWithInMemoryDb();
        _connection = conn;
        var db = provider.GetRequiredService<AppDbContext>();

        // Create parent product (finished good)
        var finishedGood = new Product
        {
            Sku = "BIKE-001",
            Name = "Bicycle",
            BaseUom = "EA",
            VatRate = 20,
            Active = true,
            Cost = 0,
            ReservedQty = 0
        };
        db.Products.Add(finishedGood);

        // Create component products (raw materials)
        var wheel = new Product
        {
            Sku = "WHEEL-001",
            Name = "Wheel",
            BaseUom = "EA",
            VatRate = 20,
            Active = true,
            Cost = 50,
            ReservedQty = 0
        };
        var frame = new Product
        {
            Sku = "FRAME-001",
            Name = "Frame",
            BaseUom = "EA",
            VatRate = 20,
            Active = true,
            Cost = 100,
            ReservedQty = 0
        };
        db.Products.AddRange(wheel, frame);
        await db.SaveChangesAsync();

        // Act: Add BOM items (simulating UI behavior)
        var bomItem1 = new BomItem
        {
            ParentProductId = finishedGood.Id,
            ComponentProductId = wheel.Id,
            QtyPer = 2m // 2 wheels per bike
        };
        var bomItem2 = new BomItem
        {
            ParentProductId = finishedGood.Id,
            ComponentProductId = frame.Id,
            QtyPer = 1m // 1 frame per bike
        };
        db.BomItems.AddRange(bomItem1, bomItem2);
        await db.SaveChangesAsync();

        // Assert
        var savedBomItems = await db.BomItems
            .Include(b => b.ComponentProduct)
            .Where(b => b.ParentProductId == finishedGood.Id)
            .ToListAsync();

        savedBomItems.Should().HaveCount(2);
        savedBomItems.Should().Contain(b => b.ComponentProduct!.Sku == "WHEEL-001" && b.QtyPer == 2m);
        savedBomItems.Should().Contain(b => b.ComponentProduct!.Sku == "FRAME-001" && b.QtyPer == 1m);
    }

    [Fact]
    public async Task WhenDeletingProduct_ShouldCascadeDeleteVariantsAndBom()
    {
        // Arrange
        var (provider, conn) = TestServiceProviderFactory.CreateWithInMemoryDb();
        _connection = conn;
        var db = provider.GetRequiredService<AppDbContext>();

        // Create product with variants and BOM
        var product = new Product
        {
            Sku = "TEST-CASCADE",
            Name = "Test Cascade Delete",
            BaseUom = "EA",
            VatRate = 20,
            Active = true,
            Cost = 0,
            ReservedQty = 0
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var variant = new ProductVariant
        {
            ProductId = product.Id,
            Code = "TEST-CASCADE-V1"
        };
        db.ProductVariants.Add(variant);

        var component = new Product
        {
            Sku = "COMPONENT-001",
            Name = "Component",
            BaseUom = "EA",
            VatRate = 20,
            Active = true,
            Cost = 0,
            ReservedQty = 0
        };
        db.Products.Add(component);
        await db.SaveChangesAsync();

        var bomItem = new BomItem
        {
            ParentProductId = product.Id,
            ComponentProductId = component.Id,
            QtyPer = 1m
        };
        db.BomItems.Add(bomItem);
        await db.SaveChangesAsync();

        // Act: Delete parent product
        db.Products.Remove(product);
        await db.SaveChangesAsync();

        // Assert: Variants and BOM items should be cascade deleted
        var remainingVariants = await db.ProductVariants
            .Where(v => v.ProductId == product.Id)
            .ToListAsync();
        var remainingBomItems = await db.BomItems
            .Where(b => b.ParentProductId == product.Id)
            .ToListAsync();

        remainingVariants.Should().BeEmpty("variants should be cascade deleted");
        remainingBomItems.Should().BeEmpty("BOM items should be cascade deleted");
        
        // Component product should still exist
        var componentExists = await db.Products.AnyAsync(p => p.Id == component.Id);
        componentExists.Should().BeTrue("component product should not be deleted");
    }

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
    }
}
