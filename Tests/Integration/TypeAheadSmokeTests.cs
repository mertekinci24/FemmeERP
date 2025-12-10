using System;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Tests.Infrastructure;
using InventoryERP.Domain.Entities;
using InventoryERP.Application.Products;
using System.Collections.Generic;
using Persistence;

namespace Tests.Integration;

public class TypeAheadSmokeTests : BaseIntegrationTest
{
    private class TestProductsSvc : IProductsReadService
    {
        private readonly AppDbContext _ctx;
        public TestProductsSvc(AppDbContext ctx) => _ctx = ctx;
        public Task<IReadOnlyList<ProductRowDto>> GetListAsync(string? search)
        {
            var q = _ctx.Products.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search)) q = q.Where(p => p.Name.Contains(search) || p.Sku.Contains(search));
            var list = q.Select(p => new ProductRowDto(p.Id, p.Sku, p.Name, p.BaseUom, p.VatRate, p.Active, 0m)).ToList();
            return Task.FromResult((IReadOnlyList<ProductRowDto>)list);
        }
        public Task<IReadOnlyList<ProductUomDto>> GetUomsAsync(int productId)
        {
            return Task.FromResult((IReadOnlyList<ProductUomDto>)new List<ProductUomDto>());
        }
        public Task<IReadOnlyList<Application.Products.ProductLotDto>> GetLotsForProductAsync(int productId)
        {
            return Task.FromResult((IReadOnlyList<Application.Products.ProductLotDto>)new List<Application.Products.ProductLotDto>());
        }
        public Task<IReadOnlyList<ProductVariantDto>> GetVariantsAsync(int productId)
        {
            return Task.FromResult((IReadOnlyList<ProductVariantDto>)new List<ProductVariantDto>());
        }
        public Task<ProductRowDto?> GetByCodeAsync(string code) => Task.FromResult<ProductRowDto?>(null);
    }

    private class DummyCmd : Application.Documents.IDocumentCommandService
    {
        public Task<int> CreateDraftAsync(Application.Documents.DTOs.DocumentDetailDto dto) => Task.FromResult(0);
        public Task UpdateDraftAsync(int id, Application.Documents.DTOs.DocumentDetailDto dto) => Task.CompletedTask;
        public Task DeleteDraftAsync(int id) => Task.CompletedTask;
        public Task ApproveAsync(int id) => Task.CompletedTask;
        public Task CancelAsync(int id) => Task.CompletedTask;
        public Task<int> ConvertSalesOrderToDispatchAsync(int salesOrderId) => Task.FromResult(0);
        public Task<int> ConvertDispatchToInvoiceAsync(int dispatchId) => Task.FromResult(0);
        public Task SaveAndApproveAdjustmentAsync(int id, Application.Documents.DTOs.DocumentDetailDto dto) => Task.CompletedTask;
    }

    [Fact(Timeout = 60000)]
    public async Task TypeAhead_10kProducts_Filter_under_300ms()
    {
        // seed 10k products with distributed names containing 'ab' in ~10% of them
        const int total = 10000;
        var rnd = new Random(12345);
        for (int i = 0; i < total; i++)
        {
            var has = (i % 10 == 0); // every 10th contains 'ab'
            var name = has ? $"Product AB {i}" : $"Product {i} {rnd.Next(0,100000)}";
            var sku = has ? $"AB-{i:00000}" : $"SKU-{i:00000}";
            _ = Ctx.Products.Add(new Product { Name = name, Sku = sku, BaseUom = "pcs", VatRate = 1, Active = true });
        }
        await Ctx.SaveChangesAsync();

        // create vm and populate Products directly
        var dto = new Application.Documents.DTOs.DocumentDetailDto { Id = 9999 };
        var vm = new InventoryERP.Presentation.ViewModels.DocumentEditViewModel(dto, new DummyCmd(), new TestProductsSvc(Ctx), new Tests.Unit.TestHelpers.StubDialogService());

        // populate Products collection from DB (simulate load)
        var svc = new TestProductsSvc(Ctx);
        var list = await svc.GetListAsync(null);
        foreach (var p in list) vm.Products.Add(p);

        // measure filter time for SearchText = "ab"
        vm.SearchText = "ab"; // setter does immediate refresh + schedules debounce; measure explicit refresh cost

        var sw = Stopwatch.StartNew();
        vm.ProductsView.Refresh();
        sw.Stop();

        var ms = sw.ElapsedMilliseconds;
        Assert.True(ms < 300, $"ProductsView.Refresh took {ms} ms, expected < 300 ms");
    }
}
