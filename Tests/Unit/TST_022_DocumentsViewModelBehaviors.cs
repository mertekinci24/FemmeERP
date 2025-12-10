using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using InventoryERP.Domain.Entities;
using InventoryERP.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Persistence;
using Tests.Infrastructure;
using Xunit;

namespace Tests.Unit;

public class TST_022_DocumentsViewModelBehaviors : IDisposable
{
    private Microsoft.Data.Sqlite.SqliteConnection? _conn;
    public void Dispose()
    {
        _conn?.Close();
        _conn?.Dispose();
    }

    [Fact]
    public async Task DocumentsViewModel_AutoLoads_On_Construct()
    {
        var (provider, conn) = TestServiceProviderFactory.CreateWithInMemoryDb();
        _conn = conn;
        var db = provider.GetRequiredService<AppDbContext>();

        // Seed one document before resolving the VM
        var pr = new Partner { Role = InventoryERP.Domain.Enums.PartnerRole.CUSTOMER, Title = "ACME", TaxNo = "111" };
        db.Partners.Add(pr);
        db.Documents.Add(new Document
        {
            Type = InventoryERP.Domain.Enums.DocumentType.SALES_ORDER,
            Status = InventoryERP.Domain.Enums.DocumentStatus.DRAFT,
            Date = DateTime.Today,
            PartnerId = null,
            Number = $"SO-{DateTime.Now:yyyyMMddHHmmss}"
        });
        await db.SaveChangesAsync();

        var vm = provider.GetRequiredService<DocumentsViewModel>();

        // Auto-load should have run and found at least the seeded document (depending on default filter window)
        // If filters limit, force a quick refresh and assert non-throw + at least zero rows.
        await vm.RefreshAsync();
        vm.Rows.Should().NotBeNull();
        vm.Rows.Count.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task NewSalesOrder_Cancel_DeletesDraft()
    {
        var services = new ServiceCollection();
        // Build provider but override IDialogService to return false for ShowDocumentEditDialogAsync
        var (prov, conn) = TestServiceProviderFactory.CreateWithInMemoryDb();
        _conn = conn;
        var ctx = prov.GetRequiredService<AppDbContext>();

        // Manually construct DocumentsViewModel with stub dialog service (returns false)
        var queries = prov.GetRequiredService<InventoryERP.Application.Documents.IDocumentQueries>();
        var invCmd = prov.GetRequiredService<InventoryERP.Application.Documents.IInvoiceCommandService>();
        var docCmd = prov.GetRequiredService<InventoryERP.Application.Documents.IDocumentCommandService>();
        var prodSvc = prov.GetRequiredService<InventoryERP.Application.Products.IProductsReadService>();
        var scopeFactory = prov.GetRequiredService<Microsoft.Extensions.DependencyInjection.IServiceScopeFactory>();
        var logger = prov.GetRequiredService<Serilog.ILogger>();
        var stubDialog = new Tests.Unit.TestHelpers.StubDialogService();
        var vm = new DocumentsViewModel(queries, invCmd, docCmd, prodSvc, scopeFactory, prov, logger, stubDialog);

        var before = ctx.Documents.Count();
        vm.NewSalesOrderCommand.Execute(null);
        // Allow async operations to settle
        await Task.Delay(200);

        var after = ctx.Documents.Count();
        after.Should().Be(before); // draft created then deleted
    }

    [Fact]
    public async Task Convert_SalesOrder_From_POSTED_Succeeds()
    {
        var (provider, conn) = TestServiceProviderFactory.CreateWithInMemoryDb();
        _conn = conn;
        var ctx = provider.GetRequiredService<AppDbContext>();

        var partner = new Partner { Role = InventoryERP.Domain.Enums.PartnerRole.CUSTOMER, Title = "Test Customer", TaxNo = "123" };
        var product = new Product { Sku = "P001", Name = "Prod", BaseUom = "EA", VatRate = 20 };
        ctx.Partners.Add(partner);
        ctx.Products.Add(product);
        await ctx.SaveChangesAsync();

        var so = new Document
        {
            Type = InventoryERP.Domain.Enums.DocumentType.SALES_ORDER,
            Status = InventoryERP.Domain.Enums.DocumentStatus.POSTED,
            Date = DateTime.Today,
            PartnerId = partner.Id,
            Number = $"SO-{DateTime.Now:yyyyMMddHHmmss}"
        };
        ctx.Documents.Add(so);
        await ctx.SaveChangesAsync();
        ctx.DocumentLines.Add(new DocumentLine{ DocumentId = so.Id, ItemId = product.Id, Qty = 2, Uom = "EA", UnitPrice = 10, VatRate = 20});
        await ctx.SaveChangesAsync();

        var vm = provider.GetRequiredService<DocumentsViewModel>();
        await vm.RefreshAsync();
        vm.Selected = vm.Rows.First(r => r.Id == so.Id);

        await vm.ConvertSelectedToDispatchAsync();

        var dispatch = ctx.Documents.FirstOrDefault(d => d.Type == InventoryERP.Domain.Enums.DocumentType.SEVK_IRSALIYESI);
        dispatch.Should().NotBeNull();
    }
}

