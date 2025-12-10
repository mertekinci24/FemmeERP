using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using InventoryERP.Domain.Entities;
using InventoryERP.Infrastructure.Queries;
using Tests.Infrastructure;

namespace Tests.Integration;

public class StockQueriesEfTests : BaseIntegrationTest
{
    [Fact]
    public async Task ListMovesAsync_FiltersByProductAndDate_AndOrdersDesc()
    {
        // arrange - create product and several stock moves
    var p = new Product { Sku = "TST-001", Name = "Test Prod", BaseUom = "pcs", VatRate = 1 };
        Ctx.Products.Add(p);
        await Ctx.SaveChangesAsync();

        var today = DateTime.Today;
        var older = today.AddDays(-10);
        var newer = today.AddDays(-1);

        Ctx.StockMoves.Add(new StockMove { ItemId = p.Id, Date = older, QtySigned = 5, UnitCost = 10m, Note = "old" });
        Ctx.StockMoves.Add(new StockMove { ItemId = p.Id, Date = newer, QtySigned = -2, UnitCost = 11m, Note = "new" });
    var other = new Product { Sku = "TST-002", Name = "Other Prod", BaseUom = "pcs", VatRate = 1 };
    Ctx.Products.Add(other);
    await Ctx.SaveChangesAsync();
    Ctx.StockMoves.Add(new StockMove { ItemId = other.Id, Date = newer, QtySigned = 100, UnitCost = 1m, Note = "other product" });
        await Ctx.SaveChangesAsync();

        var svc = new StockQueriesEf(Ctx);

        // act - no date range
        var all = await svc.ListMovesAsync(p.Id, null, null);

        // assert
        all.Should().NotBeNull();
        all.Count.Should().BeGreaterThanOrEqualTo(2);
        // order should be descending by date -> newest first
        all.First().Date.Date.Should().Be(newer.Date);

        // act - with from filter that excludes older
        var filtered = await svc.ListMovesAsync(p.Id, DateOnly.FromDateTime(DateTime.Today.AddDays(-3)), null);
        filtered.Should().OnlyContain(r => r.Date.Date >= DateTime.Today.AddDays(-3));
    }
}
