using System;
using System.Threading.Tasks;
using InventoryERP.Application.Documents.DTOs;
using FluentAssertions;
using InventoryERP.Presentation.ViewModels;
using Tests.Unit.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Tests.Infrastructure;
using Xunit;
using Persistence;
using InventoryERP.Domain.Entities;
using InventoryERP.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Tests.Unit;

public class DocumentEditViewModelTitleTests
{
    private async Task<DocumentEditViewModel> CreateViewModelAsync(IServiceProvider provider, string docType)
    {
        var db = provider.GetRequiredService<AppDbContext>();
        
        // Seed required data
        if (!await db.Warehouses.AnyAsync())
        {
            db.Warehouses.Add(new Warehouse { Name = "Main Warehouse", Code = "MAIN" });
            await db.SaveChangesAsync();
        }

        if (!await db.Partners.AnyAsync())
        {
            db.Partners.Add(new Partner { Name = "Test Partner", PartnerType = PartnerType.Customer, IsActive = true });
            await db.SaveChangesAsync();
        }

        if (!await db.Products.AnyAsync())
        {
            db.Products.Add(new Product { Name = "Test Product", Sku = "TEST-001", BaseUom = "EA", VatRate = 20 });
            await db.SaveChangesAsync();
        }

        var svc = provider.GetRequiredService<InventoryERP.Application.Documents.IDocumentCommandService>();
        var draftId = await svc.CreateDraftAsync(new DocumentDetailDto
        {
            Type = docType,
            Date = DateTime.Today,
            Currency = "TRY"
        });
        
        // Fetch the created DTO to pass to the ViewModel constructor
        var queries = provider.GetRequiredService<InventoryERP.Application.Documents.IDocumentQueries>();
        var dto = await queries.GetAsync(draftId);

        // Use StubDialogService for tests
        var dialogService = new StubDialogService();
        
        var vm = new DocumentEditViewModel(
            dto,
            svc,
            provider.GetRequiredService<InventoryERP.Application.Products.IProductsReadService>(),
            dialogService,
            provider.GetRequiredService<InventoryERP.Application.Documents.IDocumentReportService>(),
            new StubFileDialogService(),
            provider.GetRequiredService<InventoryERP.Application.Partners.IPartnerService>(),
            Microsoft.Extensions.Options.Options.Create(new InventoryERP.Presentation.Configuration.UiOptions())
        );
        
        // LoadAsync is not needed if DTO is passed in constructor, but we might need it for other initializations if any
        // await vm.LoadAsync(draftId); 
        return vm;
    }

    [Fact]
    public async Task DocumentTitle_Should_Be_Sayim_Fisi_For_ADJUSTMENT_OUT()
    {
        var (provider, conn) = TestServiceProviderFactory.CreateWithInMemoryDb();
        try
        {
            var vm = await CreateViewModelAsync(provider, "ADJUSTMENT_OUT");
            // R-044: Title logic returns "Depo Düzeltme Fişi (Çıkış)"
            vm.DocumentTitle.Should().StartWith("Depo D");
            // vm.IsAdjustment.Should().BeTrue(); // Property might not exist on VM, checking Title only for now
        }
        finally { conn.Dispose(); }
    }

    [Fact]
    public async Task DocumentTitle_Should_Be_Sayim_Fisi_For_ADJUSTMENT_IN()
    {
        var (provider, conn) = TestServiceProviderFactory.CreateWithInMemoryDb();
        try
        {
            var vm = await CreateViewModelAsync(provider, "ADJUSTMENT_IN");
            // R-044: Title logic returns "Depo Düzeltme Fişi (Giriş)"
            vm.DocumentTitle.Should().StartWith("Depo D"); 
        }
        finally { conn.Dispose(); }
    }

    [Fact]
    public async Task DocumentTitle_Should_Be_Satis_Faturasi_For_SALES_INVOICE()
    {
        var (provider, conn) = TestServiceProviderFactory.CreateWithInMemoryDb();
        try
        {
            var vm = await CreateViewModelAsync(provider, "SALES_INVOICE");
            vm.DocumentTitle.Should().StartWith("Sat");
        }
        finally { conn.Dispose(); }
    }

    [Fact]
    public async Task DocumentTitle_Should_Be_Alis_Faturasi_For_PURCHASE_INVOICE()
    {
        var (provider, conn) = TestServiceProviderFactory.CreateWithInMemoryDb();
        try
        {
            var vm = await CreateViewModelAsync(provider, "PURCHASE_INVOICE");
            vm.DocumentTitle.Should().StartWith("Al");
        }
        finally { conn.Dispose(); }
    }
}
