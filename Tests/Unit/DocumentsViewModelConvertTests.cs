using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using InventoryERP.Presentation.ViewModels;
using InventoryERP.Application.Documents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Tests.Infrastructure;
using Persistence;
using InventoryERP.Domain.Entities;

namespace Tests.Unit;

/// <summary>
/// R-036: Integration test for DocumentsViewModel convert operations.
/// Refactored to use TestServiceProviderFactory with real DI container and in-memory database.
/// Tests document conversion workflows (SALES_ORDER -> DISPATCH -> INVOICE).
/// NO MORE DUMMY/MOCK SERVICES - uses real Infrastructure + Persistence layers.
/// </summary>
public class DocumentsViewModelConvertTests : IDisposable
{
    private Microsoft.Data.Sqlite.SqliteConnection? _connection;

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
    }

    [Fact]
    public async Task ConvertSelected_Calls_Service_When_Selected_APPROVED_SALES_ORDER()
    {
        // R-036: Use TestServiceProviderFactory to create real DI container with in-memory database
        var (provider, conn) = TestServiceProviderFactory.CreateWithInMemoryDb();
        _connection = conn;

        var ctx = provider.GetRequiredService<AppDbContext>();
        
        // Seed database with a SALES_ORDER document
        var partner = new Partner 
        { 
            Role = Domain.Enums.PartnerRole.CUSTOMER,
            Title = "Test Customer", 
            TaxNo = "1234567890"
        };
        var product = new Product 
        { 
            Sku = "P001", 
            Name = "Test Product", 
            BaseUom = "EA",
            VatRate = 20 // Required: Must be 1, 10, or 20
        };
        
        ctx.Partners.Add(partner);
        ctx.Products.Add(product);
        await ctx.SaveChangesAsync(); // Save to get IDs
        
        var doc = new Document 
        { 
            Type = Domain.Enums.DocumentType.SALES_ORDER,
            Status = Domain.Enums.DocumentStatus.APPROVED,
            Date = DateTime.Today,
            PartnerId = partner.Id
        };
        
        ctx.Documents.Add(doc);
        await ctx.SaveChangesAsync();
        
        // Add line to document
        var line = new DocumentLine
        {
            DocumentId = doc.Id,
            ItemId = product.Id,
            Qty = 10m,
            Uom = "EA",
            UnitPrice = 100m,
            VatRate = 20
        };
        ctx.DocumentLines.Add(line);
        await ctx.SaveChangesAsync();

        var vm = provider.GetRequiredService<DocumentsViewModel>();

        // Act: Refresh to load the seeded SALES_ORDER
        await vm.RefreshAsync();
        
        // Select the SALES_ORDER
        var salesOrder = vm.Rows.FirstOrDefault(r => r.Type == "SALES_ORDER");
        Assert.NotNull(salesOrder);
        vm.Selected = salesOrder;

        // Convert SALES_ORDER -> DISPATCH
        await vm.ConvertSelectedToDispatchAsync();

        // Assert: Verify a new DISPATCH document was created in database
        var dispatch = await ctx.Documents
            .Include(d => d.Lines)
            .FirstOrDefaultAsync(d => d.Type == Domain.Enums.DocumentType.SEVK_IRSALIYESI);
        Assert.NotNull(dispatch);
        Assert.Equal(Domain.Enums.DocumentStatus.DRAFT, dispatch!.Status); // Converted document should be in Draft status
        Assert.Single(dispatch.Lines); // Should have 1 line copied from SALES_ORDER
    }
}
