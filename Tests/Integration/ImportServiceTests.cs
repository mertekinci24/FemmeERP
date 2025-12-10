using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using InventoryERP.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tests.Infrastructure;
using Xunit;

namespace Tests.Integration;

public class ImportServiceTests : BaseIntegrationTest
{
    [Fact]
    public async Task ImportProductsFromCsv_Inserts_New_Products()
    {
        // Arrange - R-033: Use semicolon delimiter, test Category and Alýþ Kdv
        var tmp = Path.GetTempFileName();
        await File.WriteAllTextAsync(tmp, "Sku;Name;BaseUom;VatRate;Active;Kategori\nSKU1;Prod 1;ADET;1;true;Cat A\nSKU2;Prod 2;ADET;1;true;Cat B\n");

        var services = new ServiceCollection();
        services.AddScoped(_ => Ctx);
        services.AddSingleton<Serilog.ILogger>(new Serilog.LoggerConfiguration().CreateLogger()); // R-037
        services.AddInfrastructure();
        using var sp = services.BuildServiceProvider();

        var svc = sp.GetRequiredService<Application.Import.IImportService>();

        // Act
        var count = await svc.ImportProductsFromCsvAsync(tmp);

        // Assert
    count.Should().Be(2);
    (await Ctx.Products.CountAsync()).Should().Be(2);
    var prod1 = await Ctx.Products.FirstOrDefaultAsync(p => p.Sku == "SKU1");
    prod1.Should().NotBeNull();
    prod1!.Category.Should().Be("Cat A"); // R-033: Category imported

        // Cleanup
        try { File.Delete(tmp); } catch { }
    }
}
