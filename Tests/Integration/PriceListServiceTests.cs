using System;
using System.Linq;
using System.Threading.Tasks;
using InventoryERP.Application.Products;
using InventoryERP.Domain.Entities;
using FluentAssertions;
using InventoryERP.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Xunit;

namespace Tests.Integration;

/// <summary>
/// Integration tests for PriceListService (R-041).
/// Verifies CRUD operations and effective price logic with real database.
/// </summary>
public class PriceListServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly PriceListService _sut;
    private readonly int _testProductId;

    public PriceListServiceTests()
    {
        // Setup in-memory SQLite database
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AppDbContext(options);
        _db.Database.Migrate(); // Apply migrations including AddPricesTable

        _sut = new PriceListService(_db);

        // Seed test product
        var product = new Product
        {
            Sku = "TEST-PRICE-001",
            Name = "Test Product for Prices",
            BaseUom = "EA",
            VatRate = 20,
            Active = true
        };
        _db.Products.Add(product);
        _db.SaveChanges();
        _testProductId = product.Id;
    }

    [Fact]
    public async Task AddPrice_Success()
    {
        // Arrange
        var dto = new CreatePriceDto(
            ProductId: _testProductId,
            ListCode: "NAKÝT",
            UomName: "EA",
            UnitPrice: 100.00m,
            Currency: "TRY",
            ValidFrom: null,
            ValidTo: null
        );

        // Act
        var result = await _sut.AddPriceAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.ProductId.Should().Be(_testProductId);
        result.ListCode.Should().Be("NAKÝT");
        result.UomName.Should().Be("EA");
        result.UnitPrice.Should().Be(100.00m);
        result.Currency.Should().Be("TRY");

        // Verify in database
        var dbPrice = await _db.Prices.FindAsync(result.Id);
        dbPrice.Should().NotBeNull();
        dbPrice!.ListCode.Should().Be("NAKÝT");
    }

    [Fact]
    public async Task AddPrice_InvalidProductId_ThrowsArgumentException()
    {
        // Arrange
        var dto = new CreatePriceDto(
            ProductId: 99999, // Non-existent
            ListCode: "TEST",
            UomName: "EA",
            UnitPrice: 100.00m,
            Currency: "TRY",
            ValidFrom: null,
            ValidTo: null
        );

        // Act & Assert
        var act = async () => await _sut.AddPriceAsync(dto);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Product with ID 99999 does not exist*");
    }

    [Fact]
    public async Task AddPrice_InvalidDateRange_ThrowsArgumentException()
    {
        // Arrange
        var dto = new CreatePriceDto(
            ProductId: _testProductId,
            ListCode: "TEST",
            UomName: "EA",
            UnitPrice: 100.00m,
            Currency: "TRY",
            ValidFrom: new DateTime(2025, 12, 31),
            ValidTo: new DateTime(2025, 1, 1) // ValidTo before ValidFrom
        );

        // Act & Assert
        var act = async () => await _sut.AddPriceAsync(dto);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*ValidFrom must be before ValidTo*");
    }

    [Fact]
    public async Task AddPrice_ZeroUnitPrice_ThrowsArgumentException()
    {
        // Arrange
        var dto = new CreatePriceDto(
            ProductId: _testProductId,
            ListCode: "TEST",
            UomName: "EA",
            UnitPrice: 0m, // Invalid
            Currency: "TRY",
            ValidFrom: null,
            ValidTo: null
        );

        // Act & Assert
        var act = async () => await _sut.AddPriceAsync(dto);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*UnitPrice must be greater than zero*");
    }

    [Fact]
    public async Task GetPricesByProductId_ReturnsMultiple()
    {
        // Arrange - Add 3 prices (NAKÝT, VADELÝ, BAYÝ) as per acceptance criteria
        var nakit = new CreatePriceDto(_testProductId, "NAKÝT", "EA", 100.00m, "TRY", null, null);
        var vadeli = new CreatePriceDto(_testProductId, "VADELÝ", "EA", 110.00m, "TRY", null, null);
        var bayi = new CreatePriceDto(_testProductId, "BAYÝ", "EA", 95.00m, "USD", null, null);

        await _sut.AddPriceAsync(nakit);
        await _sut.AddPriceAsync(vadeli);
        await _sut.AddPriceAsync(bayi);

        // Act
        var result = await _sut.GetPricesByProductIdAsync(_testProductId);

        // Assert
        result.Should().HaveCount(3);
        result.Should().ContainSingle(p => p.ListCode == "NAKÝT" && p.UnitPrice == 100.00m && p.Currency == "TRY");
        result.Should().ContainSingle(p => p.ListCode == "VADELÝ" && p.UnitPrice == 110.00m && p.Currency == "TRY");
        result.Should().ContainSingle(p => p.ListCode == "BAYÝ" && p.UnitPrice == 95.00m && p.Currency == "USD");
        
        // Verify ordered by ListCode then UomName
        result.Select(p => p.ListCode).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task UpdatePrice_Success()
    {
        // Arrange
        var original = await _sut.AddPriceAsync(new CreatePriceDto(
            _testProductId, "TEST", "EA", 100.00m, "TRY", null, null
        ));

        var update = new UpdatePriceDto(
            Id: original.Id,
            ListCode: "TEST-UPDATED",
            UomName: "KG",
            UnitPrice: 200.50m,
            Currency: "USD",
            ValidFrom: new DateTime(2025, 1, 1),
            ValidTo: new DateTime(2025, 12, 31)
        );

        // Act
        var result = await _sut.UpdatePriceAsync(original.Id, update);

        // Assert
        result.ListCode.Should().Be("TEST-UPDATED");
        result.UomName.Should().Be("KG");
        result.UnitPrice.Should().Be(200.50m);
        result.Currency.Should().Be("USD");
        result.ValidFrom.Should().Be(new DateTime(2025, 1, 1));
        result.ValidTo.Should().Be(new DateTime(2025, 12, 31));

        // Verify in database
        var dbPrice = await _db.Prices.FindAsync(original.Id);
        dbPrice!.ListCode.Should().Be("TEST-UPDATED");
    }

    [Fact]
    public async Task UpdatePrice_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var update = new UpdatePriceDto(
            Id: 99999, // Non-existent
            ListCode: "TEST",
            UomName: "EA",
            UnitPrice: 100.00m,
            Currency: "TRY",
            ValidFrom: null,
            ValidTo: null
        );

        // Act & Assert
        var act = async () => await _sut.UpdatePriceAsync(99999, update);
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Price with ID 99999 not found*");
    }

    [Fact]
    public async Task DeletePrice_Success()
    {
        // Arrange
        var price = await _sut.AddPriceAsync(new CreatePriceDto(
            _testProductId, "TO-DELETE", "EA", 100.00m, "TRY", null, null
        ));

        // Act
        await _sut.DeletePriceAsync(price.Id);

        // Assert
        var dbPrice = await _db.Prices.FindAsync(price.Id);
        dbPrice.Should().BeNull();
    }

    [Fact]
    public async Task DeletePrice_NotFound_ThrowsKeyNotFoundException()
    {
        // Act & Assert
        var act = async () => await _sut.DeletePriceAsync(99999);
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Price with ID 99999 not found*");
    }

    [Fact]
    public async Task GetEffectivePrice_WithValidDateRange_ReturnsPrice()
    {
        // Arrange
        var dto = new CreatePriceDto(
            ProductId: _testProductId,
            ListCode: "SUMMER",
            UomName: "EA",
            UnitPrice: 150.00m,
            Currency: "TRY",
            ValidFrom: new DateTime(2025, 6, 1),
            ValidTo: new DateTime(2025, 8, 31)
        );
        await _sut.AddPriceAsync(dto);

        // Act - Query for a date within range
        var result = await _sut.GetEffectivePriceAsync(
            productId: _testProductId,
            listCode: "SUMMER",
            uomName: "EA",
            date: new DateTime(2025, 7, 15)
        );

        // Assert
        result.Should().NotBeNull();
        result!.UnitPrice.Should().Be(150.00m);
        result.ListCode.Should().Be("SUMMER");
    }

    [Fact]
    public async Task GetEffectivePrice_OutsideValidRange_ReturnsNull()
    {
        // Arrange
        var dto = new CreatePriceDto(
            ProductId: _testProductId,
            ListCode: "WINTER",
            UomName: "EA",
            UnitPrice: 200.00m,
            Currency: "TRY",
            ValidFrom: new DateTime(2024, 12, 1),
            ValidTo: new DateTime(2024, 12, 31) // Expired
        );
        await _sut.AddPriceAsync(dto);

        // Act - Query for a date after expiry
        var result = await _sut.GetEffectivePriceAsync(
            productId: _testProductId,
            listCode: "WINTER",
            uomName: "EA",
            date: new DateTime(2025, 1, 15)
        );

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetEffectivePrice_NullValidDates_AlwaysValid()
    {
        // Arrange - Price with no date restrictions
        var dto = new CreatePriceDto(
            ProductId: _testProductId,
            ListCode: "ALWAYS",
            UomName: "EA",
            UnitPrice: 99.99m,
            Currency: "TRY",
            ValidFrom: null,
            ValidTo: null
        );
        await _sut.AddPriceAsync(dto);

        // Act - Query for any date
        var result1 = await _sut.GetEffectivePriceAsync(
            productId: _testProductId,
            listCode: "ALWAYS",
            uomName: "EA",
            date: new DateTime(2020, 1, 1)
        );

        var result2 = await _sut.GetEffectivePriceAsync(
            productId: _testProductId,
            listCode: "ALWAYS",
            uomName: "EA",
            date: new DateTime(2030, 12, 31)
        );

        // Assert
        result1.Should().NotBeNull();
        result1!.UnitPrice.Should().Be(99.99m);
        result2.Should().NotBeNull();
        result2!.UnitPrice.Should().Be(99.99m);
    }

    [Fact]
    public async Task GetEffectivePrice_WrongListCode_ReturnsNull()
    {
        // Arrange
        await _sut.AddPriceAsync(new CreatePriceDto(
            _testProductId, "LIST-A", "EA", 100.00m, "TRY", null, null
        ));

        // Act - Query with different list code
        var result = await _sut.GetEffectivePriceAsync(
            productId: _testProductId,
            listCode: "LIST-B", // Different
            uomName: "EA",
            date: DateTime.Today
        );

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetEffectivePrice_WrongUom_ReturnsNull()
    {
        // Arrange
        await _sut.AddPriceAsync(new CreatePriceDto(
            _testProductId, "TEST", "EA", 100.00m, "TRY", null, null
        ));

        // Act - Query with different UOM
        var result = await _sut.GetEffectivePriceAsync(
            productId: _testProductId,
            listCode: "TEST",
            uomName: "KG", // Different
            date: DateTime.Today
        );

        // Assert
        result.Should().BeNull();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
