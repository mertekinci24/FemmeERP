// v1.0.20: Integration test for UPSERT logic in ImportService
using FluentAssertions;
using InventoryERP.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Serilog;

namespace Tests.Integration;

public class ImportServiceUpsertTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly ImportService _sut;
    private readonly ILogger _logger;

    public ImportServiceUpsertTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;
        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
        
        _logger = new LoggerConfiguration().CreateLogger();
        _sut = new ImportService(_db, _logger);
    }

    [Fact]
    public async Task ImportProductsFromCsvAsync_Should_Insert_NewProduct_On_FirstImport()
    {
        // Arrange
        var csvPath = Path.Combine(Path.GetTempPath(), $"test_products_{Guid.NewGuid()}.csv");
        await File.WriteAllTextAsync(csvPath, """
            Ürün Kodu;Ürün Adý;Birim;Alýþ Kdv;Kategori;Aktif
            P001;Test Ürün 1;Adet;20%;Kategori A;Aktif
            """);

        // Act
        var result = await _sut.ImportProductsFromCsvAsync(csvPath);

        // Assert
        result.Should().Be(1);
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Sku == "P001");
        product.Should().NotBeNull();
        product!.Name.Should().Be("Test Ürün 1");
        product.BaseUom.Should().Be("Adet");
        product.VatRate.Should().Be(20);
        product.Active.Should().BeTrue();
        product.Category.Should().Be("Kategori A");

        // Cleanup
        File.Delete(csvPath);
    }

    [Fact]
    public async Task ImportProductsFromCsvAsync_Should_Update_ExistingProduct_On_SecondImport()
    {
        // Arrange - First import
        var csvPath = Path.Combine(Path.GetTempPath(), $"test_products_{Guid.NewGuid()}.csv");
        await File.WriteAllTextAsync(csvPath, """
            Ürün Kodu;Ürün Adý;Birim;Alýþ Kdv;Kategori;Aktif
            P001;Test Ürün 1;Adet;20%;Kategori A;Aktif
            """);
        await _sut.ImportProductsFromCsvAsync(csvPath);

        // Arrange - Second import with UPDATED data for same SKU
        await File.WriteAllTextAsync(csvPath, """
            Ürün Kodu;Ürün Adý;Birim;Alýþ Kdv;Kategori;Aktif
            P001;Test Ürün 1 UPDATED;Kutu;10%;Kategori B;Pasif
            """);

        // Act
        var result = await _sut.ImportProductsFromCsvAsync(csvPath);

        // Assert
        result.Should().Be(1); // Still counts as 1 operation (update)
        var products = await _db.Products.Where(p => p.Sku == "P001").ToListAsync();
        products.Should().HaveCount(1, "because UPSERT should UPDATE existing product, not INSERT duplicate");
        
        var product = products.First();
        product.Name.Should().Be("Test Ürün 1 UPDATED", "because name should be updated");
        product.BaseUom.Should().Be("Kutu", "because UOM should be updated");
        product.VatRate.Should().Be(10, "because VAT rate should be updated");
        product.Active.Should().BeFalse("because Active should be updated to Pasif");
        product.Category.Should().Be("Kategori B", "because category should be updated");

        // Cleanup
        File.Delete(csvPath);
    }

    [Fact]
    public async Task ImportProductsFromCsvAsync_Should_Handle_MixedInsertAndUpdate()
    {
        // Arrange - First import with 2 products
        var csvPath = Path.Combine(Path.GetTempPath(), $"test_products_{Guid.NewGuid()}.csv");
        await File.WriteAllTextAsync(csvPath, """
            Ürün Kodu;Ürün Adý;Birim;Alýþ Kdv;Kategori;Aktif
            P001;Ürün 1;Adet;20%;Kategori A;Aktif
            P002;Ürün 2;Kg;10%;Kategori B;Aktif
            """);
        await _sut.ImportProductsFromCsvAsync(csvPath);

        // Arrange - Second import: Update P001, Insert P003
        await File.WriteAllTextAsync(csvPath, """
            Ürün Kodu;Ürün Adý;Birim;Alýþ Kdv;Kategori;Aktif
            P001;Ürün 1 UPDATED;Kutu;1%;Kategori C;Pasif
            P003;Ürün 3;Litre;10%;Kategori D;Aktif
            """);

        // Act
        var result = await _sut.ImportProductsFromCsvAsync(csvPath);

        // Assert
        result.Should().Be(2); // 1 update + 1 insert = 2 operations
        
        var allProducts = await _db.Products.OrderBy(p => p.Sku).ToListAsync();
        allProducts.Should().HaveCount(3, "because P001 updated, P002 untouched, P003 inserted");
        
        var p1 = allProducts.First(p => p.Sku == "P001");
        p1.Name.Should().Be("Ürün 1 UPDATED");
        p1.Active.Should().BeFalse();
        
        var p2 = allProducts.First(p => p.Sku == "P002");
        p2.Name.Should().Be("Ürün 2", "because P002 was not in second import, should remain unchanged");
        
        var p3 = allProducts.First(p => p.Sku == "P003");
        p3.Name.Should().Be("Ürün 3");
        p3.Active.Should().BeTrue();

        // Cleanup
        File.Delete(csvPath);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
