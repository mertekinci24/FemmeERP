using System;
using System.Threading.Tasks;
using InventoryERP.Application.Documents;
using InventoryERP.Application.Documents.DTOs;
using InventoryERP.Application.Products;
using FluentAssertions;
using InventoryERP.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Tests.Infrastructure;
using Tests.Unit.TestHelpers;
using Xunit;

namespace Tests.Unit;

public class TST_022_SalesOrderValidationTests : IDisposable
{
    private Microsoft.Data.Sqlite.SqliteConnection? _conn;
    public void Dispose()
    {
        _conn?.Close();
        _conn?.Dispose();
    }

    [Fact]
    public async Task SaveCommand_IsDisabled_When_Partner_Is_Missing_For_SalesOrder()
    {
        var (provider, conn) = TestServiceProviderFactory.CreateWithInMemoryDb();
        _conn = conn;

        var cmd = provider.GetRequiredService<IDocumentCommandService>();
        var products = provider.GetRequiredService<IProductsReadService>();
        var dialog = new Tests.Unit.TestHelpers.StubDialogService();

        // Seed a product to allow adding a valid line
        var db = provider.GetRequiredService<Persistence.AppDbContext>();
        var p = new InventoryERP.Domain.Entities.Product { Sku = "SKU-1", Name = "Test", BaseUom = "EA", VatRate = 20 };
        db.Products.Add(p);
        await db.SaveChangesAsync();

        var dto = new DocumentDetailDto
        {
            Type = "SALES_ORDER",
            Number = $"SO-{DateTime.Now:yyyyMMddHHmmss}",
            Date = DateTime.Today,
            PartnerId = null, // Mandatory missing
            Currency = "TRY",
            Lines = new System.Collections.Generic.List<DocumentLineDto>
            {
                new DocumentLineDto{ ItemId = p.Id, Qty = 1, Uom = "EA", UnitPrice = 10, VatRate = 20 }
            }
        };

        var vm = DocumentEditViewModelFactory.Create(dto, cmd, products, dialog);

        // Assert: SaveCommand cannot execute; MUST NOT reach DB and trigger FK crash
        vm.SaveCommand.CanExecute(null).Should().BeFalse();

        // Extra safety: calling SaveAsync should return false and not throw
        var ok = await vm.SaveAsync();
        ok.Should().BeFalse();
    }
}
